using Microsoft.AspNetCore.Mvc;
using SupportAtos.Application.Exceptions;
using SupportAtos.Domain.Exceptions;

namespace SupportAtos.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest, ToProblemDetails(StatusCodes.Status400BadRequest, "Requisição inválida", ex.Errors)),
            DomainException ex => (StatusCodes.Status400BadRequest, ToProblemDetails(StatusCodes.Status400BadRequest, "Requisição inválida", new[] { ex.Message })),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, ToProblemDetails(StatusCodes.Status403Forbidden, "Acesso negado", new[] { ex.Message })),
            NotFoundException ex => (StatusCodes.Status404NotFound, ToProblemDetails(StatusCodes.Status404NotFound, "Não encontrado", new[] { ex.Message })),
            ConflictException ex => (StatusCodes.Status409Conflict, ToProblemDetails(StatusCodes.Status409Conflict, "Conflito", new[] { ex.Message })),
            _ => HandleUnexpected(context, exception)
        };
        (int, ProblemDetails) HandleUnexpected(HttpContext ctx, Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado na requisição {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            return (StatusCodes.Status500InternalServerError,
                ToProblemDetails(StatusCodes.Status500InternalServerError, "Erro interno do servidor", new[] { "Ocorreu um erro inesperado ao processar a requisição." }));
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(problemDetails));
    }

    private static ProblemDetails ToProblemDetails(int statusCode, string title, IEnumerable<string> errors)
    {
        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = errors.FirstOrDefault()
        };

        details.Extensions["errors"] = errors;

        return details;
    }
}
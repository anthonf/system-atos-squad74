using Microsoft.AspNetCore.Mvc;
using SupportAtos.Api.Authentication;
using SupportAtos.Api.Security;

namespace SupportAtos.Api.Middleware;

public class SecurityRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityRateLimiter _limiter;

    public SecurityRateLimitMiddleware(RequestDelegate next, ISecurityRateLimiter limiter)
    {
        _next = next;
        _limiter = limiter;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identificador = context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        if (_limiter.EstaBloqueado(identificador))
        {
            await Escrever429(context, "Muitas falhas de autenticação. Tente novamente em instantes.");
            return;
        }

        if (context.Request.Headers.ContainsKey(AuthenticationSetup.ApiKeyHeaderName)
            && !_limiter.PermitirRequisicao(identificador))
        {
            await Escrever429(context, "Limite de requisições excedido. Tente novamente em instantes.");
            return;
        }

        await _next(context);
    }

    private static async Task Escrever429(HttpContext context, string detalhe)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = "60";
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Muitas requisições",
            Detail = detalhe,
            Instance = context.Request.Path
        });
    }
}

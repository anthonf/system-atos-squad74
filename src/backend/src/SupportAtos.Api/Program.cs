using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using SupportAtos.Api.Authentication;
using SupportAtos.Api.Middleware;
using SupportAtos.Api.Providers;
using SupportAtos.Application;
using SupportAtos.Application.Abstractions;
using SupportAtos.Infrastructure;

var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--check-dev-auth").ToArray());

if (args.Contains("--check-dev-auth"))
{
    if (!builder.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("O diagnóstico --check-dev-auth exige ambiente Development.");
        Environment.ExitCode = 1;
        return;
    }

    Environment.ExitCode = DevAuthenticationDiagnostic.Executar(
        builder.Configuration, builder.Environment.ContentRootPath, Console.Out);
    return;
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT (RS256) emitido pelo portal. Rotas de portal exigem este scheme."
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = AuthenticationSetup.ApiKeyHeaderName,
        Description = "API key máquina-a-máquina (ex.: n8n). Só atende endpoints de webhook; rotas de portal retornam 401/403."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

// CORS para o frontend (Vite em dev e Static Site no Render em produção).
// Origens via config: "Frontend:Origins": ["https://seu-frontend.onrender.com"]
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>();

        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            // Em produção (Render) sem config explícita, libera para chamadas de qualquer origem
            // usando Bearer Token no header Authorization (não depende de cookies).
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    }));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSsoAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UsuarioPortal", policy =>
        policy.RequireAuthenticatedUser().AddRequirements(new UsuarioPortalRequirement()));
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddScoped<IAuthorizationHandler, UsuarioPortalAuthorizationHandler>();

builder.Services.AddScoped<ICurrentUser, JwtCurrentUserProvider>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using SupportAtos.Api.Authentication;
using SupportAtos.Api.Middleware;
using SupportAtos.Api.Options;
using SupportAtos.Api.Providers;
using SupportAtos.Api.Security;
using SupportAtos.Application;
using SupportAtos.Application.Abstractions;
using SupportAtos.Domain.Enums;
using SupportAtos.Infrastructure;
using SupportAtos.Infrastructure.Webhooks;

if (args.Contains("--generate-api-key"))
{
    Environment.ExitCode = ApiKeyGenerator.Executar(args, Console.Out);
    return;
}

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

var apiKeySettings = builder.Configuration
    .GetSection(ApiKeySettings.SectionName)
    .Get<ApiKeySettings>() ?? new ApiKeySettings();
ApiKeyConfigurationValidator.Validar(apiKeySettings, builder.Environment);

var webhookSettings = builder.Configuration
    .GetSection(WebhookSettings.SectionName)
    .Get<WebhookSettings>() ?? new WebhookSettings();
WebhookSettingsValidator.Validar(webhookSettings, builder.Environment);

if (!string.IsNullOrWhiteSpace(webhookSettings.ApiKey))
{
    var hashWebhook = ApiKeyHasher.Hash(webhookSettings.ApiKey);
    if (apiKeySettings.Portal.Hashes()
        .Concat(apiKeySettings.Automacao.Hashes())
        .Any(hash => string.Equals(hash.Trim(), hashWebhook, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException(
            "Webhooks:ApiKey deve ser distinta das API keys de entrada (portal/automação).");
    }
}

builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection(ApiKeySettings.SectionName));

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
        Description = "JWT (RS256) da equipe: Admin, Supervisor e Agente."
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = AuthenticationSetup.ApiKeyHeaderName,
        Description = "API key servidor-a-servidor. Escopos: portal (abre/consulta chamados em nome de usuários) e automacao (n8n)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

// Origens aceitas vêm de "Cors:AllowedOrigins" (Cors__AllowedOrigins__0, ...) e de
// "Frontend:Origins" (Frontend__Origins no Render). Ambas aceitam lista ou valor único.
var origensFrontend = new[] { "Cors:AllowedOrigins", "Frontend:Origins" }
    .SelectMany(secao => LerOrigens(builder.Configuration, secao))
    .Where(origem => !string.IsNullOrWhiteSpace(origem))
    .Select(origem => origem!)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
if (origensFrontend.Length == 0 && builder.Environment.IsDevelopment())
{
    origensFrontend = new[] { "http://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (origensFrontend.Length > 0)
        {
            policy.WithOrigins(origensFrontend)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            // Sem origens configuradas (ex.: produção no Render): libera o acesso
            // para não bloquear o front por CORS.
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

static IEnumerable<string?> LerOrigens(IConfiguration configuration, string secao)
{
    var section = configuration.GetSection(secao);
    yield return section.Value;
    foreach (var filho in section.GetChildren())
    {
        yield return filho.Value;
    }
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSsoAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthenticationSetup.PolicyPortal, policy =>
        policy.AddAuthenticationSchemes(AuthenticationSetup.ApiKeyScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(AuthenticationSetup.ScopeClaim, AuthenticationSetup.PortalScope));

    options.AddPolicy(AuthenticationSetup.PolicyAutomacao, policy =>
        policy.AddAuthenticationSchemes(AuthenticationSetup.ApiKeyScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(AuthenticationSetup.ScopeClaim, AuthenticationSetup.AutomacaoScope));

    options.AddPolicy(AuthenticationSetup.PolicyJwt, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());

    options.AddPolicy(AuthenticationSetup.PolicyEquipe, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole(
                nameof(PapelUsuario.Admin),
                nameof(PapelUsuario.Supervisor),
                nameof(PapelUsuario.Agente)));

    options.AddPolicy(AuthenticationSetup.PolicyEquipeAdmin, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole(nameof(PapelUsuario.Admin)));

    options.AddPolicy(AuthenticationSetup.PolicyAtribuicao, policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireRole(
                nameof(PapelUsuario.Agente),
                nameof(PapelUsuario.Supervisor),
                nameof(PapelUsuario.Admin)));

    options.AddPolicy(AuthenticationSetup.PolicyPortalOuEquipe, policy =>
        policy.AddAuthenticationSchemes(
                AuthenticationSetup.ApiKeyScheme,
                JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                context.User.HasClaim(AuthenticationSetup.ScopeClaim, AuthenticationSetup.PortalScope)
                || context.User.IsInRole(nameof(PapelUsuario.Admin))
                || context.User.IsInRole(nameof(PapelUsuario.Supervisor))
                || context.User.IsInRole(nameof(PapelUsuario.Agente))));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUserProvider>();
builder.Services.AddScoped<PortalUserResolver>();
builder.Services.AddSingleton<ISecurityRateLimiter, SecurityRateLimiter>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<SecurityRateLimitMiddleware>();

app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    if (response.HasStarted || response.ContentLength > 0)
    {
        return;
    }

    var (titulo, detalhe) = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ("Não autenticado", "Credenciais ausentes ou inválidas."),
        StatusCodes.Status403Forbidden => ("Acesso negado", "Você não tem permissão para acessar este recurso."),
        StatusCodes.Status404NotFound => ("Recurso não encontrado", "O recurso solicitado não existe."),
        StatusCodes.Status405MethodNotAllowed => ("Método não permitido", "O método HTTP não é suportado por este recurso."),
        _ => (string.Empty, string.Empty)
    };

    if (string.IsNullOrEmpty(titulo))
    {
        return;
    }

    response.ContentType = "application/problem+json";
    await response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = response.StatusCode,
        Title = titulo,
        Detail = detalhe,
        Instance = statusCodeContext.HttpContext.Request.Path
    });
});

app.UseCors("Frontend");

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PortalUserContextMiddleware>();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;

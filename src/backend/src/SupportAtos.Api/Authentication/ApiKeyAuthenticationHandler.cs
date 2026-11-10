using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SupportAtos.Api.Options;
using SupportAtos.Api.Security;

namespace SupportAtos.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IOptionsMonitor<ApiKeySettings> _settings;
    private readonly ISecurityRateLimiter _limiter;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeySettings> settings,
        ISecurityRateLimiter limiter,
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _settings = settings;
        _limiter = limiter;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthenticationSetup.ApiKeyHeaderName, out var valores) || valores.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var recebida = valores.ToString();
        if (string.IsNullOrWhiteSpace(recebida))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key ausente."));
        }

        var escopo = ResolverEscopo(recebida, _settings.CurrentValue);
        if (escopo is null)
        {
            var ip = Context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
            _limiter.RegistrarFalhaAutenticacao(ip);
            Logger.LogWarning(
                "Falha de autenticação por API key. IP={Ip} Rota={Rota}",
                ip,
                Context.Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));
        }

        var identidade = new ClaimsIdentity(Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.Name, $"api-key:{escopo}"));
        identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, $"api-key:{escopo}"));
        identidade.AddClaim(new Claim(AuthenticationSetup.ScopeClaim, escopo));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string? ResolverEscopo(string recebida, ApiKeySettings settings)
    {
        var portal = settings.Portal.Hashes().Any(hash => ApiKeyHasher.Corresponde(recebida, hash));
        var automacao = settings.Automacao.Hashes().Any(hash => ApiKeyHasher.Corresponde(recebida, hash));

        if (portal)
        {
            return AuthenticationSetup.PortalScope;
        }

        return automacao ? AuthenticationSetup.AutomacaoScope : null;
    }
}

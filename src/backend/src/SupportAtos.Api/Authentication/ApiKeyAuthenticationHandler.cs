using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SupportAtos.Api.Options;

namespace SupportAtos.Api.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IOptionsMonitor<SsoSettings> _sso;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<SsoSettings> sso,
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _sso = sso;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var esperada = _sso.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(esperada))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(AuthenticationSetup.ApiKeyHeaderName, out var valores) || valores.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var recebida = valores[0]!;
        if (!FixedTimeEquals(esperada, recebida))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));
        }

        var identidade = new ClaimsIdentity(Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.Name, "webhook"));
        identidade.AddClaim(new Claim(ClaimTypes.NameIdentifier, "webhook"));

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidade), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string esperada, string recebida)
    {
        // Compara hashes SHA-256 (32 bytes fixos) em tempo constante para não
        // vazar o tamanho da chave configurada via tempo de resposta.
        var a = SHA256.HashData(Encoding.UTF8.GetBytes(esperada));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(recebida));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
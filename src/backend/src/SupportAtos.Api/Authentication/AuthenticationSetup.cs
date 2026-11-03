using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SupportAtos.Api.Options;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.Ports;

namespace SupportAtos.Api.Authentication;

public static class AuthenticationSetup
{
    private const string FalhaProvisionamento = "SupportAtos.FalhaProvisionamento";
    public const string ApiKeyScheme = "ApiKey";
    public const string ApiKeyHeaderName = "X-Api-Key";

    public static IServiceCollection AddSsoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var sso = new SsoSettings();
        configuration.GetSection("Sso").Bind(sso);
        services.Configure<SsoSettings>(configuration.GetSection("Sso"));

        if (string.IsNullOrWhiteSpace(sso.JwksUrl) && string.IsNullOrWhiteSpace(sso.PublicKeyPem))
        {
            throw new InvalidOperationException(
                "SSO não configurado: defina 'Sso:JwksUrl' (produção) ou 'Sso:PublicKeyPem' (local/testes) via user-secrets ou variáveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(sso.Issuer))
        {
            throw new InvalidOperationException(
                "SSO não configurado: defina 'Sso:Issuer' via user-secrets ou variáveis de ambiente.");
        }

        if (string.IsNullOrWhiteSpace(sso.Audience))
        {
            throw new InvalidOperationException(
                "SSO não configurado: defina 'Sso:Audience' via user-secrets ou variáveis de ambiente.");
        }

        var apiKeyConfigurada = !string.IsNullOrWhiteSpace(sso.ApiKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(jwt =>
        {
            jwt.MapInboundClaims = false;
            jwt.SaveToken = true;
            // Rotas de portal exigem JWT explícito (AuthenticationSchemes = JwtBearer).
            // O seletor só desvia para ApiKey quando há X-Api-Key configurada no servidor
            // E não há Bearer na requisição — um JWT válido nunca é derrubado por ApiKey.
            jwt.ForwardDefaultSelector = ctx =>
            {
                if (!apiKeyConfigurada)
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }

                var temApiKey = ctx.Request.Headers.ContainsKey(ApiKeyHeaderName);
                var authorization = ctx.Request.Headers.Authorization.ToString();
                var temBearer = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

                return temApiKey && !temBearer ? ApiKeyScheme : JwtBearerDefaults.AuthenticationScheme;
            };

            var tvp = jwt.TokenValidationParameters;
            tvp.ClockSkew = TimeSpan.FromSeconds(sso.ClockSkewSeconds);
            tvp.ValidIssuer = sso.Issuer;
            tvp.ValidAudience = sso.Audience;
            tvp.ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 };
            tvp.NameClaimType = sso.ClaimNames.Nome;
            tvp.RoleClaimType = sso.ClaimNames.Role;
            tvp.ValidateIssuerSigningKey = true;
            tvp.ValidateIssuer = true;
            tvp.ValidateAudience = true;
            tvp.ValidateLifetime = true;

            if (!string.IsNullOrWhiteSpace(sso.PublicKeyPem))
            {
                tvp.IssuerSigningKey = ChaveRsaDePem(sso.PublicKeyPem);
            }
            else
            {
                var retriever = new HttpDocumentRetriever { RequireHttps = true };
                jwt.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    sso.JwksUrl!,
                    new OpenIdConnectConfigurationRetriever(),
                    retriever)
                {
                    AutomaticRefreshInterval = TimeSpan.FromHours(12),
                    RefreshInterval = TimeSpan.FromMinutes(5)
                };

                tvp.IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var configuracao = jwt.ConfigurationManager
                        .GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return string.IsNullOrEmpty(kid)
                        ? configuracao.SigningKeys
                        : configuracao.SigningKeys.Where(chave => chave.KeyId == kid);
                };
            }

            jwt.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Falhas de infraestrutura/provisionamento não são tokens inválidos.
                    if (context.HttpContext.Items.ContainsKey(FalhaProvisionamento)
                        || context.Exception.GetBaseException() is HttpRequestException)
                    {
                        throw new InvalidOperationException("Falha ao preparar a identidade autenticada.", context.Exception);
                    }

                    // Também cobre exceções de claims lançadas em OnTokenValidated.
                    context.Fail(context.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var claimsPortal = PortalIdentityClaimsReader.Ler(context.Principal!, sso.ClaimNames);
                    var provisioning = context.HttpContext.RequestServices.GetRequiredService<IUserProvisioningService>();

                    var contextoExterno = new PortalUserContext(
                        0, 0,
                        claimsPortal.CodigoExternoUsuario,
                        claimsPortal.CodigoExternoGrupo,
                        claimsPortal.Email,
                        claimsPortal.Nome,
                        claimsPortal.NomeGrupo,
                        claimsPortal.Papel);

                    SupportAtos.Application.DTOs.ProvisioningResult resultado;
                    try
                    {
                        resultado = await provisioning.ProvisionAsync(contextoExterno, context.HttpContext.RequestAborted);
                    }
                    catch
                    {
                        context.HttpContext.Items[FalhaProvisionamento] = true;
                        throw;
                    }

                    var portal = contextoExterno with
                    {
                        UsuarioIdInterno = resultado.UsuarioIdInterno,
                        GrupoEmpresaIdInterno = resultado.GrupoEmpresaIdInterno
                    };

                    if (context.Principal?.Identity is ClaimsIdentity identidade)
                    {
                        foreach (var claimCargo in identidade.FindAll(sso.ClaimNames.Role).ToList())
                        {
                            identidade.RemoveClaim(claimCargo);
                        }

                        identidade.AddClaim(new Claim(sso.ClaimNames.Role, portal.Papel.ToString()));
                    }

                    context.HttpContext.Items[PortalUserContextItems.Chave] = portal;
                    context.HttpContext.Items[PortalUserContextItems.ExpiracaoToken] =
                        DateTime.SpecifyKind(context.SecurityToken.ValidTo, DateTimeKind.Utc);
                }
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyScheme, "API key (máquina a máquina)", null);

        return services;
    }

    private static SecurityKey ChaveRsaDePem(string pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return new RsaSecurityKey(rsa);
    }
}

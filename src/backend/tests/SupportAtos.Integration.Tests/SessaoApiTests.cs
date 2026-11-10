using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;

namespace SupportAtos.Integration.Tests;

// Exercita o pipeline JWT real; apenas o provisionamento é substituído para não exigir PostgreSQL.
public class SessaoApiTests
{
    private sealed class Provisionamento : IUserProvisioningService
    {
        public bool Falhar { get; set; }
        public int Chamadas { get; private set; }

        public Task<ProvisioningResult> ProvisionAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
        {
            Chamadas++;
            if (Falhar) throw new InvalidOperationException("Banco indisponível no teste.");
            return Task.FromResult(new ProvisioningResult(12, 3));
        }

        public Task<ProvisioningResult> ProvisionPortalAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
            => ProvisionAsync(usuario, cancellationToken);
    }

    private sealed class Fabrica : WebApplicationFactory<Program>
    {
        public Provisionamento Provisionamento { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestWebAppFactory.ConexaoBanco);
            builder.UseSetting("Sso:Issuer", TestTokenFactory.Issuer);
            builder.UseSetting("Sso:Audience", TestTokenFactory.Audience);
            builder.UseSetting("Sso:PublicKeyPem", TestTokenFactory.ChavePublicaPem);
            builder.UseSetting("ApiKeys:Portal:PrimaryHash", TestTokenFactory.PortalApiKeyHash);
            builder.UseSetting("ApiKeys:Automacao:PrimaryHash", TestTokenFactory.AutomacaoApiKeyHash);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserProvisioningService>();
                services.AddSingleton<IUserProvisioningService>(Provisionamento);
            });
        }
    }

    private static string Token(string papel = "agente") => TestTokenFactory.CriarToken(
        "portal-sessao", "ana@atos.com", "Ana", "grupo-externo", "Grupo Norte", papel);

    private static HttpRequestMessage Requisicao(string? token = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios/me");
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    [Theory]
    [InlineData("agente", "Agente")]
    [InlineData("Supervisor", "Supervisor")]
    [InlineData("Admin", "Admin")]
    [InlineData("Cliente", "Cliente")]
    public async Task Sessao_TokenValido_RetornaIdentidadeProvisionada(string papel, string esperado)
    {
        using var fabrica = new Fabrica();
        using var cliente = fabrica.CreateClient();
        using var resposta = await cliente.SendAsync(Requisicao(Token(papel)));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = await resposta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(12, dados.GetProperty("usuarioId").GetInt32());
        Assert.Equal(3, dados.GetProperty("grupoEmpresaId").GetInt32());
        Assert.Equal("Ana", dados.GetProperty("nome").GetString());
        Assert.Equal("ana@atos.com", dados.GetProperty("email").GetString());
        Assert.Equal("Grupo Norte", dados.GetProperty("grupoEmpresaNome").GetString());
        Assert.Equal(esperado, dados.GetProperty("papel").GetString());
        Assert.True(dados.GetProperty("expiraEm").GetDateTime() > dados.GetProperty("servidorEm").GetDateTime());
        Assert.Equal(DateTimeKind.Utc, dados.GetProperty("expiraEm").GetDateTime().Kind);
        Assert.True(resposta.Headers.CacheControl?.NoStore);
        Assert.Equal(1, fabrica.Provisionamento.Chamadas);
        Assert.False(dados.TryGetProperty("token", out _));
    }

    [Theory]
    [InlineData("ausente")]
    [InlineData("malformado")]
    [InlineData("expirado")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("assinatura")]
    [InlineData("claim")]
    [InlineData("futuro")]
    public async Task Sessao_TokenRejeitado_NaoProvisiona(string caso)
    {
        using var fabrica = new Fabrica();
        using var cliente = fabrica.CreateClient();
        using var outraChave = RSA.Create(2048);
        var token = caso switch
        {
            "ausente" => null,
            "malformado" => "token-invalido",
            _ => TestTokenFactory.CriarToken("portal-sessao", "ana@atos.com", "Ana", "grupo-1", "Grupo", "Agente",
                vida: caso == "expirado" ? TimeSpan.FromMinutes(-10) : TimeSpan.FromMinutes(5),
                issuer: caso == "issuer" ? "outro-emissor" : null,
                audience: caso == "audience" ? "outra-api" : null,
                chaveAssinatura: caso == "assinatura" ? new RsaSecurityKey(outraChave) : null,
                omitirClaim: caso == "claim" ? "email" : null,
                inicio: caso == "futuro" ? TimeSpan.FromMinutes(2) : null)
        };
        using var resposta = await cliente.SendAsync(Requisicao(token));
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(0, fabrica.Provisionamento.Chamadas);
    }

    [Fact]
    public async Task Sessao_ApiKey_NaoRetornaIdentidadeDeUsuario()
    {
        using var fabrica = new Fabrica();
        using var cliente = fabrica.CreateClient();
        using var req = Requisicao();
        req.Headers.Add("X-Api-Key", TestTokenFactory.AutomacaoApiKey);
        using var resposta = await cliente.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(0, fabrica.Provisionamento.Chamadas);
    }

    [Fact]
    public async Task Sessao_FalhaDeProvisionamento_NaoRetorna401()
    {
        using var fabrica = new Fabrica();
        fabrica.Provisionamento.Falhar = true;
        using var cliente = fabrica.CreateClient();
        using var resposta = await cliente.SendAsync(Requisicao(Token()));
        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
    }
}

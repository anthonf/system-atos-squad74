using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Integration.Tests;

public class SupportAtosApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _cliente;
    private readonly TestWebAppFactory _fabrica;

    public SupportAtosApiTests(TestWebAppFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static HttpContent CorpoChamado() => JsonContent.Create(new
    {
        produto = "Portal",
        categoria = "Login",
        mensagemInicial = "Não consigo acessar o sistema."
    });

    private HttpRequestMessage RequisicaoChamado(string? bearer = null, string? apiKey = null)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/chamados") { Content = CorpoChamado() };
        if (bearer is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (apiKey is not null)
        {
            requisicao.Headers.Add("X-Api-Key", apiKey);
        }

        return requisicao;
    }

    [Fact]
    public async Task HealthCheck_DeveRetornar200SemAutenticacao()
    {
        var resposta = await _cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_SemToken_DeveRetornar401()
    {
        var resposta = await _cliente.SendAsync(RequisicaoChamado());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_TokenInvalido_DeveRetornar401()
    {
        var resposta = await _cliente.SendAsync(RequisicaoChamado(bearer: "eyJhbGciOiJSUzI1NiJ9.inválido.falso"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_TokenExpirado_DeveRetornar401()
    {
        var token = TestTokenFactory.CriarToken(
            "portal-expirado", "expirado@atos.com", "Expirado", "grupo-1", "Grupo Norte", "agente",
            vida: TimeSpan.FromMinutes(-10));

        var resposta = await _cliente.SendAsync(RequisicaoChamado(bearer: token));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CadastrarUsuario_PapelInsuficiente_DeveRetornar403()
    {
        var token = TestTokenFactory.CriarToken(
            "portal-agente", "agente@atos.com", "Agente Fulano", "grupo-1", "Grupo Norte", "agente");

        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new { nome = "Novo", email = "novo@atos.com", grupoEmpresaId = 1, papel = "Cliente" })
        };
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_TokenValido_CriaChamadoProvisionaUsuarioEPermiteConsultar()
    {
        var token = TestTokenFactory.CriarToken(
            "portal-fulano", "fulano@atos.com", "Fulano", "grupo-1", "Grupo Norte", "agente");

        var criacao = await _cliente.SendAsync(RequisicaoChamado(bearer: token));

        Assert.True(criacao.StatusCode == HttpStatusCode.Created,
            $"Esperado Created, obtido {criacao.StatusCode}: {await criacao.Content.ReadAsStringAsync()}");

        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var codigoPublico = corpo.GetProperty("codigoPublico").GetString();

        using (var escopo = _fabrica.Services.CreateScope())
        {
            var dbContext = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
            var usuario = await dbContext.Usuarios.SingleAsync(u => u.CodigoExterno == "portal-fulano");
            Assert.Equal("Fulano", usuario.Nome);
            Assert.Equal("fulano@atos.com", usuario.Email);
            Assert.Equal("Agente", usuario.Papel.ToString());
            var grupo = await dbContext.GrupoEmpresas.SingleAsync(g => g.CodigoExterno == "grupo-1");
            Assert.Equal(usuario.GrupoEmpresaId, grupo.Id);
        }

        using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"/api/chamados/{codigoPublico}");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var consulta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var detalhe = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, detalhe.GetProperty("interacoes").GetArrayLength());
    }

    [Fact]
    public async Task CriarChamado_TokenRepetido_DeveSerIdempotenteNoProvisionamento()
    {
        var token = TestTokenFactory.CriarToken(
            "portal-repetido", "repetido@atos.com", "Repetido", "grupo-2", "Grupo Teste", "supervisor");

        var primeira = await _cliente.SendAsync(RequisicaoChamado(bearer: token));
        var segunda = await _cliente.SendAsync(RequisicaoChamado(bearer: token));

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var dbContext = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        var quantidade = await dbContext.Usuarios.CountAsync(u => u.CodigoExterno == "portal-repetido");
        Assert.Equal(1, quantidade);
        var auditorias = await dbContext.ProvisionamentoAudits.CountAsync(a => a.CodigoExternoUsuario == "portal-repetido");
        Assert.Equal(1, auditorias);
    }

    [Fact]
    public async Task ConsultarChamado_ClienteDeOutroGrupo_DeveRetornar404()
    {
        var tokenCriador = TestTokenFactory.CriarToken(
            "cliente-a", "clientea@atos.com", "Cliente A", "grupo-a", "Grupo A", "cliente");

        var criacao = await _cliente.SendAsync(RequisicaoChamado(bearer: tokenCriador));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var codigoPublico = (await criacao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigoPublico").GetString();

        var tokenOutroGrupo = TestTokenFactory.CriarToken(
            "cliente-b", "clienteb@atos.com", "Cliente B", "grupo-b", "Grupo B", "cliente");
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, $"/api/chamados/{codigoPublico}");
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenOutroGrupo);
        var resposta = await _cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_ApiKeyValida_AutenticaMasChamadosExigemUsuarioDoPortal()
    {
        var criacao = await _cliente.SendAsync(RequisicaoChamado(apiKey: TestTokenFactory.ApiKey));
        Assert.Equal(HttpStatusCode.Forbidden, criacao.StatusCode);

        var cadastro = new HttpRequestMessage(HttpMethod.Post, "/api/usuarios")
        {
            Content = JsonContent.Create(new { nome = "Novo", email = "novo@atos.com", grupoEmpresaId = 1, papel = "Cliente" })
        };
        cadastro.Headers.Add("X-Api-Key", TestTokenFactory.ApiKey);
        var respostaCadastro = await _cliente.SendAsync(cadastro);

        Assert.Equal(HttpStatusCode.Forbidden, respostaCadastro.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_ApiKeyInvalida_DeveRetornar401()
    {
        var resposta = await _cliente.SendAsync(RequisicaoChamado(apiKey: "chave-errada"));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
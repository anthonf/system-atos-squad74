using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;
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

    private static string NovoId(string prefixo) => $"{prefixo}-{Guid.NewGuid():N}";

    private static Dictionary<string, object?> CorpoPortal(
        string usuarioId,
        string grupoId,
        string email = "cliente@atos.com",
        string nome = "Cliente",
        string grupoNome = "Grupo Teste",
        string? titulo = "Erro ao acessar o portal",
        string produto = "Portal",
        string categoria = "Login",
        string mensagem = "Não consigo acessar o sistema.")
    {
        return new Dictionary<string, object?>
        {
            ["usuario"] = new Dictionary<string, object?>
            {
                ["id"] = usuarioId,
                ["nome"] = nome,
                ["email"] = email
            },
            ["grupoEmpresa"] = new Dictionary<string, object?>
            {
                ["id"] = grupoId,
                ["nome"] = grupoNome
            },
            ["produto"] = produto,
            ["categoria"] = categoria,
            ["titulo"] = titulo,
            ["mensagemInicial"] = mensagem
        };
    }

    private static HttpRequestMessage Requisicao(
        HttpMethod metodo,
        string caminho,
        object? corpo = null,
        string? apiKey = null,
        string? usuarioId = null,
        string? grupoId = null,
        string? bearer = null)
    {
        var requisicao = new HttpRequestMessage(metodo, caminho);
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        if (apiKey is not null)
        {
            requisicao.Headers.Add("X-Api-Key", apiKey);
        }

        if (usuarioId is not null)
        {
            requisicao.Headers.Add("X-Portal-User-Id", usuarioId);
        }

        if (grupoId is not null)
        {
            requisicao.Headers.Add("X-Portal-Group-Id", grupoId);
        }

        if (bearer is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return requisicao;
    }

    private async Task<(int UsuarioId, int GrupoId)> SemearUsuarioAsync(
        string codigoUsuario,
        string email,
        string nome,
        PapelUsuario papel,
        string codigoGrupo,
        string nomeGrupo)
    {
        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();

        var grupo = await db.GrupoEmpresas.FirstOrDefaultAsync(g => g.CodigoExterno == codigoGrupo);
        if (grupo is null)
        {
            grupo = GrupoEmpresa.Criar(nomeGrupo);
            grupo.AtribuirCodigoExterno(codigoGrupo);
            db.GrupoEmpresas.Add(grupo);
            await db.SaveChangesAsync();
        }

        var usuario = Usuario.Criar(nome, email, grupo.Id, papel);
        usuario.AtribuirCodigoExterno(codigoUsuario);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return (usuario.Id, grupo.Id);
    }

    [Fact]
    public async Task HealthCheck_DeveRetornar200SemAutenticacao()
    {
        var resposta = await _cliente.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_SemApiKey_DeveRetornar401()
    {
        var corpo = CorpoPortal(NovoId("portal"), NovoId("grupo"));
        using var resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, "/api/chamados", corpo));
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_ApiKeyInvalida_DeveRetornar401()
    {
        var corpo = CorpoPortal(NovoId("portal"), NovoId("grupo"));
        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: "chave-invalida"));
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_ApiKeyAutomacao_DeveRetornar403()
    {
        var corpo = CorpoPortal(NovoId("portal"), NovoId("grupo"));
        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.AutomacaoApiKey));
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarChamado_NovoUsuarioEGrupo_DeveCriarComoCliente()
    {
        var codigoUsuario = NovoId("portal");
        var codigoGrupo = NovoId("grupo");
        var email = $"{codigoUsuario}@atos.com";
        var corpo = CorpoPortal(codigoUsuario, codigoGrupo, email, "Novo Cliente", "Novo Grupo");

        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(criado.GetProperty("codigoPublico").GetString()));

        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        var usuario = await db.Usuarios.SingleAsync(u => u.CodigoExterno == codigoUsuario);
        Assert.Equal(PapelUsuario.Cliente, usuario.Papel);
        Assert.Equal("Novo Cliente", usuario.Nome);
        Assert.Equal(email, usuario.Email);
        Assert.True(await db.GrupoEmpresas.AnyAsync(g => g.CodigoExterno == codigoGrupo && g.Nome == "Novo Grupo"));
    }

    [Fact]
    public async Task CriarChamado_Repetido_NaoDuplicaUsuarioNemAuditoria()
    {
        var codigoUsuario = NovoId("portal-repetido");
        var codigoGrupo = NovoId("grupo-repetido");
        var corpo = CorpoPortal(codigoUsuario, codigoGrupo, $"{codigoUsuario}@atos.com");

        using var primeira = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));
        using var segunda = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        Assert.Equal(1, await db.Usuarios.CountAsync(u => u.CodigoExterno == codigoUsuario));
        Assert.Equal(1, await db.ProvisionamentoAudits.CountAsync(a => a.CodigoExternoUsuario == codigoUsuario));
    }

    [Fact]
    public async Task CriarChamado_NovoUsuarioComGrupoExistente_ReutilizaGrupo()
    {
        var codigoGrupo = NovoId("grupo-compartilhado");
        var usuarioA = NovoId("portal-a");
        var usuarioB = NovoId("portal-b");

        using var primeira = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/chamados",
            CorpoPortal(usuarioA, codigoGrupo, $"{usuarioA}@atos.com", "Cliente A", "Grupo Compartilhado"),
            apiKey: TestTokenFactory.PortalApiKey));
        using var segunda = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/chamados",
            CorpoPortal(usuarioB, codigoGrupo, $"{usuarioB}@atos.com", "Cliente B", "Grupo Compartilhado"),
            apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        Assert.Equal(1, await db.GrupoEmpresas.CountAsync(g => g.CodigoExterno == codigoGrupo));
        Assert.Equal(2, await db.Usuarios.CountAsync(u => u.CodigoExterno == usuarioA || u.CodigoExterno == usuarioB));
    }

    [Fact]
    public async Task CriarChamado_CampoPapelNoCorpo_DeveRetornar400()
    {
        var corpo = CorpoPortal(NovoId("portal"), NovoId("grupo"));
        corpo["papel"] = "Admin";

        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("papel", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CriarChamado_CodigoExternoDeAgente_DeveRetornar409()
    {
        var codigoAgente = NovoId("agente");
        var codigoGrupo = NovoId("grupo-agente");
        var email = $"{codigoAgente}@atos.com";
        await SemearUsuarioAsync(codigoAgente, email, "Agente Existente", PapelUsuario.Agente, codigoGrupo, "Grupo Agente");

        var corpo = CorpoPortal(codigoAgente, codigoGrupo, email, "Agente Existente");
        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        var usuario = await db.Usuarios.SingleAsync(u => u.CodigoExterno == codigoAgente);
        Assert.Equal(PapelUsuario.Agente, usuario.Papel);
    }

    [Fact]
    public async Task CriarChamado_EmailDeOutroUsuario_DeveRetornar409()
    {
        var codigoExistente = NovoId("portal-existente");
        var codigoGrupo = NovoId("grupo-existente");
        var email = $"{codigoExistente}@atos.com";
        await SemearUsuarioAsync(codigoExistente, email, "Cliente Existente", PapelUsuario.Cliente, codigoGrupo, "Grupo Existente");

        var corpo = CorpoPortal(NovoId("portal-novo"), NovoId("grupo-novo"), email, "Cliente Novo");
        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CriarChamado_SemTitulo_DeveRetornar400(string? titulo)
    {
        var corpo = CorpoPortal(NovoId("portal-titulo"), NovoId("grupo-titulo"), titulo: titulo);

        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var erro = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("título", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CriarChamado_ComTitulo_DeveApararTitulo()
    {
        var codigoUsuario = NovoId("portal-titulo-ok");
        var codigoGrupo = NovoId("grupo-titulo-ok");
        var corpo = CorpoPortal(codigoUsuario, codigoGrupo, $"{codigoUsuario}@atos.com", titulo: "  Erro ao acessar o portal  ");

        using var resposta = await _cliente.SendAsync(
            Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        using var escopo = _fabrica.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        var chamado = await db.Chamados.OrderByDescending(c => c.Id).FirstAsync(c => c.Usuario!.CodigoExterno == codigoUsuario);
        Assert.Equal("Erro ao acessar o portal", chamado.Titulo);
    }

    [Fact]
    public async Task CriarChamado_ValidacoesDeCampos_DevemRetornar400()
    {
        var casos = new List<Dictionary<string, object?>>
        {
            CorpoPortal(NovoId("portal"), NovoId("grupo"), email: "email-invalido"),
            CorpoPortal(NovoId("portal"), NovoId("grupo"), nome: new string('a', 151)),
            CorpoPortal(NovoId("portal"), NovoId("grupo"), grupoNome: new string('g', 151)),
            CorpoPortal(NovoId("portal"), NovoId("grupo"), produto: new string('p', 101)),
            CorpoPortal(NovoId("portal"), NovoId("grupo"), categoria: new string('c', 101)),
            CorpoPortal(NovoId("portal"), NovoId("grupo"), titulo: new string('t', 201)),
            CorpoPortal("id com espaço", NovoId("grupo")),
            CorpoPortal(NovoId("portal"), new string('x', 101))
        };

        foreach (var corpo in casos)
        {
            using var resposta = await _cliente.SendAsync(
                Requisicao(HttpMethod.Post, "/api/chamados", corpo, apiKey: TestTokenFactory.PortalApiKey));
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }
    }

    [Fact]
    public async Task ConsultarChamado_ClienteDeOutroGrupo_DeveRetornar404()
    {
        var usuarioA = NovoId("cliente-a");
        var grupoA = NovoId("grupo-a");
        var usuarioB = NovoId("cliente-b");
        var grupoB = NovoId("grupo-b");

        using var criacao = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/chamados",
            CorpoPortal(usuarioA, grupoA, $"{usuarioA}@atos.com", "Cliente A", "Grupo A"),
            apiKey: TestTokenFactory.PortalApiKey));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var codigoPublico = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigoPublico").GetString();

        using var criacaoB = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/chamados",
            CorpoPortal(usuarioB, grupoB, $"{usuarioB}@atos.com", "Cliente B", "Grupo B"),
            apiKey: TestTokenFactory.PortalApiKey));
        Assert.Equal(HttpStatusCode.Created, criacaoB.StatusCode);

        using var consulta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            $"/api/chamados/{codigoPublico}",
            apiKey: TestTokenFactory.PortalApiKey,
            usuarioId: usuarioB,
            grupoId: grupoB));

        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task ConsultarChamado_ClienteNaoVeNotaInterna()
    {
        var codigoUsuario = NovoId("cliente-nota");
        var codigoGrupo = NovoId("grupo-nota");
        var email = $"{codigoUsuario}@atos.com";

        using var criacao = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/chamados",
            CorpoPortal(codigoUsuario, codigoGrupo, email, "Cliente Nota", "Grupo Nota"),
            apiKey: TestTokenFactory.PortalApiKey));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var codigoPublico = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("codigoPublico").GetString();

        var tokenAgente = TestTokenFactory.CriarToken(
            NovoId("agente-nota"), $"agente-{Guid.NewGuid():N}@atos.com", "Agente Nota",
            NovoId("grupo-agente-nota"), "Grupo Agente", "agente");

        using var nota = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            $"/api/chamados/{codigoPublico}/interacoes",
            new { mensagem = "Nota interna da equipe.", tipo = "NotaInterna" },
            bearer: tokenAgente));
        Assert.Equal(HttpStatusCode.Created, nota.StatusCode);

        using var consulta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            $"/api/chamados/{codigoPublico}",
            apiKey: TestTokenFactory.PortalApiKey,
            usuarioId: codigoUsuario,
            grupoId: codigoGrupo));
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        var detalhe = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        var interacoes = detalhe.GetProperty("interacoes").EnumerateArray().ToList();
        Assert.Single(interacoes);
        Assert.DoesNotContain(interacoes, i => i.GetProperty("mensagem").GetString() == "Nota interna da equipe.");
    }

    [Fact]
    public async Task WebhookTest_ComApiKeyAutomacao_DeveRetornar202()
    {
        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/webhooks/test",
            new { evento = "chamado.criado" },
            apiKey: TestTokenFactory.AutomacaoApiKey));

        Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
    }

    [Fact]
    public async Task WebhookTest_ComApiKeyPortal_DeveRetornar403()
    {
        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/webhooks/test",
            new { evento = "chamado.criado" },
            apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task WebhookTest_SemApiKey_DeveRetornar401()
    {
        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Post,
            "/api/webhooks/test",
            new { evento = "chamado.criado" }));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Automacao_ListarChamados_ComApiKeyAutomacao_DeveRetornar200()
    {
        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            "/api/automacao/chamados",
            apiKey: TestTokenFactory.AutomacaoApiKey));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Automacao_ComApiKeyPortal_DeveRetornar403()
    {
        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            "/api/automacao/chamados",
            apiKey: TestTokenFactory.PortalApiKey));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Equipe_SemToken_DeveRetornar401()
    {
        using var resposta = await _cliente.GetAsync("/api/usuarios?grupoEmpresaId=1");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Equipe_ComTokenCliente_DeveRetornar403()
    {
        var token = TestTokenFactory.CriarToken(
            NovoId("cliente-equipe"), $"cliente-{Guid.NewGuid():N}@atos.com", "Cliente Equipe",
            NovoId("grupo-cliente-equipe"), "Grupo Cliente", "cliente");

        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            "/api/usuarios?grupoEmpresaId=1",
            bearer: token));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Equipe_ComTokenAgente_DeveRetornar200()
    {
        var token = TestTokenFactory.CriarToken(
            NovoId("agente-equipe"), $"agente-{Guid.NewGuid():N}@atos.com", "Agente Equipe",
            NovoId("grupo-agente-equipe"), "Grupo Agente", "agente");

        using var resposta = await _cliente.SendAsync(Requisicao(
            HttpMethod.Get,
            "/api/usuarios?grupoEmpresaId=1",
            bearer: token));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}

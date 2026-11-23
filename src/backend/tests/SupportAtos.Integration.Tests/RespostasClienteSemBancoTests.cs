using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Integration.Tests;

// Pipeline HTTP e JWT reais; persistencia e provisionamento isolados do PostgreSQL.
public class RespostasClienteSemBancoTests
{
    public class Repository : DispatchProxy
    {
        public FilaChamadosQuery? UltimoFiltro;
        public Chamado Chamado = Chamado.Criar("SA-1", 1, 1,
            SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Teste");

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "ListarFilaAsync")
            {
                UltimoFiltro = (FilaChamadosQuery)args![0]!;
                return Task.FromResult<IReadOnlyList<Chamado>>(UltimoFiltro.GrupoEmpresaId == Chamado.GrupoEmpresaId ? [Chamado] : []);
            }
            return method.Name switch
            {
                "GetByCodigoPublicoComHistoricoAsync" or "GetByCodigoPublicoAsync" => Task.FromResult<Chamado?>(Chamado),
                _ => throw new InvalidOperationException("Consulta inesperada: " + method.Name)
            };
        }
    }

    public class SlaRepository : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name == "ListarAtivasAsync"
            ? Task.FromResult<IReadOnlyList<SlaCategoria>>([SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4)])
            : throw new InvalidOperationException("Consulta inesperada: " + method.Name);
    }

    private sealed class Provisioning : IUserProvisioningService
    {
        public Task<ProvisioningResult> ProvisionAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProvisioningResult(3, int.Parse(usuario.CodigoExternoGrupo)));
        public Task<ProvisioningResult> ProvisionPortalAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
            => ProvisionAsync(usuario, cancellationToken);
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int Gravacoes;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Gravacoes++;
            return Task.FromResult(1);
        }
    }

    private sealed class Webhook : IWebhookDispatcher
    {
        public Task EnviarAsync(WebhookEvent evento, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static WebApplicationFactory<Program> CriarFactory(IChamadoRepository repo, UnitOfWork unitOfWork)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=127.0.0.1;Port=1;Database=never_used;Username=synthetic;Password=synthetic");
            builder.UseSetting("Sso:Issuer", TestTokenFactory.Issuer);
            builder.UseSetting("Sso:Audience", TestTokenFactory.Audience);
            builder.UseSetting("Sso:PublicKeyPem", TestTokenFactory.ChavePublicaPem);
            builder.UseSetting("Auth:Issuer", TestTokenFactory.Issuer);
            builder.UseSetting("Auth:Audience", TestTokenFactory.Audience);
            builder.UseSetting("Auth:PrivateKeyPem", TestTokenFactory.ChavePrivadaPem);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChamadoRepository>();
                services.AddSingleton(repo);
                services.RemoveAll<ISlaCategoriaRepository>();
                services.AddSingleton(DispatchProxy.Create<ISlaCategoriaRepository, SlaRepository>());
                services.RemoveAll<IUserProvisioningService>();
                services.AddSingleton<IUserProvisioningService>(new Provisioning());
                services.RemoveAll<IUnitOfWork>();
                services.AddSingleton<IUnitOfWork>(unitOfWork);
                services.RemoveAll<IWebhookDispatcher>();
                services.AddSingleton<IWebhookDispatcher>(new Webhook());
            });
        });
    }

    [Theory]
    [InlineData(null, "Publica", HttpStatusCode.Unauthorized)]
    [InlineData("1", "Publica", HttpStatusCode.Created)]
    [InlineData("2", "Publica", HttpStatusCode.NotFound)]
    [InlineData("1", "NotaInterna", HttpStatusCode.Forbidden)]
    [InlineData("1", "Publica", HttpStatusCode.BadRequest, true)]
    public async Task Responder_ExigeAutenticacaoGrupoETipoPermitido(string? grupo, string tipo, HttpStatusCode esperado, bool fechado = false)
    {
        var repo = DispatchProxy.Create<IChamadoRepository, Repository>();
        var chamado = ((Repository)repo).Chamado;
        if (fechado) chamado.MudarStatus(StatusChamado.Fechado, DateTime.UtcNow);
        var unitOfWork = new UnitOfWork();
        using var factory = CriarFactory(repo, unitOfWork);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        if (grupo is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                TestTokenFactory.CriarToken("cliente", "cliente@test.invalid", "Cliente", grupo, "Empresa", "cliente"));

        using var response = await client.PostAsJsonAsync("/api/chamados/SA-1/interacoes", new { mensagem = "Resposta do cliente", tipo });
        Assert.Equal(esperado, response.StatusCode);
        Assert.Equal(esperado == HttpStatusCode.Created ? 1 : 0, unitOfWork.Gravacoes);
        using var consultaGrupo = await client.GetAsync("/api/chamados/SA-1");
        Assert.Equal(grupo is null ? HttpStatusCode.Unauthorized : grupo == "2" ? HttpStatusCode.NotFound : HttpStatusCode.OK, consultaGrupo.StatusCode);
        if (grupo is not null)
        {
            using var statusGrupo = await client.PatchAsJsonAsync("/api/chamados/SA-1/status", new { novoStatus = "Aberto" });
            Assert.Equal(grupo == "2" ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden, statusGrupo.StatusCode);
            using var fila = await client.GetAsync("/api/chamados?grupoEmpresaId=999");
            Assert.Equal(HttpStatusCode.OK, fila.StatusCode);
            Assert.Equal(int.Parse(grupo), ((Repository)repo).UltimoFiltro!.GrupoEmpresaId);
            using var categorias = await client.GetAsync("/api/sla-categorias");
            Assert.Equal(HttpStatusCode.OK, categorias.StatusCode);
        }
        if (esperado == HttpStatusCode.Created)
        {
            Assert.Equal("Resposta do cliente", Assert.Single(chamado.Interacoes).Mensagem);
            chamado.AdicionarInteracao(2, TipoInteracao.NotaInterna, "Nota sigilosa", DateTime.UtcNow);
            using var consulta = await client.GetAsync("/api/chamados/SA-1");
            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
            var detalhe = await consulta.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Resposta do cliente", Assert.Single(detalhe.GetProperty("interacoes").EnumerateArray()).GetProperty("mensagem").GetString());
            using var status = await client.PatchAsJsonAsync("/api/chamados/SA-1/status", new { novoStatus = "Fechado" });
            Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
            using var interno = await client.PostAsJsonAsync("/api/chamados/interno", new { });
            Assert.Equal(HttpStatusCode.Forbidden, interno.StatusCode);
        }
        else Assert.Empty(chamado.Interacoes);
        Assert.Equal(fechado ? StatusChamado.Fechado : StatusChamado.Aberto, chamado.Status);
        Assert.Equal(esperado == HttpStatusCode.Created ? 1 : 0, unitOfWork.Gravacoes);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expirado")]
    [InlineData("assinatura")]
    [InlineData("grupo_ausente")]
    public async Task Responder_ComJwtInvalido_DeveRetornar401SemGravar(string falha)
    {
        var repo = DispatchProxy.Create<IChamadoRepository, Repository>();
        var unitOfWork = new UnitOfWork();
        using var factory = CriarFactory(repo, unitOfWork);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var outraChave = RSA.Create(2048);
        var token = TestTokenFactory.CriarToken("cliente", "cliente@test.invalid", "Cliente", "1", "Empresa", "cliente",
            issuer: falha == "issuer" ? "nao-confiavel" : null,
            audience: falha == "audience" ? "outra-api" : null,
            vida: falha == "expirado" ? TimeSpan.FromMinutes(-10) : null,
            chaveAssinatura: falha == "assinatura" ? new RsaSecurityKey(outraChave) : null,
            omitirClaim: falha == "grupo_ausente" ? "grupo_empresa_id" : null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.PostAsJsonAsync("/api/chamados/SA-1/interacoes", new { mensagem = "Resposta", tipo = "Publica" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, unitOfWork.Gravacoes);
        Assert.Empty(((Repository)repo).Chamado.Interacoes);
    }
}

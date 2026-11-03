using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Services;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class UserProvisioningServiceTests
{
    private readonly ServiceProvider _provider;
    private readonly FakeUsuarioRepository _usuarios = new();
    private readonly FakeGrupoEmpresaRepository _grupos = new();
    private readonly FakeProvisionamentoAuditRepository _auditoria = new();
    private readonly FakeUniqueConstraintViolationDetector _detector = new();

    public UserProvisioningServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUsuarioRepository>(_usuarios);
        services.AddSingleton<IGrupoEmpresaRepository>(_grupos);
        services.AddSingleton<IProvisionamentoAuditRepository>(_auditoria);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IUniqueConstraintViolationDetector>(_detector);
        services.AddMemoryCache();
        _provider = services.BuildServiceProvider();
    }

    private UserProvisioningService CriarServico(IMemoryCache? cache = null)
        => new(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _detector,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            cacheTtl: TimeSpan.FromSeconds(60));

    private static PortalUserContext Contexto(string codigoUsuario = "portal-1", string codigoGrupo = "grupo-1")
        => new(0, 0, codigoUsuario, codigoGrupo, "joao@atos.com", "João Silva", "Grupo Norte", PapelUsuario.Agente);

    [Fact]
    public async Task ProvisionAsync_NovoUsuarioEGrupo_DeveCriarPersistirEAuditarCriacao()
    {
        var servico = CriarServico();

        var resultado = await servico.ProvisionAsync(Contexto());

        var usuario = Assert.Single(_usuarios.Usuarios);
        Assert.Equal("portal-1", usuario.CodigoExterno);
        Assert.Equal("João Silva", usuario.Nome);
        Assert.Equal(PapelUsuario.Agente, usuario.Papel);
        Assert.Equal(resultado.UsuarioIdInterno, usuario.Id);
        Assert.Equal(resultado.GrupoEmpresaIdInterno, usuario.GrupoEmpresaId);
        Assert.Equal("grupo-1", _grupos.Grupos.Single(g => g.CodigoExterno == "grupo-1").CodigoExterno);

        var auditoria = Assert.Single(_auditoria.Auditorias);
        Assert.Equal("Criado", auditoria.Acao);
    }

    [Fact]
    public async Task ProvisionAsync_IdempotenteSemMudancas_DeveRetornarSemPersistirOuAuditar()
    {
        var usuario = Usuario.Criar("João Silva", "joao@atos.com", 20, PapelUsuario.Agente);
        usuario.AtribuirCodigoExterno("portal-1");
        usuario.SetIdForTest(10);
        await _usuarios.AddAsync(usuario);
        var grupo = GrupoEmpresa.Criar("Grupo Norte");
        grupo.AtribuirCodigoExterno("grupo-1");
        grupo.SetIdForTest(20);
        await _grupos.AddAsync(grupo);

        var servico = CriarServico();
        var resultado = await servico.ProvisionAsync(Contexto());

        Assert.Equal(10, resultado.UsuarioIdInterno);
        Assert.Equal(20, resultado.GrupoEmpresaIdInterno);
        Assert.Single(_usuarios.Usuarios);
        Assert.Empty(_auditoria.Auditorias);
        Assert.Equal(2, _grupos.Grupos.Count);
    }

    [Fact]
    public async Task ProvisionAsync_UsuarioExistenteComDiferencas_DeveAtualizarEAuditar()
    {
        var usuario = Usuario.Criar("Nome Antigo", "antigo@atos.com", 1, PapelUsuario.Cliente);
        usuario.AtribuirCodigoExterno("portal-1");
        usuario.SetIdForTest(10);
        await _usuarios.AddAsync(usuario);
        var grupo = GrupoEmpresa.Criar("Grupo Norte");
        grupo.AtribuirCodigoExterno("grupo-1");
        grupo.SetIdForTest(20);
        await _grupos.AddAsync(grupo);

        var servico = CriarServico();
        var resultado = await servico.ProvisionAsync(Contexto());

        Assert.Equal(10, resultado.UsuarioIdInterno);
        Assert.Equal("João Silva", usuario.Nome);
        Assert.Equal("joao@atos.com", usuario.Email);
        Assert.Equal(PapelUsuario.Agente, usuario.Papel);
        var auditoria = Assert.Single(_auditoria.Auditorias);
        Assert.Equal("Atualizado", auditoria.Acao);
    }

    [Fact]
    public async Task ProvisionAsync_ViolacaoDeUnicidadeConcorrente_DeveReexecutarERetornarEstadoPersistido()
    {
        var repositorioGrupo = new GrupoEmpresaRepositoryConcorrente(_usuarios);
        var services = new ServiceCollection();
        services.AddSingleton<IUsuarioRepository>(_usuarios);
        services.AddSingleton<IGrupoEmpresaRepository>(repositorioGrupo);
        services.AddSingleton<IProvisionamentoAuditRepository>(_auditoria);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IUniqueConstraintViolationDetector>(new FakeUniqueConstraintViolationDetector(ex => ex is UniqueViolationSimException));
        services.AddMemoryCache();
        using var provider = services.BuildServiceProvider();

        var servico = new UserProvisioningService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IUniqueConstraintViolationDetector>(),
            provider.GetRequiredService<IMemoryCache>());

        var resultado = await servico.ProvisionAsync(Contexto());

        Assert.Equal(50, resultado.UsuarioIdInterno);
        Assert.Equal(60, resultado.GrupoEmpresaIdInterno);
        Assert.Equal(1, repositorioGrupo.ChamadasAdd);
        Assert.Empty(_auditoria.Auditorias);
    }

    [Fact]
    public async Task ProvisionAsync_ChamadasRepetidas_DeveUsarCacheEAcumularAuditoriaSoDaPrimeira()
    {
        var servico = CriarServico();
        var primeiro = await servico.ProvisionAsync(Contexto());
        var segundo = await servico.ProvisionAsync(Contexto());

        Assert.Equal(primeiro, segundo);
        Assert.Single(_usuarios.Usuarios);
        Assert.Single(_auditoria.Auditorias);
    }

    private sealed class GrupoEmpresaRepositoryConcorrente : IGrupoEmpresaRepository
    {
        private readonly FakeUsuarioRepository _usuariosCompartilhados;
        private GrupoEmpresa? _grupoPersistido;

        public GrupoEmpresaRepositoryConcorrente(FakeUsuarioRepository usuariosCompartilhados)
        {
            _usuariosCompartilhados = usuariosCompartilhados;
        }

        public int ChamadasAdd { get; private set; }

        public Task<GrupoEmpresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_grupoPersistido);

        public Task<GrupoEmpresa?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default)
            => Task.FromResult(_grupoPersistido);

        public Task AddAsync(GrupoEmpresa grupoEmpresa, CancellationToken cancellationToken = default)
        {
            ChamadasAdd++;
            var grupo = GrupoEmpresa.Criar(grupoEmpresa.Nome);
            grupo.AtribuirCodigoExterno(grupoEmpresa.CodigoExterno!);
            grupo.SetIdForTest(60);
            var usuario = Usuario.Criar("João Silva", "joao@atos.com", 60, PapelUsuario.Agente);
            usuario.AtribuirCodigoExterno("portal-1");
            usuario.SetIdForTest(50);
            _usuariosCompartilhados.AddAsync(usuario).GetAwaiter().GetResult();

            _grupoPersistido = grupo;
            throw new UniqueViolationSimException();
        }
    }

    private sealed class UniqueViolationSimException : Exception
    {
    }
}
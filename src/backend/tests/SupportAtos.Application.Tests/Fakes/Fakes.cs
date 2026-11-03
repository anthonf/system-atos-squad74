using SupportAtos.Application.Abstractions;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests.Fakes;

public class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();

    public IReadOnlyList<Usuario> Usuarios => _usuarios;

    public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task<Usuario?> GetComGrupoEmpresaPorIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<Usuario>> ListarPorGrupoEmpresaAsync(int grupoEmpresaId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.Where(u => u.GrupoEmpresaId == grupoEmpresaId).OrderBy(u => u.Nome).ToList());

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.Email == email));

    public Task<Usuario?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.FirstOrDefault(u => u.CodigoExterno == codigoExterno));

    public Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(_usuarios.Any(u => u.Email == email));

    public Task<IReadOnlyList<Usuario>> ListarAgentesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Usuario>>(_usuarios
            .Where(u => u.Papel is PapelUsuario.Agente or PapelUsuario.Supervisor or PapelUsuario.Admin)
            .OrderBy(u => u.Id)
            .ToList());

    public Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        if (usuario.Id == 0)
        {
            usuario.SetIdForTest(_usuarios.Count + 1);
        }

        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }
}

public class FakeGrupoEmpresaRepository : IGrupoEmpresaRepository
{
    private readonly List<GrupoEmpresa> _grupos = new() { GrupoEmpresa.Criar("Empresa Demo") };

    public FakeGrupoEmpresaRepository()
    {
        _grupos[0].SetIdForTest(1);
    }

    public IReadOnlyList<GrupoEmpresa> Grupos => _grupos;

    public Task<GrupoEmpresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_grupos.FirstOrDefault(g => g.Id == id));

    public Task<GrupoEmpresa?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default)
        => Task.FromResult(_grupos.FirstOrDefault(g => g.CodigoExterno == codigoExterno));

    public Task AddAsync(GrupoEmpresa grupoEmpresa, CancellationToken cancellationToken = default)
    {
        if (grupoEmpresa.Id == 0)
        {
            grupoEmpresa.SetIdForTest(_grupos.Count + 1);
        }

        _grupos.Add(grupoEmpresa);
        return Task.CompletedTask;
    }
}

public class FakeProvisionamentoAuditRepository : IProvisionamentoAuditRepository
{
    private readonly List<ProvisionamentoAudit> _auditorias = new();

    public IReadOnlyList<ProvisionamentoAudit> Auditorias => _auditorias;

    public Task AddAsync(ProvisionamentoAudit auditoria, CancellationToken cancellationToken = default)
    {
        _auditorias.Add(auditoria);
        return Task.CompletedTask;
    }
}

public class FakeUniqueConstraintViolationDetector : IUniqueConstraintViolationDetector
{
    private readonly Func<Exception, bool> _quandoForViolacao;

    public FakeUniqueConstraintViolationDetector(Func<Exception, bool>? quandoForViolacao = null)
    {
        _quandoForViolacao = quandoForViolacao ?? (_ => false);
    }

    public bool IsUniqueViolation(Exception exception) => _quandoForViolacao(exception);
}

public class FakeSlaCategoriaRepository : ISlaCategoriaRepository
{
    private readonly List<SlaCategoria> _slas = new()
    {
        SlaCategoria.Criar("Portal", "Login", Domain.Enums.Prioridade.Alta, 1, 4)
    };

    public Task<SlaCategoria?> GetByProdutoECategoriaAsync(string produto, string categoria, CancellationToken cancellationToken = default)
        => Task.FromResult(_slas.FirstOrDefault(s => s.Produto == produto && s.Categoria == categoria));
}

public class FakeChamadoRepository : IChamadoRepository
{
    private readonly List<Chamado> _chamados = new();

    public IReadOnlyList<Chamado> Chamados => _chamados;

    public Task<Chamado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_chamados.FirstOrDefault(c => c.Id == id));

    public Task<Chamado?> GetByCodigoPublicoAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => Task.FromResult(_chamados.FirstOrDefault(c => c.CodigoPublico == codigoPublico));

    public Task<Chamado?> GetByCodigoPublicoComHistoricoAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => Task.FromResult(_chamados.FirstOrDefault(c => c.CodigoPublico == codigoPublico));

    public Task<bool> CodigoPublicoExisteAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => Task.FromResult(_chamados.Any(c => c.CodigoPublico == codigoPublico));

    public Task<IReadOnlyList<Chamado>> ListarFilaAsync(DTOs.FilaChamadosQuery filtro, CancellationToken cancellationToken = default)
    {
        var query = _chamados.AsQueryable();

        if (filtro.Status.HasValue)
        {
            query = query.Where(c => c.Status == filtro.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            query = query.Where(c => c.Categoria == filtro.Categoria);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Produto))
        {
            query = query.Where(c => c.Produto == filtro.Produto);
        }

        if (filtro.AgenteId.HasValue)
        {
            query = query.Where(c => c.AgenteId == filtro.AgenteId.Value);
        }

        if (filtro.GrupoEmpresaId.HasValue)
        {
            query = query.Where(c => c.GrupoEmpresaId == filtro.GrupoEmpresaId.Value);
        }

        if (filtro.SemAgente == true)
        {
            query = query.Where(c => c.AgenteId == null);
        }

        return Task.FromResult<IReadOnlyList<Chamado>>(query.OrderBy(c => c.CriadoEm).ToList());
    }

    public Task<int> ContarAbertosPorAgenteAsync(int agenteId, CancellationToken cancellationToken = default)
        => Task.FromResult(_chamados.Count(c =>
            c.AgenteId == agenteId && (c.Status == StatusChamado.Aberto || c.Status == StatusChamado.EmAndamento)));

    public Task AddAsync(Chamado chamado, CancellationToken cancellationToken = default)
    {
        _chamados.Add(chamado);
        return Task.CompletedTask;
    }
}

public class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(1);
}

public class FakeCurrentUser : ICurrentUser
{
    public static readonly PortalUserContext ContextoPadrao = new(
        UsuarioIdInterno: 1,
        GrupoEmpresaIdInterno: 1,
        CodigoExternoUsuario: "portal-1",
        CodigoExternoGrupo: "grupo-1",
        Email: "usuario@atos.com",
        Nome: "Usuário Demo",
        NomeGrupo: "Grupo Demo",
        Papel: PapelUsuario.Agente);

    public PortalUserContext Usuario { get; init; } = ContextoPadrao;

    public bool IsAutenticado => true;

    public PortalUserContext UsuarioAtual => Usuario;
}

public static class TestExtensions
{
    public static void SetIdForTest<T>(this T entity, int id)
        where T : class
    {
        var property = typeof(T).GetProperty("Id");
        property!.SetValue(entity, id);
    }

    public static void SetForTest<T>(this T entity, string propertyName, object? value)
        where T : class
    {
        typeof(T).GetProperty(propertyName)!.SetValue(entity, value);
    }
}
using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.Ports;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> GetComGrupoEmpresaPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarPorGrupoEmpresaAsync(int grupoEmpresaId, CancellationToken cancellationToken = default);

    Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Usuario?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarAgentesAsync(CancellationToken cancellationToken = default);

    Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
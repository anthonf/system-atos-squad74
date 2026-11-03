using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.Ports;

public interface IGrupoEmpresaRepository
{
    Task<GrupoEmpresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<GrupoEmpresa?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default);

    Task AddAsync(GrupoEmpresa grupoEmpresa, CancellationToken cancellationToken = default);
}
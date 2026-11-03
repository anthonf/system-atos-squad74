using SupportAtos.Application.DTOs;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Ports;

public interface IChamadoRepository
{
    Task<Chamado?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Chamado?> GetByCodigoPublicoAsync(string codigoPublico, CancellationToken cancellationToken = default);

    Task<Chamado?> GetByCodigoPublicoComHistoricoAsync(string codigoPublico, CancellationToken cancellationToken = default);

    Task<bool> CodigoPublicoExisteAsync(string codigoPublico, CancellationToken cancellationToken = default);

    Task AddAsync(Chamado chamado, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chamado>> ListarFilaAsync(
        FilaChamadosQuery filtro,
        CancellationToken cancellationToken = default);

    Task<int> ContarAbertosPorAgenteAsync(int agenteId, CancellationToken cancellationToken = default);
}

using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.Ports;

public interface ISlaCategoriaRepository
{
    Task<SlaCategoria?> GetByProdutoECategoriaAsync(string produto, string categoria, CancellationToken cancellationToken = default);
}
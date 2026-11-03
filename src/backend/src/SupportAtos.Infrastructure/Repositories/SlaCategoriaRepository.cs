using Microsoft.EntityFrameworkCore;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class SlaCategoriaRepository : ISlaCategoriaRepository
{
    private readonly SupportAtosDbContext _context;

    public SlaCategoriaRepository(SupportAtosDbContext context)
    {
        _context = context;
    }

    public Task<SlaCategoria?> GetByProdutoECategoriaAsync(string produto, string categoria, CancellationToken cancellationToken = default)
        => _context.SlaCategorias
            .Where(s => s.Produto == produto && s.Categoria == categoria)
            .OrderByDescending(s => s.Prioridade)
            .FirstOrDefaultAsync(cancellationToken);
}
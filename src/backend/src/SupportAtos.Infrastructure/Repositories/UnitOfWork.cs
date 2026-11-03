using SupportAtos.Application.Abstractions;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SupportAtosDbContext _context;

    public UnitOfWork(SupportAtosDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
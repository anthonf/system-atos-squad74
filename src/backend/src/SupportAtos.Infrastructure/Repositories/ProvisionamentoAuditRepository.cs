using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class ProvisionamentoAuditRepository : IProvisionamentoAuditRepository
{
    private readonly SupportAtosDbContext _context;

    public ProvisionamentoAuditRepository(SupportAtosDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProvisionamentoAudit log, CancellationToken cancellationToken = default)
        => await _context.ProvisionamentoAudits.AddAsync(log, cancellationToken);
}
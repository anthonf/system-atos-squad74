using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.Ports;

public interface IProvisionamentoAuditRepository
{
    Task AddAsync(ProvisionamentoAudit log, CancellationToken cancellationToken = default);
}
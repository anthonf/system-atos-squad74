using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;

namespace SupportAtos.Application.Ports;

public interface IUserProvisioningService
{
    Task<ProvisioningResult> ProvisionAsync(PortalUserContext usuario, CancellationToken cancellationToken = default);

    Task<ProvisioningResult> ProvisionPortalAsync(PortalUserContext usuario, CancellationToken cancellationToken = default);
}
using SupportAtos.Application.DTOs;

namespace SupportAtos.Application.Ports;

public interface IWebhookDispatcher
{
    Task EnviarAsync(WebhookEvent evento, CancellationToken cancellationToken = default);
}

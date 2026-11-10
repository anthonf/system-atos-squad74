using Microsoft.Extensions.Hosting;

namespace SupportAtos.Infrastructure.Webhooks;

public class WebhookSettings
{
    public const string SectionName = "Webhooks";

    public string? PortalUrl { get; set; }

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    public int MaxAttempts { get; set; } = 3;
}

public static class WebhookSettingsValidator
{
    public static void Validar(WebhookSettings settings, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(settings.PortalUrl))
        {
            return;
        }

        if (!Uri.TryCreate(settings.PortalUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Webhooks:PortalUrl deve ser uma URL absoluta HTTP/HTTPS.");
        }

        if (!environment.IsDevelopment()
            && !environment.IsEnvironment("Testing")
            && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Webhooks:PortalUrl deve usar HTTPS fora de Development/Testing.");
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey) || settings.ApiKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Webhooks:ApiKey não configurada ou com menos de 32 caracteres. Use uma chave distinta da API key de entrada.");
        }

        if (settings.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("Webhooks:TimeoutSeconds deve ser maior que zero.");
        }

        if (settings.MaxAttempts is <= 0 or > 10)
        {
            throw new InvalidOperationException("Webhooks:MaxAttempts deve estar entre 1 e 10.");
        }
    }
}

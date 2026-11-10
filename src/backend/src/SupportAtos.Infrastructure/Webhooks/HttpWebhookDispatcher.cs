using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;

namespace SupportAtos.Infrastructure.Webhooks;

public class HttpWebhookDispatcher : IWebhookDispatcher
{
    private const string HeaderApiKey = "X-Api-Key";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<WebhookSettings> _settings;
    private readonly ILogger<HttpWebhookDispatcher> _logger;

    public HttpWebhookDispatcher(
        IHttpClientFactory httpClientFactory,
        IOptions<WebhookSettings> settings,
        ILogger<HttpWebhookDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task EnviarAsync(WebhookEvent evento, CancellationToken cancellationToken = default)
    {
        var settings = _settings.Value;
        if (string.IsNullOrWhiteSpace(settings.PortalUrl))
        {
            _logger.LogDebug("Webhook desabilitado: Webhooks:PortalUrl não configurada.");
            return;
        }

        for (var tentativa = 1; tentativa <= settings.MaxAttempts; tentativa++)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("webhooks");
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

                using var request = new HttpRequestMessage(HttpMethod.Post, settings.PortalUrl)
                {
                    Content = JsonContent.Create(evento, options: JsonOptions)
                };
                request.Headers.TryAddWithoutValidation(HeaderApiKey, settings.ApiKey);

                using var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook {Evento} do chamado {Codigo} entregue com status {Status}.",
                        evento.Evento,
                        evento.CodigoPublico,
                        (int)response.StatusCode);
                    return;
                }

                _logger.LogWarning(
                    "Webhook {Evento} do chamado {Codigo} falhou com status {Status} (tentativa {Tentativa}/{Maximo}).",
                    evento.Evento,
                    evento.CodigoPublico,
                    (int)response.StatusCode,
                    tentativa,
                    settings.MaxAttempts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Webhook {Evento} do chamado {Codigo} falhou (tentativa {Tentativa}/{Maximo}).",
                    evento.Evento,
                    evento.CodigoPublico,
                    tentativa,
                    settings.MaxAttempts);
            }

            if (tentativa < settings.MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * tentativa), cancellationToken);
            }
        }

        _logger.LogError(
            "Webhook {Evento} do chamado {Codigo} não foi entregue após {Maximo} tentativas.",
            evento.Evento,
            evento.CodigoPublico,
            settings.MaxAttempts);
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAtos.Api.Authentication;

namespace SupportAtos.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ILogger<WebhooksController> logger)
    {
        _logger = logger;
    }

    [HttpPost("test")]
    [Authorize(Policy = AuthenticationSetup.PolicyAutomacao)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult ReceberTeste([FromBody] JsonElement payload)
    {
        var evento = payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("evento", out var valor)
            && valor.ValueKind == JsonValueKind.String
                ? valor.GetString()
                : "desconhecido";

        _logger.LogInformation("Webhook de teste recebido: evento={Evento}.", evento);
        return Accepted(new { recebido = true, evento });
    }
}

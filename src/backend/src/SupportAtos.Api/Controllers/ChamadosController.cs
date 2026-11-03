using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "UsuarioPortal")]
[Route("api/chamados")]
public class ChamadosController : ControllerBase
{
    private readonly CriarChamadoUseCase _criarChamadoUseCase;
    private readonly ConsultarChamadoUseCase _consultarChamadoUseCase;
    private readonly ListarChamadosUseCase _listarChamadosUseCase;
    private readonly ResponderChamadoUseCase _responderChamadoUseCase;
    private readonly MudarStatusChamadoUseCase _mudarStatusChamadoUseCase;
    private readonly AtribuirAgenteUseCase _atribuirAgenteUseCase;

    public ChamadosController(
        CriarChamadoUseCase criarChamadoUseCase,
        ConsultarChamadoUseCase consultarChamadoUseCase,
        ListarChamadosUseCase listarChamadosUseCase,
        ResponderChamadoUseCase responderChamadoUseCase,
        MudarStatusChamadoUseCase mudarStatusChamadoUseCase,
        AtribuirAgenteUseCase atribuirAgenteUseCase)
    {
        _criarChamadoUseCase = criarChamadoUseCase;
        _consultarChamadoUseCase = consultarChamadoUseCase;
        _listarChamadosUseCase = listarChamadosUseCase;
        _responderChamadoUseCase = responderChamadoUseCase;
        _mudarStatusChamadoUseCase = mudarStatusChamadoUseCase;
        _atribuirAgenteUseCase = atribuirAgenteUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChamadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CriarChamado([FromBody] CriarChamadoInput input, CancellationToken cancellationToken)
    {
        var chamado = await _criarChamadoUseCase.Handle(input, cancellationToken);
        return CreatedAtAction(nameof(ConsultarChamado), new { codigoPublico = chamado.CodigoPublico }, chamado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChamadoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarFila(
        [FromQuery] StatusChamado? status,
        [FromQuery] string? categoria,
        [FromQuery] string? produto,
        [FromQuery] int? agenteId,
        [FromQuery] int? grupoEmpresaId,
        [FromQuery] bool? semAgente,
        CancellationToken cancellationToken)
    {
        var chamados = await _listarChamadosUseCase.Handle(
            new FilaChamadosQuery(status, categoria, produto, agenteId, grupoEmpresaId, semAgente),
            cancellationToken);
        return Ok(chamados);
    }

    [HttpGet("{codigoPublico}")]
    [ProducesResponseType(typeof(ChamadoDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarChamado(string codigoPublico, CancellationToken cancellationToken)
    {
        var detalhe = await _consultarChamadoUseCase.Handle(codigoPublico, cancellationToken);
        return Ok(detalhe);
    }

    [HttpPost("{codigoPublico}/interacoes")]
    [ProducesResponseType(typeof(InteracaoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResponderChamado(string codigoPublico, [FromBody] ResponderChamadoInput input, CancellationToken cancellationToken)
    {
        var interacao = await _responderChamadoUseCase.Handle(codigoPublico, input, cancellationToken);
        return CreatedAtAction(nameof(ConsultarChamado), new { codigoPublico }, interacao);
    }

    [HttpPatch("{codigoPublico}/status")]
    [ProducesResponseType(typeof(ChamadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MudarStatus(string codigoPublico, [FromBody] MudarStatusInput input, CancellationToken cancellationToken)
    {
        var chamado = await _mudarStatusChamadoUseCase.Handle(codigoPublico, input, cancellationToken);
        return Ok(chamado);
    }

    [HttpPatch("{codigoPublico}/atribuir")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Supervisor,Agente")]
    [ProducesResponseType(typeof(ChamadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtribuirAgente(string codigoPublico, [FromBody] AtribuirAgenteInput input, CancellationToken cancellationToken)
    {
        var chamado = await _atribuirAgenteUseCase.Handle(codigoPublico, input, cancellationToken);
        return Ok(chamado);
    }
}

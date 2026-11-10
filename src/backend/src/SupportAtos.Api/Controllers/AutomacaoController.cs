using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAtos.Api.Authentication;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Api.Controllers;

[ApiController]
[Route("api/automacao")]
[Authorize(Policy = AuthenticationSetup.PolicyAutomacao)]
public class AutomacaoController : ControllerBase
{
    private readonly IChamadoRepository _chamadoRepository;

    public AutomacaoController(IChamadoRepository chamadoRepository)
    {
        _chamadoRepository = chamadoRepository;
    }

    [HttpGet("chamados")]
    [ProducesResponseType(typeof(IReadOnlyList<ChamadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarChamados(
        [FromQuery] StatusChamado? status,
        [FromQuery] string? categoria,
        [FromQuery] string? produto,
        [FromQuery] int? agenteId,
        [FromQuery] int? grupoEmpresaId,
        [FromQuery] bool? semAgente,
        CancellationToken cancellationToken)
    {
        var chamados = await _chamadoRepository.ListarFilaAsync(
            new FilaChamadosQuery(status, categoria, produto, agenteId, grupoEmpresaId, semAgente),
            cancellationToken);
        return Ok(chamados.Select(DtoMapper.ToDto).ToList());
    }

    [HttpGet("chamados/{codigoPublico}")]
    [ProducesResponseType(typeof(ChamadoDetalheDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarChamado(
        string codigoPublico,
        CancellationToken cancellationToken)
    {
        var chamado = await _chamadoRepository.GetByCodigoPublicoComHistoricoAsync(codigoPublico, cancellationToken);
        if (chamado is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Não encontrado",
                Detail = $"Chamado '{codigoPublico}' não encontrado.",
                Instance = HttpContext.Request.Path
            });
        }

        var interacoes = chamado.Interacoes
            .OrderBy(interacao => interacao.CriadoEm)
            .Select(DtoMapper.ToDto)
            .ToList();

        return Ok(new ChamadoDetalheDto(DtoMapper.ToDto(chamado), interacoes));
    }
}

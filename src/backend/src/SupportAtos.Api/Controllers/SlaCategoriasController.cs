using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAtos.Api.Authentication;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.UseCases;

namespace SupportAtos.Api.Controllers;

[ApiController]
[Route("api/sla-categorias")]
public class SlaCategoriasController : ControllerBase
{
    private readonly ListarSlaCategoriasUseCase _listarSlaCategoriasUseCase;

    public SlaCategoriasController(ListarSlaCategoriasUseCase listarSlaCategoriasUseCase)
    {
        _listarSlaCategoriasUseCase = listarSlaCategoriasUseCase;
    }

    [HttpGet]
    [Authorize(Policy = AuthenticationSetup.PolicyPortalOuEquipe)]
    [ProducesResponseType(typeof(IReadOnlyList<SlaCategoriaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
        => Ok(await _listarSlaCategoriasUseCase.Handle(cancellationToken));
}

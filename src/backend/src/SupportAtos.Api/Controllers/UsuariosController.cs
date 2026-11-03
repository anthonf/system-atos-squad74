using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.UseCases;
using SupportAtos.Application.Abstractions;
using SupportAtos.Api.Authentication;

namespace SupportAtos.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly CadastrarUsuarioUseCase _cadastrarUsuarioUseCase;
    private readonly ConsultarUsuarioUseCase _consultarUsuarioUseCase;
    private readonly ListarUsuariosPorGrupoUseCase _listarUsuariosPorGrupoUseCase;

    public UsuariosController(
        CadastrarUsuarioUseCase cadastrarUsuarioUseCase,
        ConsultarUsuarioUseCase consultarUsuarioUseCase,
        ListarUsuariosPorGrupoUseCase listarUsuariosPorGrupoUseCase)
    {
        _cadastrarUsuarioUseCase = cadastrarUsuarioUseCase;
        _consultarUsuarioUseCase = consultarUsuarioUseCase;
        _listarUsuariosPorGrupoUseCase = listarUsuariosPorGrupoUseCase;
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "UsuarioPortal")]
    [ProducesResponseType(typeof(SessaoUsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ConsultarSessao([FromServices] ICurrentUser currentUser)
    {
        Response.Headers.CacheControl = "no-store";
        var usuario = currentUser.UsuarioAtual;
        var expiracao = (DateTime)HttpContext.Items[PortalUserContextItems.ExpiracaoToken]!;
        return Ok(new SessaoUsuarioDto(
            usuario.UsuarioIdInterno, usuario.Nome, usuario.Email, usuario.Papel,
            usuario.GrupoEmpresaIdInterno, usuario.NomeGrupo, expiracao, DateTime.UtcNow));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CadastrarUsuario([FromBody] CadastrarUsuarioInput input, CancellationToken cancellationToken)
    {
        var usuario = await _cadastrarUsuarioUseCase.Handle(input, cancellationToken);
        return CreatedAtAction(nameof(ConsultarUsuario), new { id = usuario.Id }, usuario);
    }

    [HttpGet("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Supervisor,Agente")]
    [ProducesResponseType(typeof(UsuarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarUsuario(int id, CancellationToken cancellationToken)
    {
        var usuario = await _consultarUsuarioUseCase.Handle(id, cancellationToken);
        return Ok(usuario);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Supervisor,Agente")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUsuariosPorGrupo([FromQuery] int grupoEmpresaId, CancellationToken cancellationToken)
    {
        var usuarios = await _listarUsuariosPorGrupoUseCase.Handle(grupoEmpresaId, cancellationToken);
        return Ok(usuarios);
    }
}

using SupportAtos.Application.Abstractions;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Validation;

namespace SupportAtos.Api.Authentication;

public static class PortalHeaders
{
    public const string UsuarioId = "X-Portal-User-Id";
    public const string GrupoId = "X-Portal-Group-Id";
}

public class PortalUserResolver
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IGrupoEmpresaRepository _grupos;

    public PortalUserResolver(IUsuarioRepository usuarios, IGrupoEmpresaRepository grupos)
    {
        _usuarios = usuarios;
        _grupos = grupos;
    }

    public async Task<PortalUserContext> ResolverAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var codigoUsuario = PortalIdentificadorValidator.Validar(
            context.Request.Headers[PortalHeaders.UsuarioId].ToString(),
            PortalHeaders.UsuarioId);
        var codigoGrupo = PortalIdentificadorValidator.Validar(
            context.Request.Headers[PortalHeaders.GrupoId].ToString(),
            PortalHeaders.GrupoId);

        var usuario = await _usuarios.GetByCodigoExternoAsync(codigoUsuario, cancellationToken)
            ?? throw new NotFoundException("Usuário do portal não encontrado.");
        var grupo = await _grupos.GetByCodigoExternoAsync(codigoGrupo, cancellationToken)
            ?? throw new NotFoundException("Grupo do portal não encontrado.");

        if (usuario.GrupoEmpresaId != grupo.Id)
        {
            throw new NotFoundException("Usuário não pertence ao grupo informado.");
        }

        return new PortalUserContext(
            usuario.Id,
            grupo.Id,
            usuario.CodigoExterno!,
            grupo.CodigoExterno!,
            usuario.Email,
            usuario.Nome,
            grupo.Nome,
            usuario.Papel);
    }
}

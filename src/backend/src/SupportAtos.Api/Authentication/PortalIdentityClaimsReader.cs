using SupportAtos.Api.Options;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Api.Authentication;

/// <summary>
/// Claims relevantes do portal, lidas do token já validado (sem os ids internos da API).
/// </summary>
public record PortalIdentityClaims(
    string CodigoExternoUsuario,
    string CodigoExternoGrupo,
    string Email,
    string Nome,
    string NomeGrupo,
    PapelUsuario Papel);

public static class PortalIdentityClaimsReader
{
    public static PortalIdentityClaims Ler(System.Security.Claims.ClaimsPrincipal principal, ClaimNamesSettings nomes)
    {
        var sub = principal.FindFirst(nomes.Sub)?.Value;
        var email = principal.FindFirst(nomes.Email)?.Value;
        var nome = principal.FindFirst(nomes.Nome)?.Value;
        var grupoId = principal.FindFirst(nomes.GrupoEmpresaId)?.Value;
        var grupoNome = principal.FindFirst(nomes.GrupoEmpresaNome)?.Value;
        var papel = principal.FindFirst(nomes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: identificação do usuário.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: e-mail.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: nome.");
        }

        if (string.IsNullOrWhiteSpace(grupoId))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: grupo de empresa.");
        }

        if (string.IsNullOrWhiteSpace(grupoNome))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: nome do grupo de empresa.");
        }

        if (string.IsNullOrWhiteSpace(papel))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Claim obrigatória ausente: papel/role.");
        }

        if (!Enum.TryParse<PapelUsuario>(papel, ignoreCase: true, out var papelParsed))
        {
            throw new Microsoft.IdentityModel.Tokens.SecurityTokenValidationException("Papel/role inválido no token.");
        }

        return new PortalIdentityClaims(sub.Trim(), grupoId.Trim(), email.Trim(), nome.Trim(), grupoNome.Trim(), papelParsed);
    }
}
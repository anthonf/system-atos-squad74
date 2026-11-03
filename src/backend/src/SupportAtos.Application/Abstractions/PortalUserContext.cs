using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Abstractions;

/// <summary>
/// Identidade do usuário autenticado via SSO do portal, já provisionada
/// (com os ids internos persistidos na API).
/// </summary>
public record PortalUserContext(
    int UsuarioIdInterno,
    int GrupoEmpresaIdInterno,
    string CodigoExternoUsuario,
    string CodigoExternoGrupo,
    string Email,
    string Nome,
    string NomeGrupo,
    PapelUsuario Papel)
{
    public string ObterHashClaims()
    {
        var bruto = string.Join('|', CodigoExternoUsuario, Email, Nome, CodigoExternoGrupo, NomeGrupo, Papel);
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(bruto));
        return Convert.ToHexString(bytes);
    }
}
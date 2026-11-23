using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Ports;

/// <summary>
/// Porta para geração de tokens de autenticação (JWT).
/// </summary>
public interface ITokenGerador
{
    /// <summary>Gera um token JWT para o usuário informado.</summary>
    string GerarToken(
        int usuarioId,
        string email,
        string nome,
        PapelUsuario papel,
        int? grupoEmpresaId);
}

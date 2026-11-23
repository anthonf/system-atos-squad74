namespace SupportAtos.Application.Ports;

/// <summary>
/// Porta para geração de refresh tokens criptograficamente aleatórios.
/// </summary>
public interface IRefreshTokenGerador
{
    /// <summary>Gera um refresh token criptograficamente aleatório (texto puro, não hash).</summary>
    string GerarToken();
}

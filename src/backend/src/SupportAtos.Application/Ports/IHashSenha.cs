using System.Threading.Tasks;

namespace SupportAtos.Application.Ports;

/// <summary>
/// Porta para hash e verificação de senhas usando Argon2id.
/// </summary>
public interface IHashSenha
{
    /// <summary>Gera o hash Argon2id de uma senha em texto puro.</summary>
    string Hash(string senha);

    /// <summary>Verifica se a senha corresponde ao hash armazenado.</summary>
    bool Verifica(string senha, string hash);
}

using System.Security.Cryptography;
using System.Text;

namespace SupportAtos.Api.Options;

/// <summary>
/// Configuração das chaves de API (seção "ApiKeys"). Só hashes SHA-256 são
/// persistidos em configuração; o valor em texto puro é exibido uma única vez
/// pelo comando <c>--generate-api-key</c> e nunca deve ser versionado.
/// </summary>
public class ApiKeySettings
{
    public const string SectionName = "ApiKeys";

    public ApiKeyClientSettings Portal { get; set; } = new();

    public ApiKeyClientSettings Automacao { get; set; } = new();
}

public class ApiKeyClientSettings
{
    /// <summary>Hash SHA-256 (hex) da chave primária.</summary>
    public string? PrimaryHash { get; set; }

    /// <summary>Hash SHA-256 (hex) da chave secundária, para rotação sem downtime.</summary>
    public string? SecondaryHash { get; set; }

    public IEnumerable<string> Hashes()
    {
        if (!string.IsNullOrWhiteSpace(PrimaryHash))
        {
            yield return PrimaryHash.Trim();
        }

        if (!string.IsNullOrWhiteSpace(SecondaryHash))
        {
            yield return SecondaryHash.Trim();
        }
    }
}

public static class ApiKeyHasher
{
    public const int TamanhoMinimoChave = 32;

    public static string Hash(string chave)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(chave));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Compara a chave recebida com o hash configurado em tempo constante.</summary>
    public static bool Corresponde(string chaveRecebida, string hashConfigurado)
    {
        if (string.IsNullOrWhiteSpace(hashConfigurado))
        {
            return false;
        }

        var calculado = SHA256.HashData(Encoding.UTF8.GetBytes(chaveRecebida));
        var esperado = ConverterHex(hashConfigurado);
        return esperado is not null && CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    public static bool HashValido(string? hash)
        => !string.IsNullOrWhiteSpace(hash)
           && hash.Trim().Length == 64
           && ConverterHex(hash) is not null;

    private static byte[]? ConverterHex(string valor)
    {
        try
        {
            return Convert.FromHexString(valor.Trim());
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

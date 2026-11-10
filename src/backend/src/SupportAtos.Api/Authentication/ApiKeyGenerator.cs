using System.Security.Cryptography;
using SupportAtos.Api.Options;

namespace SupportAtos.Api.Authentication;

public static class ApiKeyGenerator
{
    public static int Executar(string[] args, TextWriter saida)
    {
        var escopo = LerEscopo(args);
        if (escopo is null)
        {
            saida.WriteLine("Uso: dotnet run --project src/SupportAtos.Api -- --generate-api-key [--escopo portal|automacao]");
            return 1;
        }

        var bytes = RandomNumberGenerator.GetBytes(32);
        var chave = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var hash = ApiKeyHasher.Hash(chave);

        saida.WriteLine($"Escopo: {escopo}");
        saida.WriteLine($"Chave (exibida apenas uma vez): {chave}");
        saida.WriteLine($"Hash SHA-256: {hash}");
        saida.WriteLine();
        saida.WriteLine("Guarde a chave no cofre do cliente e configure somente o hash:");
        saida.WriteLine($"ApiKeys:{char.ToUpperInvariant(escopo[0])}{escopo[1..]}:PrimaryHash = {hash}");
        saida.WriteLine();
        saida.WriteLine("Para rotação sem downtime, gere outra chave e use SecondaryHash antes de promover a nova.");
        return 0;
    }

    private static string? LerEscopo(string[] args)
    {
        var indice = Array.FindIndex(args, arg =>
            string.Equals(arg, "--escopo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "--scope", StringComparison.OrdinalIgnoreCase));

        var escopo = indice >= 0 && indice + 1 < args.Length ? args[indice + 1] : "portal";
        escopo = escopo.Trim().ToLowerInvariant();

        return escopo is "portal" or "automacao" ? escopo : null;
    }
}

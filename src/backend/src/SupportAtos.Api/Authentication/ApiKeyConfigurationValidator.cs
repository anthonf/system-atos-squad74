using SupportAtos.Api.Options;

namespace SupportAtos.Api.Authentication;

public static class ApiKeyConfigurationValidator
{
    public static void Validar(ApiKeySettings settings, IHostEnvironment environment)
    {
        ValidarHashes(settings.Portal.Hashes(), "ApiKeys:Portal");
        ValidarHashes(settings.Automacao.Hashes(), "ApiKeys:Automacao");

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        if (!settings.Portal.Hashes().Any())
        {
            throw new InvalidOperationException(
                "API key do portal não configurada. Defina 'ApiKeys:Portal:PrimaryHash' (ou SecondaryHash) via variável de ambiente/cofre.");
        }

        var todos = settings.Portal.Hashes().Concat(settings.Automacao.Hashes())
            .Select(hash => hash.ToLowerInvariant())
            .ToList();
        if (todos.Count != todos.Distinct().Count())
        {
            throw new InvalidOperationException(
                "A mesma API key não pode ser usada em escopos diferentes. Gere chaves distintas para portal e automação.");
        }
    }

    private static void ValidarHashes(IEnumerable<string> hashes, string secao)
    {
        foreach (var hash in hashes)
        {
            if (!ApiKeyHasher.HashValido(hash))
            {
                throw new InvalidOperationException(
                    $"Hash de API key inválido em '{secao}'. Use SHA-256 em hexadecimal (64 caracteres), gerado por --generate-api-key.");
            }
        }
    }
}

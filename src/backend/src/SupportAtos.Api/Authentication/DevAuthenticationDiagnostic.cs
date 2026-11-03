using System.Security.Cryptography;

namespace SupportAtos.Api.Authentication;

public static class DevAuthenticationDiagnostic
{
    // Somente chaves públicas. Não abre a chave privada nem imprime configurações sensíveis.
    public static int Executar(IConfiguration configuration, string contentRoot, TextWriter output)
    {
        var arquivo = Path.GetFullPath(Path.Combine(contentRoot, "../../tools/SupportAtos.TokenMinter/keys/dev-public.pem"));
        output.WriteLine("Diagnóstico de autenticação local (configuração efetiva da API).");
        foreach (var nome in new[] { "Sso:PublicKeyPem", "Sso:Issuer", "Sso:Audience" })
        {
            var fonte = (configuration as IConfigurationRoot)?.Providers.Reverse()
                .FirstOrDefault(provider => provider.TryGet(nome, out _));
            output.WriteLine($"{nome}: fonte = {fonte?.ToString() ?? "não encontrada"}");
        }

        if (!File.Exists(arquivo) || string.IsNullOrWhiteSpace(configuration["Sso:PublicKeyPem"]))
        {
            output.WriteLine("FALHOU: chave pública local ou configuração Sso:PublicKeyPem ausente.");
            return 1;
        }

        try
        {
            using var local = RSA.Create();
            using var configurada = RSA.Create();
            local.ImportFromPem(File.ReadAllText(arquivo));
            configurada.ImportFromPem(configuration["Sso:PublicKeyPem"]!);
            var corresponde = local.ExportSubjectPublicKeyInfo().AsSpan()
                .SequenceEqual(configurada.ExportSubjectPublicKeyInfo());
            output.WriteLine(corresponde ? "Chave pública: OK." : "Chave pública: DIVERGENTE. Atualize Sso:PublicKeyPem no User Secrets e reinicie a API.");
            var emissor = configuration["Sso:Issuer"] == "supportatos-dev";
            var audiencia = configuration["Sso:Audience"] == "supportatos-api";
            output.WriteLine($"Issuer padrão do mint-token.ps1: {(emissor ? "OK" : "DIVERGENTE; alinhe o parâmetro -Issuer do script")}");
            output.WriteLine($"Audience padrão do mint-token.ps1: {(audiencia ? "OK" : "DIVERGENTE; alinhe o parâmetro -Audience do script")}");
            return corresponde && emissor && audiencia ? 0 : 1;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            output.WriteLine("FALHOU: uma das chaves públicas não contém um PEM RSA válido.");
            return 1;
        }
    }
}

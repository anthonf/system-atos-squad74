using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SupportAtos.TokenMinter;

/// <summary>
/// Ferramenta de desenvolvimento: gera um keypair RSA (dev) e emite JWT RS256
/// compatíveis com os settings da API (Sso:Issuer, Sso:Audience e os nomes de claims
/// de SupportAtos.Api/Authentication/PortalIdentityClaimsReader.cs).
///
/// Uso:
///   dotnet run --project tools/SupportAtos.TokenMinter -- generate
///   dotnet run --project tools/SupportAtos.TokenMinter -- token --sub portal-1 --email joao@atos.com --name Joao --grupo-id grupo-1 --grupo-nome "Grupo Norte" --papel Admin
/// </summary>
internal static class Program
{
    private static readonly string DefaultKeysDir = BuscarDiretorioDeChaves();

    private static string BuscarDiretorioDeChaves()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio != null &&
               !File.Exists(Path.Combine(diretorio.FullName, "SupportAtos.TokenMinter.csproj")))
        {
            diretorio = diretorio.Parent;
        }

        return diretorio != null
            ? Path.Combine(diretorio.FullName, "keys")
            : Path.Combine("tools", "SupportAtos.TokenMinter", "keys");
    }

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Uso: <generate|token> [opcoes]");
            return 1;
        }

        try
        {
return args[0] switch
        {
            "generate" => Gerar(args[1..]),
            "token" => EmitirToken(args[1..]),
            "verify" => Verificar(args[1..]),
            _ => throw new ArgumentException($"Comando desconhecido: {args[0]}")
        };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }

    private static int Gerar(string[] args)
    {
        var outDir = Obter(args, "--out") ?? DefaultKeysDir;
        Directory.CreateDirectory(outDir);

        var arquivoPrivado = Path.Combine(outDir, "dev-private.pem");
        var arquivoPublico = Path.Combine(outDir, "dev-public.pem");

        if ((File.Exists(arquivoPrivado) || File.Exists(arquivoPublico)) && !args.Contains("--overwrite"))
        {
            throw new InvalidOperationException(
                "Já existe um par de chaves neste diretório. Para rotacionar explicitamente, use generate --overwrite e atualize a chave pública da API. Tokens anteriores deixarão de ser aceitos após a atualização.");
        }

        using var rsa = RSA.Create(2048);

        var pemPrivado = rsa.ExportPkcs8PrivateKeyPem();
        var pemPublico = rsa.ExportSubjectPublicKeyInfoPem();

        File.WriteAllText(arquivoPrivado, pemPrivado + Environment.NewLine);
        File.WriteAllText(arquivoPublico, pemPublico + Environment.NewLine);

        Console.WriteLine($"Chave privada : {arquivoPrivado}");
        Console.WriteLine($"Chave publica  : {arquivoPublico}");
        Console.WriteLine("PUBLIC_PEM_BEGIN");
        Console.Write(pemPublico);
        Console.WriteLine("PUBLIC_PEM_END");
        return 0;
    }

    private static int EmitirToken(string[] args)
    {
        var arquivoPrivado = Obter(args, "--private-key") ?? Path.Combine(DefaultKeysDir, "dev-private.pem");
        if (!File.Exists(arquivoPrivado))
        {
            throw new FileNotFoundException(
                $"Chave privada nao encontrada em '{arquivoPrivado}'. Execute 'generate' primeiro.");
        }

        var sub = Obter(args, "--sub") ?? throw new ArgumentException("Informe --sub (identificador do usuario no portal).");
        var email = Obter(args, "--email") ?? throw new ArgumentException("Informe --email.");
        var nome = Obter(args, "--name") ?? throw new ArgumentException("Informe --name.");
        var grupoId = Obter(args, "--grupo-id") ?? throw new ArgumentException("Informe --grupo-id.");
        var grupoNome = Obter(args, "--grupo-nome") ?? throw new ArgumentException("Informe --grupo-nome.");
        var papel = Obter(args, "--papel") ?? "Agente";
        var issuer = Obter(args, "--issuer") ?? "supportatos-dev";
        var audience = Obter(args, "--audience") ?? "supportatos-api";
        var minutos = int.TryParse(Obter(args, "--minutes"), out var m) ? m : 60;
        var deltaNotBefore = int.TryParse(Obter(args, "--not-before-delta"), out var n) ? n : -5;

        if (minutos <= 0)
        {
            throw new ArgumentException("O tempo de vida do token (--minutes) deve ser maior que zero.");
        }

        var agora = DateTime.UtcNow;
        var expiracao = agora.AddMinutes(minutos);
        var notBefore = agora.AddMinutes(deltaNotBefore);

        var payload = new Dictionary<string, object?>
        {
            ["sub"] = sub,
            ["email"] = email,
            ["name"] = nome,
            ["grupo_empresa_id"] = grupoId,
            ["grupo_empresa_nome"] = grupoNome,
            ["role"] = papel,
            ["iss"] = issuer,
            ["aud"] = audience,
            ["nbf"] = new DateTimeOffset(notBefore).ToUnixTimeSeconds(),
            ["iat"] = new DateTimeOffset(agora).ToUnixTimeSeconds(),
            ["exp"] = new DateTimeOffset(expiracao).ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString("N")
        };

        var header = new Dictionary<string, object?>
        {
            ["alg"] = "RS256",
            ["typ"] = "JWT"
        };

        var entrada = $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(header))}." +
                       Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(arquivoPrivado));
        var assinatura = rsa.SignData(Encoding.ASCII.GetBytes(entrada), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        Console.WriteLine($"{entrada}.{Base64Url(assinatura)}");
        return 0;
    }

    private static int Verificar(string[] args)
    {
        var token = Obter(args, "--token") ?? throw new ArgumentException("Informe --token.");
        var arquivoPublico = Obter(args, "--public-key") ?? Path.Combine(DefaultKeysDir, "dev-public.pem");
        if (!File.Exists(arquivoPublico))
        {
            throw new FileNotFoundException($"Chave publica nao encontrada em '{arquivoPublico}'.");
        }

        var partes = token.Split('.');
        if (partes.Length != 3)
        {
            throw new ArgumentException("Token com estrutura invalida (esperado header.payload.assinatura).");
        }

        var payloadBytes = Base64UrlDecodificar(partes[1]);
        using var documento = JsonDocument.Parse(payloadBytes);
        var raiz = documento.RootElement;

        var entradas = $"{partes[0]}.{partes[1]}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(arquivoPublico));
        var assinatura = Base64UrlDecodificar(partes[2]);
        var validaAssinatura = rsa.VerifyData(
            Encoding.ASCII.GetBytes(entradas), assinatura, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        Console.WriteLine($"Assinatura RS256 : {(validaAssinatura ? "OK" : "FALHOU")}");

        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var claim in new[] { "sub", "email", "name", "grupo_empresa_id", "grupo_empresa_nome", "role" })
        {
            var presente = raiz.TryGetProperty(claim, out var valorClaim) && valorClaim.ValueKind == JsonValueKind.String;
            Console.WriteLine($"Claim {claim,-23}: {(presente ? raiz.GetProperty(claim).GetString() : "AUSENTE")}");
        }

        var verifyIssuer = raiz.TryGetProperty("iss", out var issElem) && !string.IsNullOrEmpty(issElem.GetString());
        var verifyAudience = raiz.TryGetProperty("aud", out var audElem) && !string.IsNullOrEmpty(audElem.GetString());
        var verifyExp = raiz.TryGetProperty("exp", out var expElem) && expElem.GetInt64() > agora;
        var verifyNbf = !raiz.TryGetProperty("nbf", out var nbfElem) || nbfElem.GetInt64() <= agora;

        Console.WriteLine($"Issuer           : {(verifyIssuer ? issElem.GetString() : "AUSENTE")}");
        Console.WriteLine($"Audience         : {(verifyAudience ? audElem.GetString() : "AUSENTE")}");
        Console.WriteLine($"Exp <= agora      : {(verifyExp ? "OK" : "EXPIRADO")}");
        Console.WriteLine($"Nbf > agora       : {(verifyNbf ? "OK" : "AINDA NAO VALIDO")}");

        return validaAssinatura && verifyIssuer && verifyAudience && verifyExp && verifyNbf ? 0 : 1;
    }

    private static byte[] Base64UrlDecodificar(string valor)
    {
        var base64 = valor.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '='));
    }

    private static string? Obter(string[] args, string chave)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], chave, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

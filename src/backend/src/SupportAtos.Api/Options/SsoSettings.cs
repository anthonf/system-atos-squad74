namespace SupportAtos.Api.Options;

/// <summary>
/// Configurações de integração SSO (seção "Sso"). Nenhum segredo deve vir
/// do appsettings: configure via user-secrets (dev) ou variáveis de ambiente (produção).
/// </summary>
public class SsoSettings
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>URL do JWKS do portal (RS256). Vazio quando PublicKeyPem é usado.</summary>
    public string? JwksUrl { get; set; }

    /// <summary>Chave pública PEM (RS256) para uso local/testes em vez do JWKS.</summary>
    public string? PublicKeyPem { get; set; }

    public int ClockSkewSeconds { get; set; } = 30;

    public int CacheTtlSeconds { get; set; } = 60;

    public ClaimNamesSettings ClaimNames { get; set; } = new();
}

public class ClaimNamesSettings
{
    public string Sub { get; set; } = "sub";

    public string Email { get; set; } = "email";

    public string Nome { get; set; } = "name";

    public string GrupoEmpresaId { get; set; } = "grupo_empresa_id";

    public string GrupoEmpresaNome { get; set; } = "grupo_empresa_nome";

    public string Role { get; set; } = "role";
}
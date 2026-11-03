using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace SupportAtos.Integration.Tests;

/// <summary>
/// Emite tokens JWT de teste (RS256) assinados com uma chave própria,
/// cuja pública é injetada no Sso:PublicKeyPem da aplicação sob teste.
/// </summary>
public static class TestTokenFactory
{
    public const string Issuer = "supportatos-tests";
    public const string Audience = "supportatos-api";
    public const string ApiKey = "chave-teste-n8n";

    private static readonly RSA Chave = RSA.Create(2048);

    public static string ChavePublicaPem => Chave.ExportSubjectPublicKeyInfoPem();

    public static string CriarToken(
        string sub,
        string email,
        string nome,
        string grupoEmpresaId,
        string grupoEmpresaNome,
        string papel,
        TimeSpan? vida = null,
        string? issuer = null,
        string? audience = null,
        SecurityKey? chaveAssinatura = null,
        string? omitirClaim = null,
        TimeSpan? inicio = null)
    {
        var agora = DateTime.UtcNow;
        var expira = agora.Add(vida ?? TimeSpan.FromMinutes(5));
        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim("email", email),
            new Claim("name", nome),
            new Claim("grupo_empresa_id", grupoEmpresaId),
            new Claim("grupo_empresa_nome", grupoEmpresaNome),
            new Claim("role", papel)
        };

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims: claims.Where(c => c.Type != omitirClaim),
            notBefore: inicio.HasValue ? agora.Add(inicio.Value) : expira.AddMinutes(-5),
            expires: expira,
            signingCredentials: new SigningCredentials(chaveAssinatura ?? new RsaSecurityKey(Chave), SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

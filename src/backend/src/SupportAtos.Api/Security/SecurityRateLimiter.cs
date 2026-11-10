using Microsoft.Extensions.Caching.Memory;

namespace SupportAtos.Api.Security;

public interface ISecurityRateLimiter
{
    bool PermitirRequisicao(string identificador);

    bool EstaBloqueado(string identificador);

    void RegistrarFalhaAutenticacao(string identificador);
}

public class SecurityRateLimiter : ISecurityRateLimiter
{
    private const int LimiteRequisicoesPorMinuto = 120;
    private const int LimiteFalhasAutenticacaoPorMinuto = 10;

    private readonly IMemoryCache _cache;

    public SecurityRateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool PermitirRequisicao(string identificador)
    {
        var contador = ObterContador($"rate:req:{identificador}");
        lock (contador)
        {
            contador.Requisicoes++;
            return contador.Requisicoes <= LimiteRequisicoesPorMinuto;
        }
    }

    public bool EstaBloqueado(string identificador)
    {
        if (!_cache.TryGetValue($"rate:falha:{identificador}", out Contador? contador) || contador is null)
        {
            return false;
        }

        lock (contador)
        {
            return contador.Falhas >= LimiteFalhasAutenticacaoPorMinuto;
        }
    }

    public void RegistrarFalhaAutenticacao(string identificador)
    {
        var contador = ObterContador($"rate:falha:{identificador}");
        lock (contador)
        {
            contador.Falhas++;
        }
    }

    private Contador ObterContador(string chave)
        => _cache.GetOrCreate(chave, entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new Contador();
        })!;

    private sealed class Contador
    {
        public int Requisicoes { get; set; }

        public int Falhas { get; set; }
    }
}

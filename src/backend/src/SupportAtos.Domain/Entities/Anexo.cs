namespace SupportAtos.Domain.Entities;

public class Anexo
{
    public int Id { get; private set; }

    public int? InteracaoId { get; private set; }

    public Interacao? Interacao { get; private set; }

    public int ChamadoId { get; private set; }

    public Chamado? Chamado { get; private set; }

    public string NomeArquivo { get; private set; } = string.Empty;

    /// <summary>Mapeado para a coluna caminho (SQL oficial).</summary>
    public string Url { get; private set; } = string.Empty;

    public int TamanhoBytes { get; private set; }

    public string? TipoMime { get; private set; }

    protected Anexo()
    {
    }

    private Anexo(int? interacaoId, int chamadoId, string nomeArquivo, string url, int tamanhoBytes, string? tipoMime)
    {
        InteracaoId = interacaoId;
        ChamadoId = chamadoId;
        NomeArquivo = nomeArquivo;
        Url = url;
        TamanhoBytes = tamanhoBytes;
        TipoMime = tipoMime;
    }

    public static Anexo Criar(Interacao interacao, string nomeArquivo, string url, int tamanhoBytes, string? tipoMime = null)
    {
        if (interacao is null)
        {
            throw new Exceptions.DomainException("A interação do anexo é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new Exceptions.DomainException("O nome do arquivo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exceptions.DomainException("A URL do arquivo é obrigatória.");
        }

        if (tamanhoBytes < 0)
        {
            throw new Exceptions.DomainException("O tamanho do arquivo não pode ser negativo.");
        }

        return new Anexo(interacao.Id, interacao.ChamadoId, nomeArquivo.Trim(), url.Trim(), tamanhoBytes, tipoMime);
    }

    public static Anexo CriarParaChamado(int chamadoId, string nomeArquivo, string url, int tamanhoBytes, string? tipoMime = null, int? interacaoId = null)
    {
        if (chamadoId <= 0)
        {
            throw new Exceptions.DomainException("O chamado do anexo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new Exceptions.DomainException("O nome do arquivo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new Exceptions.DomainException("A URL do arquivo é obrigatória.");
        }

        if (tamanhoBytes < 0)
        {
            throw new Exceptions.DomainException("O tamanho do arquivo não pode ser negativo.");
        }

        return new Anexo(interacaoId, chamadoId, nomeArquivo.Trim(), url.Trim(), tamanhoBytes, tipoMime);
    }
}

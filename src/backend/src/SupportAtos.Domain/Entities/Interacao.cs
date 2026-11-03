using SupportAtos.Domain.Enums;

namespace SupportAtos.Domain.Entities;

public class Interacao
{
    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    public Usuario? Usuario { get; private set; }

    public int ChamadoId { get; private set; }

    public Chamado? Chamado { get; private set; }

    public TipoInteracao Tipo { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    public ICollection<Anexo> Anexos { get; private set; } = new List<Anexo>();

    protected Interacao()
    {
    }

    private Interacao(int usuarioId, int chamadoId, TipoInteracao tipo, string mensagem, DateTime criadoEm)
    {
        UsuarioId = usuarioId;
        ChamadoId = chamadoId;
        Tipo = tipo;
        Mensagem = mensagem;
        CriadoEm = criadoEm;
    }

    public static Interacao Criar(Chamado chamado, int usuarioId, TipoInteracao tipo, string mensagem, DateTime criadoEm)
    {
        if (chamado is null)
        {
            throw new Exceptions.DomainException("O chamado da interação é obrigatório.");
        }

        if (usuarioId <= 0)
        {
            throw new Exceptions.DomainException("O usuário da interação é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(mensagem))
        {
            throw new Exceptions.DomainException("A mensagem da interação é obrigatória.");
        }

        return new Interacao(usuarioId, chamado.Id, tipo, mensagem.Trim(), criadoEm);
    }

    public Anexo AdicionarAnexo(string nomeArquivo, string url, int tamanhoBytes)
    {
        var anexo = Anexo.Criar(this, nomeArquivo, url, tamanhoBytes);
        Anexos.Add(anexo);
        return anexo;
    }
}
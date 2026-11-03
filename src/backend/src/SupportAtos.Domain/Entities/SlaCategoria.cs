using SupportAtos.Domain.Enums;

namespace SupportAtos.Domain.Entities;

public class SlaCategoria
{
    public int Id { get; private set; }

    public string Produto { get; private set; } = string.Empty;

    public string Categoria { get; private set; } = string.Empty;

    public Prioridade Prioridade { get; private set; }

    public int TempoResposta { get; private set; }

    public int TempoResolucao { get; private set; }

    /// <summary>Minutos (sla_config.tempo_resposta_min / tempo_resolucao_min).</summary>
    public bool Ativo { get; private set; } = true;

    public ICollection<Chamado> Chamados { get; private set; } = new List<Chamado>();

    protected SlaCategoria()
    {
    }

    private SlaCategoria(string produto, string categoria, Prioridade prioridade, int tempoResposta, int tempoResolucao)
    {
        AlterarProduto(produto);
        AlterarCategoria(categoria);
        AlterarPrioridade(prioridade);
        AlterarTempos(tempoResposta, tempoResolucao);
    }

    public static SlaCategoria Criar(string produto, string categoria, Prioridade prioridade, int tempoResposta, int tempoResolucao, bool ativo = true)
    {
        var sla = new SlaCategoria(produto, categoria, prioridade, tempoResposta, tempoResolucao);
        sla.Ativo = ativo;
        return sla;
    }

    public void AlterarProduto(string produto)
    {
        if (string.IsNullOrWhiteSpace(produto))
        {
            throw new Exceptions.DomainException("O produto é obrigatório.");
        }

        Produto = produto.Trim();
    }

    public void AlterarCategoria(string categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            throw new Exceptions.DomainException("A categoria é obrigatória.");
        }

        Categoria = categoria.Trim();
    }

    public void AlterarPrioridade(Prioridade prioridade)
    {
        Prioridade = prioridade;
    }

    public void DefinirAtivo(bool ativo)
    {
        Ativo = ativo;
    }

    public void AlterarTempos(int tempoResposta, int tempoResolucao)
    {
        if (tempoResposta <= 0)
        {
            throw new Exceptions.DomainException("O tempo de resposta deve ser maior que zero.");
        }

        if (tempoResolucao <= 0)
        {
            throw new Exceptions.DomainException("O tempo de resolução deve ser maior que zero.");
        }

        TempoResposta = tempoResposta;
        TempoResolucao = tempoResolucao;
    }
}
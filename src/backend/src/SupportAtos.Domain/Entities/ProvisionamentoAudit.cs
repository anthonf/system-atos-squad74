namespace SupportAtos.Domain.Entities;

public class ProvisionamentoAudit
{
    public int Id { get; private set; }

    public string CodigoExternoUsuario { get; private set; } = string.Empty;

    public string CodigoExternoGrupo { get; private set; } = string.Empty;

    public string Acao { get; private set; } = string.Empty;

    public DateTime Data { get; private set; }

    protected ProvisionamentoAudit()
    {
    }

    private ProvisionamentoAudit(string codigoExternoUsuario, string codigoExternoGrupo, string acao, DateTime data)
    {
        CodigoExternoUsuario = codigoExternoUsuario;
        CodigoExternoGrupo = codigoExternoGrupo;
        Acao = acao;
        Data = data;
    }

    public static ProvisionamentoAudit Registrar(string codigoExternoUsuario, string codigoExternoGrupo, string acao, DateTime? data = null)
    {
        if (string.IsNullOrWhiteSpace(codigoExternoUsuario))
        {
            throw new Exceptions.DomainException("O código externo do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(codigoExternoGrupo))
        {
            throw new Exceptions.DomainException("O código externo do grupo é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(acao))
        {
            throw new Exceptions.DomainException("A ação do log de provisionamento é obrigatória.");
        }

        return new ProvisionamentoAudit(codigoExternoUsuario.Trim(), codigoExternoGrupo.Trim(), acao.Trim(), data ?? DateTime.UtcNow);
    }
}
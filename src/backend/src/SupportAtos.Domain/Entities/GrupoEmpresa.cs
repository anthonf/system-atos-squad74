namespace SupportAtos.Domain.Entities;

public class GrupoEmpresa
{
    public int Id { get; private set; }

    public string? CodigoExterno { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Tipo { get; private set; } = "matriz";

    public int? GrupoPaiId { get; private set; }

    public GrupoEmpresa? GrupoPai { get; private set; }

    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public ICollection<Usuario> Usuarios { get; private set; } = new List<Usuario>();

    public ICollection<Chamado> Chamados { get; private set; } = new List<Chamado>();

    protected GrupoEmpresa()
    {
    }

    private GrupoEmpresa(string nome)
    {
        AlterarNome(nome);
    }

    public static GrupoEmpresa Criar(string nome) => new(nome);

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new Exceptions.DomainException("O nome do grupo de empresa é obrigatório.");
        }

        Nome = nome.Trim();
    }

    public void DefinirTipo(string tipo)
    {
        Tipo = string.IsNullOrWhiteSpace(tipo) ? "matriz" : tipo.Trim().ToLowerInvariant();
    }

    public void DefinirPai(int? grupoPaiId)
    {
        if (grupoPaiId.HasValue && grupoPaiId.Value == Id)
        {
            throw new Exceptions.DomainException("O grupo não pode ser pai de si mesmo.");
        }

        GrupoPaiId = grupoPaiId;
    }

    public void AtribuirCodigoExterno(string codigoExterno)
    {
        if (string.IsNullOrWhiteSpace(codigoExterno))
        {
            throw new Exceptions.DomainException("O código externo do grupo de empresa é obrigatório.");
        }

        CodigoExterno = codigoExterno.Trim();
    }

    public void DefinirIdPortal(int id)
    {
        if (id <= 0)
        {
            throw new Exceptions.DomainException("O ID do portal deve ser maior que zero.");
        }

        if (Id != 0 && Id != id)
        {
            throw new Exceptions.DomainException("O ID do grupo de empresa nao pode ser alterado.");
        }

        Id = id;
    }
}
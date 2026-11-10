using System.Text.RegularExpressions;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Domain.Entities;

public partial class Usuario
{
    public int Id { get; private set; }

    public string? CodigoExterno { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public int GrupoEmpresaId { get; private set; }

    public GrupoEmpresa? GrupoEmpresa { get; private set; }

    public PapelUsuario Papel { get; private set; }

    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    public DateTime? AtualizadoEm { get; private set; }

    public ICollection<Chamado> Chamados { get; private set; } = new List<Chamado>();

    public ICollection<Interacao> Interacoes { get; private set; } = new List<Interacao>();

    public ICollection<LogAuditoria> LogsAuditoria { get; private set; } = new List<LogAuditoria>();

    protected Usuario()
    {
    }

    private Usuario(string nome, string email, int grupoEmpresaId, PapelUsuario papel)
    {
        AlterarNome(nome);
        AlterarEmail(email);
        GrupoEmpresaId = grupoEmpresaId;
        Papel = papel;
    }

    public static Usuario Criar(string nome, string email, int grupoEmpresaId, PapelUsuario papel)
    {
        if (grupoEmpresaId <= 0)
        {
            throw new Exceptions.DomainException("O grupo de empresa é obrigatório.");
        }

        return new Usuario(nome, email, grupoEmpresaId, papel);
    }

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new Exceptions.DomainException("O nome do usuário é obrigatório.");
        }

        Nome = nome.Trim();
    }

    public void AlterarEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exceptions.DomainException("O e-mail do usuário é obrigatório.");
        }

        if (!EmailRegex().IsMatch(email))
        {
            throw new Exceptions.DomainException("O e-mail informado é inválido.");
        }

        Email = email.Trim().ToLowerInvariant();
    }

    public void AlterarPapel(PapelUsuario novoPapel)
    {
        Papel = novoPapel;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void AtribuirCodigoExterno(string codigoExterno)
    {
        if (string.IsNullOrWhiteSpace(codigoExterno))
        {
            throw new Exceptions.DomainException("O código externo do usuário é obrigatório.");
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
            throw new Exceptions.DomainException("O ID do usuario nao pode ser alterado.");
        }

        Id = id;
    }

    public void AlterarGrupoEmpresa(int grupoEmpresaId)
    {
        if (grupoEmpresaId <= 0)
        {
            throw new Exceptions.DomainException("O grupo de empresa é obrigatório.");
        }

        GrupoEmpresaId = grupoEmpresaId;
        AtualizadoEm = DateTime.UtcNow;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
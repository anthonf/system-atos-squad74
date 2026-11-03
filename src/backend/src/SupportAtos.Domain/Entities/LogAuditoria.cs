namespace SupportAtos.Domain.Entities;

public class LogAuditoria
{
    public int Id { get; private set; }

    public int ChamadoId { get; private set; }

    public Chamado? Chamado { get; private set; }

    public int? UsuarioId { get; private set; }

    public Usuario? Usuario { get; private set; }

    public string Acao { get; private set; } = string.Empty;

    public string? CampoAlterado { get; private set; }

    public string? ValorAnterior { get; private set; }

    public string? ValorNovo { get; private set; }

    public DateTime Data { get; private set; }

    protected LogAuditoria()
    {
    }

    private LogAuditoria(int chamadoId, int? usuarioId, string acao, string? campoAlterado, string? valorAnterior, string? valorNovo, DateTime data)
    {
        ChamadoId = chamadoId;
        UsuarioId = usuarioId;
        Acao = acao;
        CampoAlterado = campoAlterado;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        Data = data;
    }

    public static LogAuditoria Registrar(
        int chamadoId,
        int? usuarioId,
        string acao,
        string? campoAlterado = null,
        string? valorAnterior = null,
        string? valorNovo = null,
        DateTime? data = null)
    {
        if (chamadoId <= 0)
        {
            throw new Exceptions.DomainException("O chamado do log de auditoria é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(acao))
        {
            throw new Exceptions.DomainException("A ação do log de auditoria é obrigatória.");
        }

        return new LogAuditoria(chamadoId, usuarioId, acao.Trim(), campoAlterado, valorAnterior, valorNovo, data ?? DateTime.UtcNow);
    }
}
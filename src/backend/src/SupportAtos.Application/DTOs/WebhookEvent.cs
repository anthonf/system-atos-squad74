using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.DTOs;

public record WebhookEvent(
    string Evento,
    string CodigoPublico,
    string Titulo,
    string Produto,
    string Categoria,
    string Status,
    int UsuarioId,
    int GrupoEmpresaId,
    DateTime OcorridoEm)
{
    public const string EventoCriado = "chamado.criado";
    public const string EventoAtualizado = "chamado.atualizado";
    public const string EventoAguardandoCliente = "chamado.aguardando_cliente";
    public const string EventoFechado = "chamado.fechado";

    public static WebhookEvent De(Chamado chamado, string evento)
        => new(
            evento,
            chamado.CodigoPublico,
            chamado.Titulo,
            chamado.Produto,
            chamado.Categoria,
            chamado.Status.ToString(),
            chamado.UsuarioId,
            chamado.GrupoEmpresaId,
            DateTime.UtcNow);

    public static WebhookEvent De(ChamadoDto chamado, string evento)
        => new(
            evento,
            chamado.CodigoPublico,
            chamado.Titulo,
            chamado.Produto,
            chamado.Categoria,
            chamado.Status.ToString(),
            chamado.UsuarioId,
            chamado.GrupoEmpresaId,
            DateTime.UtcNow);
}

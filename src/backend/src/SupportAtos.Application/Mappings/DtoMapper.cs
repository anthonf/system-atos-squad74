using SupportAtos.Application.DTOs;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Mappings;

public static class DtoMapper
{
    public static ChamadoDto ToDto(
        Chamado chamado,
        string? usuarioNome = null,
        string? grupoEmpresaNome = null,
        string? agenteNome = null) => new(
        chamado.Id,
        chamado.CodigoPublico,
        chamado.UsuarioId,
        chamado.GrupoEmpresaId,
        chamado.AgenteId,
        chamado.Produto,
        chamado.Categoria,
        chamado.Titulo,
        chamado.Descricao,
        chamado.Status,
        chamado.Prioridade,
        chamado.SlaPrazo,
        chamado.SlaPrazoResposta,
        chamado.AguardandoDesde,
        chamado.CriadoEm,
        chamado.AtualizadoEm,
        chamado.FechadoEm,
        usuarioNome ?? chamado.Usuario?.Nome,
        grupoEmpresaNome ?? chamado.GrupoEmpresa?.Nome,
        agenteNome ?? chamado.Agente?.Nome);

    public static AnexoDto ToDto(Anexo anexo) => new(
        anexo.Id,
        anexo.NomeArquivo,
        anexo.Url,
        anexo.TamanhoBytes);

    public static InteracaoDto ToDto(
        Interacao interacao,
        string? usuarioNome = null,
        PapelUsuario? papelUsuario = null) => new(
        interacao.Id,
        interacao.UsuarioId,
        interacao.Tipo,
        interacao.Mensagem,
        interacao.CriadoEm,
        interacao.Anexos.OrderBy(a => a.Id).Select(ToDto).ToList(),
        usuarioNome ?? interacao.Usuario?.Nome,
        papelUsuario ?? interacao.Usuario?.Papel);

    public static UsuarioDto ToDto(Usuario usuario, string? grupoEmpresaNome = null) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email,
        usuario.GrupoEmpresaId,
        grupoEmpresaNome ?? usuario.GrupoEmpresa?.Nome,
        usuario.Papel);
}

using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record ChamadoDto(
    int Id,
    string CodigoPublico,
    int UsuarioId,
    int GrupoEmpresaId,
    int? AgenteId,
    string Produto,
    string Categoria,
    string Titulo,
    string Descricao,
    StatusChamado Status,
    Prioridade Prioridade,
    DateTime SlaPrazo,
    DateTime? SlaPrazoResposta,
    DateTime? AguardandoDesde,
    DateTime CriadoEm,
    DateTime? AtualizadoEm,
    DateTime? FechadoEm);

using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record InteracaoDto(
    int Id,
    int UsuarioId,
    TipoInteracao Tipo,
    string Mensagem,
    DateTime CriadoEm,
    IReadOnlyList<AnexoDto> Anexos);
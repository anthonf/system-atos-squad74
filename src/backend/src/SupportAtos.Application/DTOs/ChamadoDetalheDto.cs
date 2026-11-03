namespace SupportAtos.Application.DTOs;

public record ChamadoDetalheDto(ChamadoDto Chamado, IReadOnlyList<InteracaoDto> Interacoes);
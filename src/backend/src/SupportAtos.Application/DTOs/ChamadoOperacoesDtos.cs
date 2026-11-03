using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record ResponderChamadoInput(string Mensagem, TipoInteracao Tipo = TipoInteracao.Publica);

public record MudarStatusInput(StatusChamado NovoStatus);

public record AtribuirAgenteInput(int? AgenteId = null, bool Automatico = false);

public record FilaChamadosQuery(
    StatusChamado? Status = null,
    string? Categoria = null,
    string? Produto = null,
    int? AgenteId = null,
    int? GrupoEmpresaId = null,
    bool? SemAgente = null);

namespace SupportAtos.Application.DTOs;

public record CriarChamadoInput(
    string Produto,
    string Categoria,
    string MensagemInicial,
    string? Titulo = null,
    string? Descricao = null);

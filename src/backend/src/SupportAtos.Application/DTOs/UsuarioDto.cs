using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record UsuarioDto(
    int Id,
    string Nome,
    string Email,
    int GrupoEmpresaId,
    string? GrupoEmpresaNome,
    PapelUsuario Papel);
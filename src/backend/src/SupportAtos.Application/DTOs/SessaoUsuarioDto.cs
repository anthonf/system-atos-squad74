using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record SessaoUsuarioDto(
    int UsuarioId,
    string Nome,
    string Email,
    PapelUsuario Papel,
    int GrupoEmpresaId,
    string GrupoEmpresaNome,
    DateTime ExpiraEm,
    DateTime ServidorEm);

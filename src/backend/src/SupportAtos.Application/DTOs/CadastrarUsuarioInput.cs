using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record CadastrarUsuarioInput(string Nome, string Email, int GrupoEmpresaId, PapelUsuario Papel);
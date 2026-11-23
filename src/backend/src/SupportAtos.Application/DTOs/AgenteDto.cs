using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.DTOs;

public record AgenteDto(int Id, string Nome, PapelUsuario Papel);

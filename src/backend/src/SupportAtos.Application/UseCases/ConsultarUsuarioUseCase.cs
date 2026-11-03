using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;

namespace SupportAtos.Application.UseCases;

public class ConsultarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ConsultarUsuarioUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<UsuarioDto> Handle(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetComGrupoEmpresaPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Usuário '{id}' não encontrado.");

        return DtoMapper.ToDto(usuario);
    }
}
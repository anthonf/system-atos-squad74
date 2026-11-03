using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

public class ListarUsuariosPorGrupoUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUser _currentUser;

    public ListarUsuariosPorGrupoUseCase(IUsuarioRepository usuarioRepository, ICurrentUser currentUser)
    {
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UsuarioDto>> Handle(int grupoEmpresaId, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = _currentUser.UsuarioAtual;
        var grupoPermitido = usuarioAtual.Papel == PapelUsuario.Admin
            ? grupoEmpresaId
            : usuarioAtual.GrupoEmpresaIdInterno;

        var usuarios = await _usuarioRepository.ListarPorGrupoEmpresaAsync(grupoPermitido, cancellationToken);

        return usuarios.Select(u => DtoMapper.ToDto(u)).ToList();
    }
}
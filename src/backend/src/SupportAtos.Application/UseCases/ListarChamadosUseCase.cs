using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

/// <summary>Fila de atendimento: equipe vê tudo, cliente só o próprio grupo.</summary>
public class ListarChamadosUseCase
{
    private readonly ICurrentUser _currentUser;
    private readonly IChamadoRepository _chamadoRepository;

    public ListarChamadosUseCase(ICurrentUser currentUser, IChamadoRepository chamadoRepository)
    {
        _currentUser = currentUser;
        _chamadoRepository = chamadoRepository;
    }

    public async Task<IReadOnlyList<ChamadoDto>> Handle(FilaChamadosQuery filtro, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = _currentUser.UsuarioAtual;

        var filtroEfetivo = usuarioAtual.Papel == PapelUsuario.Cliente
            ? filtro with { GrupoEmpresaId = usuarioAtual.GrupoEmpresaIdInterno }
            : filtro;

        var chamados = await _chamadoRepository.ListarFilaAsync(filtroEfetivo, cancellationToken);
        return chamados.Select(DtoMapper.ToDto).ToList();
    }
}

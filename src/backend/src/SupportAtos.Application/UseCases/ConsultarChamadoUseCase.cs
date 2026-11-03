using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

public class ConsultarChamadoUseCase
{
    private readonly IChamadoRepository _chamadoRepository;
    private readonly ICurrentUser _currentUser;

    public ConsultarChamadoUseCase(IChamadoRepository chamadoRepository, ICurrentUser currentUser)
    {
        _chamadoRepository = chamadoRepository;
        _currentUser = currentUser;
    }

    public async Task<ChamadoDetalheDto> Handle(string codigoPublico, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = _currentUser.UsuarioAtual;
        var chamado = await _chamadoRepository.GetByCodigoPublicoComHistoricoAsync(codigoPublico, cancellationToken)
            ?? throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");

        if (usuarioAtual.Papel == PapelUsuario.Cliente && chamado.GrupoEmpresaId != usuarioAtual.GrupoEmpresaIdInterno)
        {
            throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");
        }

        // Cliente vê só conversa pública; agente/supervisor/admin veem notas internas também.
        var visiveis = usuarioAtual.Papel == PapelUsuario.Cliente
            ? chamado.Interacoes.Where(i => i.Tipo == TipoInteracao.Publica)
            : chamado.Interacoes;

        var interacoes = visiveis
            .OrderBy(i => i.CriadoEm)
            .Select(DtoMapper.ToDto)
            .ToList();

        return new ChamadoDetalheDto(DtoMapper.ToDto(chamado), interacoes);
    }
}

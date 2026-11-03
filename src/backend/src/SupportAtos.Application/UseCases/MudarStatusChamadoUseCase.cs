using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

/// <summary>Transição de estado do chamado (aberto, em andamento, aguardando cliente, resolvido, fechado).</summary>
public class MudarStatusChamadoUseCase
{
    private readonly ICurrentUser _currentUser;
    private readonly IChamadoRepository _chamadoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MudarStatusChamadoUseCase(
        ICurrentUser currentUser,
        IChamadoRepository chamadoRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _chamadoRepository = chamadoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChamadoDto> Handle(string codigoPublico, MudarStatusInput input, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = _currentUser.UsuarioAtual;
        var chamado = await _chamadoRepository.GetByCodigoPublicoAsync(codigoPublico, cancellationToken)
            ?? throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");

        if (usuarioAtual.Papel == PapelUsuario.Cliente)
        {
            if (chamado.GrupoEmpresaId != usuarioAtual.GrupoEmpresaIdInterno)
            {
                throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");
            }

            // Cliente só pode reabrir um resolvido; demais transições são da equipe.
            var reabertura = chamado.Status == StatusChamado.Resolvido && input.NovoStatus == StatusChamado.Aberto;
            if (!reabertura)
            {
                throw new ForbiddenException("Apenas a equipe de suporte pode alterar o status do chamado.");
            }
        }

        chamado.MudarStatus(input.NovoStatus, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(chamado);
    }
}

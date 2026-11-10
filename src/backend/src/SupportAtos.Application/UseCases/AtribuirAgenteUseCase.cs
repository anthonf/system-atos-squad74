using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

/// <summary>
/// Atribuição de chamados: supervisor/administrador pode atribuir a qualquer agente,
/// usar atribuição automática ou desatribuir. O agente só pode atribuir o chamado a si mesmo.
/// </summary>
public class AtribuirAgenteUseCase
{
    private readonly ICurrentUser _currentUser;
    private readonly IChamadoRepository _chamadoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AtribuirAgenteUseCase(
        ICurrentUser currentUser,
        IChamadoRepository chamadoRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _chamadoRepository = chamadoRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChamadoDto> Handle(string codigoPublico, AtribuirAgenteInput input, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = _currentUser.UsuarioAtual;
        if (usuarioAtual.Papel == PapelUsuario.Cliente)
        {
            throw new ForbiddenException("Apenas a equipe de suporte pode atribuir chamados.");
        }

        var ehGestor = usuarioAtual.Papel is PapelUsuario.Supervisor or PapelUsuario.Admin;

        if (!ehGestor)
        {
            if (input.Desatribuir)
            {
                throw new ForbiddenException("Apenas supervisor ou administrador pode desatribuir chamados.");
            }

            if (input.Automatico)
            {
                throw new ForbiddenException("Apenas supervisor ou administrador pode usar atribuição automática.");
            }

            if (!input.AgenteId.HasValue)
            {
                throw new ValidationException("Informe o agente para atribuição.");
            }

            if (input.AgenteId.Value != usuarioAtual.UsuarioIdInterno)
            {
                throw new ForbiddenException("Agente só pode atribuir chamados a si mesmo.");
            }
        }

        var chamado = await _chamadoRepository.GetByCodigoPublicoAsync(codigoPublico, cancellationToken)
            ?? throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");

        if (input.Desatribuir)
        {
            if (input.Automatico || input.AgenteId.HasValue)
            {
                throw new ValidationException("A desatribuição não pode ser combinada com agente ou atribuição automática.");
            }

            chamado.AtribuirAgente(null, DateTime.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return DtoMapper.ToDto(chamado);
        }

        int? agenteId = input.AgenteId;

        if (input.Automatico || !agenteId.HasValue)
        {
            if (!input.Automatico && !agenteId.HasValue)
            {
                throw new ValidationException("Informe o agente, use atribuição automática ou solicite a desatribuição.");
            }

            agenteId = await EscolherAgenteComMenorCargaAsync(cancellationToken)
                ?? throw new ValidationException("Nenhum agente disponível para atribuição automática.");
        }
        else
        {
            var agente = await _usuarioRepository.GetByIdAsync(agenteId.Value, cancellationToken)
                ?? throw new ValidationException($"Agente '{agenteId}' não encontrado.");

            if (agente.Papel is not (PapelUsuario.Agente or PapelUsuario.Supervisor or PapelUsuario.Admin))
            {
                throw new ValidationException("O usuário atribuído precisa ser da equipe (agente, supervisor ou admin).");
            }
        }

        if (!ehGestor
            && chamado.AgenteId.HasValue
            && chamado.AgenteId != usuarioAtual.UsuarioIdInterno)
        {
            throw new ForbiddenException(
                "O chamado já está atribuído a outro agente. Peça ao supervisor para reatribuí-lo.");
        }

        chamado.AtribuirAgente(agenteId, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(chamado);
    }

    private async Task<int?> EscolherAgenteComMenorCargaAsync(CancellationToken cancellationToken)
    {
        var agentes = await _usuarioRepository.ListarAgentesAsync(cancellationToken);
        if (agentes.Count == 0)
        {
            return null;
        }

        var melhor = -1;
        var menorCarga = int.MaxValue;

        foreach (var agente in agentes)
        {
            var carga = await _chamadoRepository.ContarAbertosPorAgenteAsync(agente.Id, cancellationToken);
            if (carga < menorCarga)
            {
                menorCarga = carga;
                melhor = agente.Id;
            }
        }

        return melhor;
    }
}

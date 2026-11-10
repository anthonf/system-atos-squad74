using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

/// <summary>Agente responde ao cliente (pública) ou registra nota interna (só equipe).</summary>
public class ResponderChamadoUseCase
{
    private readonly ICurrentUser _currentUser;
    private readonly IChamadoRepository _chamadoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebhookDispatcher _webhook;

    public ResponderChamadoUseCase(
        ICurrentUser currentUser,
        IChamadoRepository chamadoRepository,
        IUnitOfWork unitOfWork,
        IWebhookDispatcher webhook)
    {
        _currentUser = currentUser;
        _chamadoRepository = chamadoRepository;
        _unitOfWork = unitOfWork;
        _webhook = webhook;
    }

    public async Task<InteracaoDto> Handle(string codigoPublico, ResponderChamadoInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Mensagem))
        {
            throw new ValidationException("A mensagem é obrigatória.");
        }

        var usuarioAtual = _currentUser.UsuarioAtual;
        var chamado = await _chamadoRepository.GetByCodigoPublicoComHistoricoAsync(codigoPublico, cancellationToken)
            ?? throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");

        if (usuarioAtual.Papel == PapelUsuario.Cliente)
        {
            if (chamado.GrupoEmpresaId != usuarioAtual.GrupoEmpresaIdInterno)
            {
                throw new NotFoundException($"Chamado '{codigoPublico}' não encontrado.");
            }

            if (input.Tipo == TipoInteracao.NotaInterna)
            {
                throw new ForbiddenException("Clientes não podem criar notas internas.");
            }
        }

        if (chamado.Status == StatusChamado.Fechado)
        {
            throw new ValidationException("Chamados fechados não aceitam novas interações. Reabra o chamado primeiro.");
        }

        var agora = DateTime.UtcNow;
        var interacao = chamado.AdicionarInteracao(usuarioAtual.UsuarioIdInterno, input.Tipo, input.Mensagem, agora);

        // Primeira resposta da equipe tira o chamado do Aberto automaticamente.
        if (chamado.Status == StatusChamado.Aberto && usuarioAtual.Papel != PapelUsuario.Cliente)
        {
            chamado.MudarStatus(StatusChamado.EmAndamento, agora);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _webhook.EnviarAsync(WebhookEvent.De(chamado, WebhookEvent.EventoAtualizado), cancellationToken);

        return DtoMapper.ToDto(interacao);
    }
}

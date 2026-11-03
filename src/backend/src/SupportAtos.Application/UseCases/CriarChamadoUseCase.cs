using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

public class CriarChamadoUseCase
{
    private readonly ICurrentUser _currentUser;
    private readonly ISlaCategoriaRepository _slaCategoriaRepository;
    private readonly IChamadoRepository _chamadoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarChamadoUseCase(
        ICurrentUser currentUser,
        ISlaCategoriaRepository slaCategoriaRepository,
        IChamadoRepository chamadoRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _slaCategoriaRepository = slaCategoriaRepository;
        _chamadoRepository = chamadoRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChamadoDto> Handle(CriarChamadoInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Produto))
        {
            throw new ValidationException("O produto é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(input.Categoria))
        {
            throw new ValidationException("A categoria é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(input.MensagemInicial))
        {
            throw new ValidationException("A mensagem inicial é obrigatória.");
        }

        var usuarioAtual = _currentUser.UsuarioAtual;

        var sla = await _slaCategoriaRepository.GetByProdutoECategoriaAsync(input.Produto, input.Categoria, cancellationToken)
            ?? throw new ValidationException(
                $"Nenhuma categoria de SLA encontrada para o produto '{input.Produto}' e categoria '{input.Categoria}'.");

        var codigoPublico = await GerarCodigoPublicoUnicoAsync(cancellationToken);

        var chamado = Chamado.Criar(
            codigoPublico,
            usuarioAtual.UsuarioIdInterno,
            usuarioAtual.GrupoEmpresaIdInterno,
            sla,
            input.Produto,
            input.Categoria,
            input.Titulo ?? string.Empty,
            string.IsNullOrWhiteSpace(input.Descricao) ? input.MensagemInicial : input.Descricao);
        chamado.AdicionarInteracao(usuarioAtual.UsuarioIdInterno, TipoInteracao.Publica, input.MensagemInicial, DateTime.UtcNow);

        await _chamadoRepository.AddAsync(chamado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(chamado);
    }

    private async Task<string> GerarCodigoPublicoUnicoAsync(CancellationToken cancellationToken)
    {
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var codigo = $"SA-{Random.Shared.Next(0, 1_000_000_000):D9}";
            if (!await _chamadoRepository.CodigoPublicoExisteAsync(codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new ConflictException("Não foi possível gerar um código público único para o chamado.");
    }
}

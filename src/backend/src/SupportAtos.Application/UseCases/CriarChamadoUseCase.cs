using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Validation;
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

    public Task<ChamadoDto> Handle(CriarChamadoInput input, CancellationToken cancellationToken = default)
        => Handle(input, _currentUser.UsuarioAtual, null, cancellationToken);

    public async Task<ChamadoDto> Handle(
        CriarChamadoInput input,
        PortalUserContext usuarioAtual,
        IReadOnlyList<AnexoInput>? anexos = null,
        CancellationToken cancellationToken = default)
    {
        var produto = ValidacaoTexto.Obrigatorio(input.Produto, "produto", 100);
        var categoria = ValidacaoTexto.Obrigatorio(input.Categoria, "categoria", 100);
        var titulo = string.IsNullOrWhiteSpace(input.Titulo)
            ? throw new ValidationException("O título é obrigatório.")
            : ValidacaoTexto.Obrigatorio(input.Titulo, "titulo", 200);
        var mensagem = ValidacaoTexto.Obrigatorio(input.MensagemInicial, "mensagemInicial", 5000);
        var descricao = ValidacaoTexto.Opcional(input.Descricao, "descricao", 5000) ?? mensagem;

        var sla = await _slaCategoriaRepository.GetByProdutoECategoriaAsync(produto, categoria, cancellationToken)
            ?? throw new ValidationException(
                $"Nenhuma categoria de SLA encontrada para o produto '{produto}' e categoria '{categoria}'.");

        var codigoPublico = await GerarCodigoPublicoUnicoAsync(cancellationToken);

        var chamado = Chamado.Criar(
            codigoPublico,
            usuarioAtual.UsuarioIdInterno,
            usuarioAtual.GrupoEmpresaIdInterno,
            sla,
            produto,
            categoria,
            titulo,
            descricao);
        var interacao = chamado.AdicionarInteracao(
            usuarioAtual.UsuarioIdInterno, TipoInteracao.Publica, mensagem, DateTime.UtcNow);

        if (anexos is not null)
        {
            foreach (var anexo in anexos)
            {
                interacao.AdicionarAnexo(anexo.NomeArquivo!, anexo.Url!, anexo.TamanhoBytes);
            }
        }

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

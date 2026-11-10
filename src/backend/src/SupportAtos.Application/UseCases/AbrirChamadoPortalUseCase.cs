using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Validation;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.UseCases;

public class AbrirChamadoPortalUseCase
{
    private const int TamanhoMaximoMensagem = 5000;
    private const int TamanhoMaximoUrlAnexo = 2048;
    private const int TamanhoMaximoNomeAnexo = 255;
    private const int TamanhoMaximoAnexoBytes = 50 * 1024 * 1024;

    private readonly IUserProvisioningService _provisioning;
    private readonly CriarChamadoUseCase _criarChamado;
    private readonly IWebhookDispatcher _webhook;

    public AbrirChamadoPortalUseCase(
        IUserProvisioningService provisioning,
        CriarChamadoUseCase criarChamado,
        IWebhookDispatcher webhook)
    {
        _provisioning = provisioning;
        _criarChamado = criarChamado;
        _webhook = webhook;
    }

    public async Task<ChamadoDto> Handle(AbrirChamadoPortalInput input, CancellationToken cancellationToken = default)
    {
        if (input.CamposExtras is { Count: > 0 })
        {
            throw new ValidationException(
                $"Campos não permitidos no corpo: {string.Join(", ", input.CamposExtras.Keys)}.");
        }

        var usuario = input.Usuario
            ?? throw new ValidationException("O campo 'usuario' é obrigatório.");
        var grupo = input.GrupoEmpresa
            ?? throw new ValidationException("O campo 'grupoEmpresa' é obrigatório.");

        var codigoUsuario = PortalIdentificadorValidator.Validar(usuario.Id, "usuario.id");
        var codigoGrupo = PortalIdentificadorValidator.Validar(grupo.Id, "grupoEmpresa.id");
        var nomeUsuario = ValidacaoTexto.Obrigatorio(usuario.Nome, "usuario.nome", 150);
        var email = ValidacaoTexto.Email(usuario.Email);
        var nomeGrupo = ValidacaoTexto.Obrigatorio(grupo.Nome, "grupoEmpresa.nome", 150);

        var produto = ValidacaoTexto.Obrigatorio(input.Produto, "produto", 100);
        var categoria = ValidacaoTexto.Obrigatorio(input.Categoria, "categoria", 100);
        var titulo = string.IsNullOrWhiteSpace(input.Titulo)
            ? throw new ValidationException("O título é obrigatório.")
            : ValidacaoTexto.Obrigatorio(input.Titulo, "titulo", 200);
        var mensagem = ValidacaoTexto.Obrigatorio(
            input.MensagemInicial ?? input.Descricao, "mensagemInicial", TamanhoMaximoMensagem);
        var descricao = ValidacaoTexto.Opcional(input.Descricao, "descricao", TamanhoMaximoMensagem) ?? mensagem;
        var anexos = ValidarAnexos(input.Anexos);

        var contextoExterno = new PortalUserContext(
            0,
            0,
            codigoUsuario,
            codigoGrupo,
            email,
            nomeUsuario,
            nomeGrupo,
            PapelUsuario.Cliente);

        var provisionado = await _provisioning.ProvisionPortalAsync(contextoExterno, cancellationToken);
        var contextoInterno = contextoExterno with
        {
            UsuarioIdInterno = provisionado.UsuarioIdInterno,
            GrupoEmpresaIdInterno = provisionado.GrupoEmpresaIdInterno
        };

        var chamado = await _criarChamado.Handle(
            new CriarChamadoInput(produto, categoria, mensagem, titulo, descricao),
            contextoInterno,
            anexos,
            cancellationToken);

        await _webhook.EnviarAsync(WebhookEvent.De(chamado, WebhookEvent.EventoCriado), cancellationToken);

        return chamado;
    }

    private static IReadOnlyList<AnexoInput>? ValidarAnexos(List<AnexoInput>? anexos)
    {
        if (anexos is null || anexos.Count == 0)
        {
            return null;
        }

        var validados = new List<AnexoInput>(anexos.Count);
        foreach (var anexo in anexos)
        {
            var nome = ValidacaoTexto.Obrigatorio(anexo.NomeArquivo, "anexos.nomeArquivo", TamanhoMaximoNomeAnexo);
            var url = ValidacaoTexto.Obrigatorio(anexo.Url, "anexos.url", TamanhoMaximoUrlAnexo);

            if (anexo.TamanhoBytes < 0 || anexo.TamanhoBytes > TamanhoMaximoAnexoBytes)
            {
                throw new ValidationException(
                    $"O campo 'anexos.tamanhoBytes' deve estar entre 0 e {TamanhoMaximoAnexoBytes}.");
            }

            validados.Add(new AnexoInput
            {
                NomeArquivo = nome,
                Url = url,
                TamanhoBytes = anexo.TamanhoBytes
            });
        }

        return validados;
    }
}

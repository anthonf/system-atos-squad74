using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ResponderChamadoUseCaseTests
{
    private static Chamado NovoChamado()
    {
        var chamado = Chamado.Criar(
            "SA-1", 1, 1, SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Chamado de teste");
        chamado.SetIdForTest(1);
        return chamado;
    }

    private static ResponderChamadoUseCase CriarUseCase(
        FakeChamadoRepository chamados,
        PapelUsuario papel = PapelUsuario.Agente,
        int usuarioId = 2,
        string nome = "Diego Ramos",
        int grupoEmpresaId = 1)
    {
        var currentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with
            {
                Papel = papel,
                UsuarioIdInterno = usuarioId,
                Nome = nome,
                GrupoEmpresaIdInterno = grupoEmpresaId
            }
        };
        return new ResponderChamadoUseCase(
            currentUser, chamados, new FakeUnitOfWork(), new FakeWebhookDispatcher());
    }

    [Fact]
    public async Task Handle_PrimeiraRespostaDaEquipe_DeveMoverParaEmAndamentoERetornarAutor()
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(NovoChamado());
        var useCase = CriarUseCase(chamados);

        var interacao = await useCase.Handle("SA-1", new ResponderChamadoInput("Já estamos verificando.", TipoInteracao.Publica));

        Assert.Equal(StatusChamado.EmAndamento, Assert.Single(chamados.Chamados).Status);
        Assert.Equal("Diego Ramos", interacao.UsuarioNome);
        Assert.Equal(PapelUsuario.Agente, interacao.PapelUsuario);
        Assert.Equal("Já estamos verificando.", interacao.Mensagem);
    }

    [Fact]
    public async Task Handle_ComNotaInterna_DeveRegistrarSemAlterarMensagensPublicas()
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(NovoChamado());
        var useCase = CriarUseCase(chamados, papel: PapelUsuario.Supervisor, nome: "Marina Alves");

        var interacao = await useCase.Handle("SA-1", new ResponderChamadoInput("Verificar log.", TipoInteracao.NotaInterna));

        Assert.Equal(TipoInteracao.NotaInterna, interacao.Tipo);
        Assert.Equal("Marina Alves", interacao.UsuarioNome);
        Assert.Equal(PapelUsuario.Supervisor, interacao.PapelUsuario);
    }

    [Theory]
    [InlineData(PapelUsuario.Agente)]
    [InlineData(PapelUsuario.Cliente)]
    public async Task Handle_ComChamadoFechado_DeveLancarValidationException(PapelUsuario papel)
    {
        var chamado = NovoChamado();
        chamado.MudarStatus(StatusChamado.Fechado, DateTime.UtcNow);
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(chamado);
        var useCase = CriarUseCase(chamados, papel: papel);

        var excecao = await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle("SA-1", new ResponderChamadoInput("Nova mensagem.")));

        Assert.Contains("fechados", excecao.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ComClienteNotaInterna_DeveLancarForbiddenException()
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(NovoChamado());
        var useCase = CriarUseCase(chamados, papel: PapelUsuario.Cliente, usuarioId: 1);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.Handle("SA-1", new ResponderChamadoInput("Nota.", TipoInteracao.NotaInterna)));
    }

    [Fact]
    public async Task Handle_ClienteDoMesmoGrupo_DevePersistirRespostaPublicaSemForcarStatus()
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(NovoChamado());
        var useCase = CriarUseCase(chamados, papel: PapelUsuario.Cliente, usuarioId: 3, nome: "Cliente do grupo");

        var resposta = await useCase.Handle("SA-1", new ResponderChamadoInput("Resposta pública", TipoInteracao.Publica));

        var chamado = Assert.Single(chamados.Chamados);
        Assert.Equal("Resposta pública", Assert.Single(chamado.Interacoes).Mensagem);
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Equal(PapelUsuario.Cliente, resposta.PapelUsuario);
        Assert.Equal(3, resposta.UsuarioId);
    }

    [Fact]
    public async Task Handle_ClienteDeOutroGrupo_DeveNegarSemAdicionarInteracao()
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(NovoChamado());
        var useCase = CriarUseCase(chamados, papel: PapelUsuario.Cliente, grupoEmpresaId: 2);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.Handle("SA-1", new ResponderChamadoInput("Sem autorização", TipoInteracao.Publica)));

        Assert.Empty(Assert.Single(chamados.Chamados).Interacoes);
    }

    private sealed class FakeWebhookDispatcher : IWebhookDispatcher
    {
        public Task EnviarAsync(WebhookEvent evento, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

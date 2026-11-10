using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class CriarChamadoUseCaseTests
{
    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarChamadoComCodigoPublicoEInteracaoInicial()
    {
        var fakeChamados = new FakeChamadoRepository();
        var fakeCurrentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao
        };

        var useCase = new CriarChamadoUseCase(
            fakeCurrentUser,
            new FakeSlaCategoriaRepository(),
            fakeChamados,
            new FakeUnitOfWork());

        var resultado = await useCase.Handle(new CriarChamadoInput(
            "Portal", "Login", "Não consigo acessar o sistema.", "Erro ao acessar o portal"));

        Assert.StartsWith("SA-", resultado.CodigoPublico);
        Assert.Equal(StatusChamado.Aberto, resultado.Status);
        Assert.Equal(Prioridade.Alta, resultado.Prioridade);
        var chamado = Assert.Single(fakeChamados.Chamados);
        Assert.Single(chamado.Interacoes);
        Assert.Equal("Não consigo acessar o sistema.", chamado.Interacoes.Single().Mensagem);
        Assert.Equal(FakeCurrentUser.ContextoPadrao.UsuarioIdInterno, chamado.Interacoes.Single().UsuarioId);
    }

    [Fact]
    public async Task Handle_SemSlaParaProdutoECategoria_DeveLancarValidationException()
    {
        var useCase = new CriarChamadoUseCase(
            new FakeCurrentUser(),
            new FakeSlaCategoriaRepository(),
            new FakeChamadoRepository(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle(new CriarChamadoInput("ProdutoInexistente", "CategoriaInexistente", "Alguma mensagem.", "Título")));
    }

    [Fact]
    public async Task Handle_SemUsuarioAutenticado_DeveLancarInvalidOperationException()
    {
        var useCase = new CriarChamadoUseCase(
            new FakeCurrentUserNaoAutenticado(),
            new FakeSlaCategoriaRepository(),
            new FakeChamadoRepository(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.Handle(new CriarChamadoInput("Portal", "Login", "Mensagem.", "Título")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_SemTitulo_DeveLancarValidationException(string? titulo)
    {
        var fakeChamados = new FakeChamadoRepository();
        var useCase = new CriarChamadoUseCase(
            new FakeCurrentUser { Usuario = FakeCurrentUser.ContextoPadrao },
            new FakeSlaCategoriaRepository(),
            fakeChamados,
            new FakeUnitOfWork());

        var excecao = await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle(new CriarChamadoInput("Portal", "Login", "Não consigo acessar o sistema.", titulo)));

        Assert.Equal("O título é obrigatório.", excecao.Message);
        Assert.Empty(fakeChamados.Chamados);
    }

    [Fact]
    public async Task Handle_ComTitulo_DevePersistirTituloNoChamado()
    {
        var fakeChamados = new FakeChamadoRepository();
        var useCase = new CriarChamadoUseCase(
            new FakeCurrentUser { Usuario = FakeCurrentUser.ContextoPadrao },
            new FakeSlaCategoriaRepository(),
            fakeChamados,
            new FakeUnitOfWork());

        var resultado = await useCase.Handle(
            new CriarChamadoInput("Portal", "Login", "Não consigo acessar o sistema.", "  Erro ao acessar o portal  "));

        Assert.Equal("Erro ao acessar o portal", resultado.Titulo);
        Assert.Equal("Erro ao acessar o portal", Assert.Single(fakeChamados.Chamados).Titulo);
    }

    private sealed class FakeCurrentUserNaoAutenticado : ICurrentUser
    {
        public bool IsAutenticado => false;

        public PortalUserContext UsuarioAtual
            => throw new InvalidOperationException("Requisição sem usuário do portal autenticado/provisionado.");
    }
}
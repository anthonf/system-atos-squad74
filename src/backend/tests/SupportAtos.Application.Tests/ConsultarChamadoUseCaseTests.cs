using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ConsultarChamadoUseCaseTests
{
    private static Chamado NovoChamado()
    {
        var chamado = Chamado.Criar(
            "SA-1", 1, 1, SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Chamado de teste");
        chamado.SetIdForTest(1);
        return chamado;
    }

    private static async Task<FakeChamadoRepository> PrepararRepositorioAsync()
    {
        var chamado = NovoChamado();
        var cliente = Usuario.Criar("Marina Alves", "marina@atos.com", 1, PapelUsuario.Cliente);
        cliente.SetIdForTest(1);
        var agente = Usuario.Criar("Diego Ramos", "diego@atos.com", 1, PapelUsuario.Agente);
        agente.SetIdForTest(2);

        var publica = chamado.AdicionarInteracao(1, TipoInteracao.Publica, "Mensagem do cliente", DateTime.UtcNow);
        var nota = chamado.AdicionarInteracao(2, TipoInteracao.NotaInterna, "Nota da equipe", DateTime.UtcNow.AddMinutes(1));
        publica.SetForTest(nameof(Interacao.Usuario), cliente);
        nota.SetForTest(nameof(Interacao.Usuario), agente);
        chamado.SetForTest(nameof(Chamado.Usuario), cliente);

        var repositorio = new FakeChamadoRepository();
        await repositorio.AddAsync(chamado);
        return repositorio;
    }

    [Fact]
    public async Task Handle_ComAgente_DeveRetornarInteracoesComAutorENotaInterna()
    {
        var repositorio = await PrepararRepositorioAsync();
        var currentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with { Papel = PapelUsuario.Agente }
        };
        var useCase = new ConsultarChamadoUseCase(repositorio, currentUser);

        var detalhe = await useCase.Handle("SA-1");

        Assert.Equal(2, detalhe.Interacoes.Count);
        Assert.All(detalhe.Interacoes, i =>
        {
            Assert.NotNull(i.UsuarioNome);
            Assert.NotNull(i.PapelUsuario);
        });
        Assert.Contains(detalhe.Interacoes, i => i.Tipo == TipoInteracao.NotaInterna && i.UsuarioNome == "Diego Ramos");
        Assert.Equal("Marina Alves", detalhe.Chamado.UsuarioNome);
    }

    [Fact]
    public async Task Handle_ComCliente_DeveOcultarNotasInternas()
    {
        var repositorio = await PrepararRepositorioAsync();
        var currentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with
            {
                Papel = PapelUsuario.Cliente,
                GrupoEmpresaIdInterno = 1
            }
        };
        var useCase = new ConsultarChamadoUseCase(repositorio, currentUser);

        var detalhe = await useCase.Handle("SA-1");

        var interacao = Assert.Single(detalhe.Interacoes);
        Assert.Equal(TipoInteracao.Publica, interacao.Tipo);
        Assert.Equal("Mensagem do cliente", interacao.Mensagem);
    }

    [Fact]
    public async Task Handle_ComClienteDeOutraEmpresa_DeveLancarNotFoundException()
    {
        var repositorio = await PrepararRepositorioAsync();
        var currentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with
            {
                Papel = PapelUsuario.Cliente,
                GrupoEmpresaIdInterno = 999
            }
        };
        var useCase = new ConsultarChamadoUseCase(repositorio, currentUser);

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.Handle("SA-1"));
    }
}

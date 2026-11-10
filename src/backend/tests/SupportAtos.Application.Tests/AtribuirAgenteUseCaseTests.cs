using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class AtribuirAgenteUseCaseTests
{
    private const int IdAgente = 2;
    private const int IdOutroAgente = 3;

    private static Chamado NovoChamado()
    {
        var chamado = Chamado.Criar(
            "SA-1", 1, 1,
            SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Chamado de teste");
        chamado.SetIdForTest(1);
        return chamado;
    }

    private static async Task<(AtribuirAgenteUseCase useCase, FakeChamadoRepository chamados)> PrepararAsync(
        PapelUsuario papel,
        Chamado? chamado = null,
        int actorId = 1)
    {
        var chamados = new FakeChamadoRepository();
        await chamados.AddAsync(chamado ?? NovoChamado());

        var usuarios = new FakeUsuarioRepository();

        var agente = Usuario.Criar("Agente", "agente@atos.com", 1, PapelUsuario.Agente);
        agente.SetIdForTest(IdAgente);
        await usuarios.AddAsync(agente);

        var outroAgente = Usuario.Criar("Outro Agente", "outro.agente@atos.com", 1, PapelUsuario.Agente);
        outroAgente.SetIdForTest(IdOutroAgente);
        await usuarios.AddAsync(outroAgente);

        var currentUser = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with { Papel = papel, UsuarioIdInterno = actorId }
        };

        var useCase = new AtribuirAgenteUseCase(currentUser, chamados, usuarios, new FakeUnitOfWork());
        return (useCase, chamados);
    }

    [Fact]
    public async Task Handle_ComSupervisor_DeveAtribuirAQualquerAgente()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Supervisor);

        var resultado = await useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdAgente));

        Assert.Equal(IdAgente, resultado.AgenteId);
    }

    [Fact]
    public async Task Handle_ComAdmin_DeveAtribuirAQualquerAgente()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Admin);

        var resultado = await useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdAgente));

        Assert.Equal(IdAgente, resultado.AgenteId);
    }

    [Fact]
    public async Task Handle_ComAgente_DevePermitirAutoAtribuicao()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Agente, actorId: IdAgente);

        var resultado = await useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdAgente));

        Assert.Equal(IdAgente, resultado.AgenteId);
    }

    [Fact]
    public async Task Handle_ComAgenteTentandoAtribuirParaOutro_DeveLancarForbiddenException()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Agente, actorId: IdAgente);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdOutroAgente)));
    }

    [Fact]
    public async Task Handle_ComAgenteEmChamadoDeOutroAgente_DeveLancarForbiddenException()
    {
        var chamado = NovoChamado();
        chamado.AtribuirAgente(IdOutroAgente, DateTime.UtcNow);
        var (useCase, _) = await PrepararAsync(PapelUsuario.Agente, chamado, actorId: IdAgente);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdAgente)));
    }

    [Fact]
    public async Task Handle_ComAgentePedindoDesatribuicao_DeveLancarForbiddenException()
    {
        var chamado = NovoChamado();
        chamado.AtribuirAgente(IdAgente, DateTime.UtcNow);
        var (useCase, _) = await PrepararAsync(PapelUsuario.Agente, chamado, actorId: IdAgente);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput(Desatribuir: true)));
    }

    [Fact]
    public async Task Handle_ComAgenteUsandoAutomatico_DeveLancarForbiddenException()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Agente, actorId: IdAgente);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput(Automatico: true)));
    }

    [Fact]
    public async Task Handle_ComDesatribuir_DeveLimparAgente()
    {
        var chamado = NovoChamado();
        chamado.AtribuirAgente(IdAgente, DateTime.UtcNow);
        var (useCase, _) = await PrepararAsync(PapelUsuario.Supervisor, chamado);

        var resultado = await useCase.Handle("SA-1", new AtribuirAgenteInput(Desatribuir: true));

        Assert.Null(resultado.AgenteId);
    }

    [Fact]
    public async Task Handle_DesatribuirComAgente_DeveLancarValidationException()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Supervisor);

        await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput(AgenteId: IdAgente, Desatribuir: true)));
    }

    [Fact]
    public async Task Handle_SemAgenteNemAutomaticoNemDesatribuir_DeveLancarValidationException()
    {
        var (useCase, _) = await PrepararAsync(PapelUsuario.Supervisor);

        await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle("SA-1", new AtribuirAgenteInput()));
    }
}

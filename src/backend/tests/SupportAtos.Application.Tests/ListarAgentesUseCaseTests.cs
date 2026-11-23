using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ListarAgentesUseCaseTests
{
    [Fact]
    public async Task Handle_DeveListarApenasMembrosDaEquipeComNomeEPapel()
    {
        var usuarios = new FakeUsuarioRepository();
        await usuarios.AddAsync(Usuario.Criar("Cliente", "cliente@atos.com", 1, PapelUsuario.Cliente));
        await usuarios.AddAsync(Usuario.Criar("Agente", "agente@atos.com", 1, PapelUsuario.Agente));
        await usuarios.AddAsync(Usuario.Criar("Supervisor", "supervisor@atos.com", 1, PapelUsuario.Supervisor));
        await usuarios.AddAsync(Usuario.Criar("Admin", "admin@atos.com", 1, PapelUsuario.Admin));
        var useCase = new ListarAgentesUseCase(usuarios);

        var agentes = await useCase.Handle();

        Assert.Equal(3, agentes.Count);
        Assert.Contains(agentes, a => a.Nome == "Agente" && a.Papel == PapelUsuario.Agente && a.Id > 0);
        Assert.Contains(agentes, a => a.Nome == "Supervisor" && a.Papel == PapelUsuario.Supervisor);
        Assert.Contains(agentes, a => a.Nome == "Admin" && a.Papel == PapelUsuario.Admin);
        Assert.DoesNotContain(agentes, a => a.Nome == "Cliente");
    }
}

using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ConsultarUsuarioUseCaseTests
{
    [Fact]
    public async Task Handle_ComUsuarioExistente_DeveRetornarUsuarioComNomeDoGrupo()
    {
        var usuario = Usuario.Criar("Fulano", "fulano@demo.com", 1, PapelUsuario.Cliente);
        usuario.SetIdForTest(1);
        usuario.SetForTest("GrupoEmpresa", GrupoEmpresa.Criar("Empresa Demo"));

        var fakeUsuarios = new FakeUsuarioRepository();
        await fakeUsuarios.AddAsync(usuario);

        var resultado = await new ConsultarUsuarioUseCase(fakeUsuarios).Handle(1);

        Assert.Equal("Fulano", resultado.Nome);
        Assert.Equal("Empresa Demo", resultado.GrupoEmpresaNome);
    }

    [Fact]
    public async Task Handle_ComUsuarioInexistente_DeveLancarNotFoundException()
    {
        var useCase = new ConsultarUsuarioUseCase(new FakeUsuarioRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.Handle(99));
    }
}

public class ListarUsuariosPorGrupoUseCaseTests
{
    [Fact]
    public async Task Handle_NaoAdmin_DeveListarSomenteOProprioGrupo()
    {
        var fakeUsuarios = new FakeUsuarioRepository();
        await fakeUsuarios.AddAsync(Usuario.Criar("Fulano", "fulano@demo.com", 1, PapelUsuario.Cliente));
        await fakeUsuarios.AddAsync(Usuario.Criar("Beltrano", "beltrano@demo.com", 1, PapelUsuario.Agente));
        await fakeUsuarios.AddAsync(Usuario.Criar("Ciclana", "ciclana@demo.com", 2, PapelUsuario.Supervisor));

        var resultado = await new ListarUsuariosPorGrupoUseCase(fakeUsuarios, new FakeCurrentUser()).Handle(2);

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, u => Assert.Equal(1, u.GrupoEmpresaId));
    }

    [Fact]
    public async Task Handle_Admin_DeveListarOGrupoInformado()
    {
        var fakeUsuarios = new FakeUsuarioRepository();
        await fakeUsuarios.AddAsync(Usuario.Criar("Fulano", "fulano@demo.com", 1, PapelUsuario.Cliente));
        await fakeUsuarios.AddAsync(Usuario.Criar("Ciclana", "ciclana@demo.com", 2, PapelUsuario.Supervisor));

        var admin = new FakeCurrentUser
        {
            Usuario = FakeCurrentUser.ContextoPadrao with { Papel = PapelUsuario.Admin, GrupoEmpresaIdInterno = 9 }
        };

        var resultado = await new ListarUsuariosPorGrupoUseCase(fakeUsuarios, admin).Handle(2);

        var usuario = Assert.Single(resultado);
        Assert.Equal(2, usuario.GrupoEmpresaId);
    }

    [Fact]
    public async Task Handle_SemUsuariosNoGrupo_DeveRetornarListaVazia()
    {
        var resultado = await new ListarUsuariosPorGrupoUseCase(new FakeUsuarioRepository(), new FakeCurrentUser()).Handle(1);

        Assert.Empty(resultado);
    }
}
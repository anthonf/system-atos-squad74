using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class CadastrarUsuarioUseCaseTests
{
    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarUsuario()
    {
        var fakeGrupos = new FakeGrupoEmpresaRepository();
        var fakeUsuarios = new FakeUsuarioRepository();
        var useCase = new CadastrarUsuarioUseCase(fakeGrupos, fakeUsuarios, new FakeUnitOfWork());

        var resultado = await useCase.Handle(new CadastrarUsuarioInput("Fulano", "fulano@demo.com", 1, PapelUsuario.Cliente));

        Assert.Equal("Fulano", resultado.Nome);
        Assert.Equal("fulano@demo.com", resultado.Email);
        Assert.Equal("Empresa Demo", resultado.GrupoEmpresaNome);
        Assert.Equal(PapelUsuario.Cliente, resultado.Papel);
        Assert.Single(fakeUsuarios.Usuarios);
    }

    [Fact]
    public async Task Handle_ComEmailDuplicado_DeveLancarConflictException()
    {
        var fakeGrupos = new FakeGrupoEmpresaRepository();
        var fakeUsuarios = new FakeUsuarioRepository();
        await fakeUsuarios.AddAsync(Usuario.Criar("Fulano", "dup@demo.com", 1, PapelUsuario.Cliente));
        var useCase = new CadastrarUsuarioUseCase(fakeGrupos, fakeUsuarios, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.Handle(new CadastrarUsuarioInput("Beltrano", "DUP@demo.com", 1, PapelUsuario.Agente)));

        Assert.Contains("e-mail", ex.Message);
    }

    [Fact]
    public async Task Handle_ComEmailInvalido_DeveLancarDomainException()
    {
        var fakeGrupos = new FakeGrupoEmpresaRepository();
        var fakeUsuarios = new FakeUsuarioRepository();
        var useCase = new CadastrarUsuarioUseCase(fakeGrupos, fakeUsuarios, new FakeUnitOfWork());

        await Assert.ThrowsAsync<SupportAtos.Domain.Exceptions.DomainException>(() =>
            useCase.Handle(new CadastrarUsuarioInput("Fulano", "email-invalido", 1, PapelUsuario.Cliente)));
    }

    [Fact]
    public async Task Handle_ComGrupoEmpresaInexistente_DeveLancarValidationException()
    {
        var fakeUsuarios = new FakeUsuarioRepository();
        var useCase = new CadastrarUsuarioUseCase(new FakeGrupoEmpresaRepository(), fakeUsuarios, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            useCase.Handle(new CadastrarUsuarioInput("Fulano", "fulano@demo.com", 999, PapelUsuario.Cliente)));

        Assert.Contains("não existe", ex.Message);
    }
}
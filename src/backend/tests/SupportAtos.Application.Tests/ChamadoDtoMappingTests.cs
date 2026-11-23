using SupportAtos.Application.DTOs;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Tests.Fakes;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ChamadoDtoMappingTests
{
    [Fact]
    public void ToDto_ComNavegacoesCarregadas_DeveExporNomesDeUsuarioEmpresaEagente()
    {
        var chamado = Chamado.Criar(
            "SA-1", 1, 1, SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Chamado de teste");
        var usuario = Usuario.Criar("Marina Alves", "marina@atos.com", 1, PapelUsuario.Cliente);
        usuario.SetIdForTest(1);
        var grupo = GrupoEmpresa.Criar("Atos Capital");
        grupo.SetIdForTest(1);
        var agente = Usuario.Criar("Diego Ramos", "diego@atos.com", 1, PapelUsuario.Agente);
        agente.SetIdForTest(2);
        chamado.SetForTest(nameof(Chamado.Usuario), usuario);
        chamado.SetForTest(nameof(Chamado.GrupoEmpresa), grupo);
        chamado.SetForTest(nameof(Chamado.Agente), agente);
        chamado.SetForTest(nameof(Chamado.AgenteId), 2);

        var dto = DtoMapper.ToDto(chamado);

        Assert.Equal("Marina Alves", dto.UsuarioNome);
        Assert.Equal("Atos Capital", dto.GrupoEmpresaNome);
        Assert.Equal("Diego Ramos", dto.AgenteNome);
        Assert.Equal(1, dto.UsuarioId);
        Assert.Equal(2, dto.AgenteId);
    }

    [Fact]
    public void WebhookEvent_DeChamadoDto_ContinuaFuncionandoComOsNovosCampos()
    {
        var dto = new ChamadoDto(
            1, "SA-1", 1, 1, null, "Portal", "Login", "Titulo", "Descricao",
            StatusChamado.Aberto, Prioridade.Alta, DateTime.UtcNow, null, null,
            DateTime.UtcNow, null, null, "Marina Alves", "Atos Capital", null);

        var evento = WebhookEvent.De(dto, WebhookEvent.EventoCriado);

        Assert.Equal("SA-1", evento.CodigoPublico);
        Assert.Equal("Titulo", evento.Titulo);
        Assert.Equal(WebhookEvent.EventoCriado, evento.Evento);
    }

    [Fact]
    public void ToDto_DeInteracao_ComAutorCarregado_DeveExporNomeEPapel()
    {
        var chamado = Chamado.Criar(
            "SA-1", 1, 1, SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
            "Portal", "Login", "Chamado de teste");
        var interacao = chamado.AdicionarInteracao(2, TipoInteracao.Publica, "Resposta", DateTime.UtcNow);
        var autor = Usuario.Criar("Diego Ramos", "diego@atos.com", 1, PapelUsuario.Supervisor);
        autor.SetIdForTest(2);
        interacao.SetForTest(nameof(Interacao.Usuario), autor);

        var dto = DtoMapper.ToDto(interacao);

        Assert.Equal("Diego Ramos", dto.UsuarioNome);
        Assert.Equal(PapelUsuario.Supervisor, dto.PapelUsuario);
        Assert.Equal(TipoInteracao.Publica, dto.Tipo);
    }
}

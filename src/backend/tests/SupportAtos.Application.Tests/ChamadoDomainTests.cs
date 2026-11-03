using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;
using SupportAtos.Domain.Exceptions;

namespace SupportAtos.Application.Tests;

public class ChamadoDomainTests
{
    private static SlaCategoria CriarSla() => SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4);

    [Fact]
    public void Criar_DeveGerarChamadoAbertoComPrazoDeSla()
    {
        var sla = CriarSla();

        var chamado = Chamado.Criar("SA-000000001", 1, 1, sla, "Portal", "Login");

        Assert.Equal("SA-000000001", chamado.CodigoPublico);
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Equal(Prioridade.Alta, chamado.Prioridade);
        Assert.Equal(sla.Id, chamado.SlaCategoriaId);
        Assert.Equal(chamado.CriadoEm.AddMinutes(sla.TempoResolucao), chamado.SlaPrazo);
        Assert.Equal(chamado.CriadoEm.AddMinutes(sla.TempoResposta), chamado.SlaPrazoResposta);
        Assert.Null(chamado.FechadoEm);
    }

    [Theory]
    [InlineData(StatusChamado.EmAndamento)]
    [InlineData(StatusChamado.AguardandoCliente)]
    [InlineData(StatusChamado.Resolvido)]
    [InlineData(StatusChamado.Fechado)]
    public void MudarStatus_DeAberto_ParaStatusValido_DeveAlterar(StatusChamado novoStatus)
    {
        var chamado = Chamado.Criar("SA-000000001", 1, 1, CriarSla(), "Portal", "Login");

        chamado.MudarStatus(novoStatus, DateTime.UtcNow);

        Assert.Equal(novoStatus, chamado.Status);
        if (novoStatus == StatusChamado.Fechado)
        {
            Assert.NotNull(chamado.FechadoEm);
        }
        else
        {
            Assert.Null(chamado.FechadoEm);
        }
    }

    [Fact]
    public void MudarStatus_DeTransicaoInvalida_DeveLancarDomainException()
    {
        var chamado = Chamado.Criar("SA-000000001", 1, 1, CriarSla(), "Portal", "Login");
        chamado.MudarStatus(StatusChamado.Resolvido, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => chamado.MudarStatus(StatusChamado.AguardandoCliente, DateTime.UtcNow));
    }

    [Fact]
    public void Fechado_NaoPodeIrParaAguardandoCliente()
    {
        var chamado = Chamado.Criar("SA-000000001", 1, 1, CriarSla(), "Portal", "Login");
        chamado.MudarStatus(StatusChamado.Fechado, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => chamado.MudarStatus(StatusChamado.AguardandoCliente, DateTime.UtcNow));
    }

    [Fact]
    public void AdicionarInteracao_DeveAdicionarInteracaoPublicaComMensagem()
    {
        var chamado = Chamado.Criar("SA-000000001", 1, 1, CriarSla(), "Portal", "Login");

        chamado.AdicionarInteracao(1, TipoInteracao.Publica, "Não consigo acessar o sistema.", DateTime.UtcNow);

        var interacao = Assert.Single(chamado.Interacoes);
        Assert.Equal(TipoInteracao.Publica, interacao.Tipo);
        Assert.Equal("Não consigo acessar o sistema.", interacao.Mensagem);
    }

    [Fact]
    public void ReabrirChamadoFechado_DeveLimparFechadoEm()
    {
        var chamado = Chamado.Criar("SA-000000001", 1, 1, CriarSla(), "Portal", "Login");
        chamado.MudarStatus(StatusChamado.Fechado, DateTime.UtcNow);
        Assert.NotNull(chamado.FechadoEm);

        chamado.MudarStatus(StatusChamado.Aberto, DateTime.UtcNow);

        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Null(chamado.FechadoEm);
    }
}
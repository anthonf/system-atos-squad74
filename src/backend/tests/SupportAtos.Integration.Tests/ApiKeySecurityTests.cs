using System.Net;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportAtos.Api.Authentication;
using SupportAtos.Api.Options;
using SupportAtos.Application.DTOs;
using SupportAtos.Infrastructure.Webhooks;

namespace SupportAtos.Integration.Tests;

public class ApiKeySecurityTests
{
    private sealed class Ambiente : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "SupportAtos.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Fact]
    public void Hash_DeveSerDeterministicoEHex64()
    {
        var hash = ApiKeyHasher.Hash(TestTokenFactory.PortalApiKey);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, ApiKeyHasher.Hash(TestTokenFactory.PortalApiKey));
        Assert.True(ApiKeyHasher.HashValido(hash));
    }

    [Fact]
    public void Corresponde_DeveAceitarSomenteAChaveCorreta()
    {
        var hash = ApiKeyHasher.Hash(TestTokenFactory.PortalApiKey);
        Assert.True(ApiKeyHasher.Corresponde(TestTokenFactory.PortalApiKey, hash));
        Assert.False(ApiKeyHasher.Corresponde(TestTokenFactory.AutomacaoApiKey, hash));
    }

    [Fact]
    public void Validar_SemChaveEmProducao_DeveLancar()
    {
        var settings = new ApiKeySettings();
        var ambiente = new Ambiente { EnvironmentName = "Production" };
        Assert.Throws<InvalidOperationException>(() => ApiKeyConfigurationValidator.Validar(settings, ambiente));
    }

    [Fact]
    public void Validar_ComChaveEmProducao_DevePassar()
    {
        var settings = new ApiKeySettings
        {
            Portal = new ApiKeyClientSettings { PrimaryHash = ApiKeyHasher.Hash(TestTokenFactory.PortalApiKey) },
            Automacao = new ApiKeyClientSettings { PrimaryHash = ApiKeyHasher.Hash(TestTokenFactory.AutomacaoApiKey) }
        };
        var ambiente = new Ambiente { EnvironmentName = "Production" };
        ApiKeyConfigurationValidator.Validar(settings, ambiente);
    }

    [Fact]
    public void Validar_HashInvalido_DeveLancar()
    {
        var settings = new ApiKeySettings
        {
            Portal = new ApiKeyClientSettings { PrimaryHash = "nao-e-hex" }
        };
        var ambiente = new Ambiente { EnvironmentName = "Production" };
        Assert.Throws<InvalidOperationException>(() => ApiKeyConfigurationValidator.Validar(settings, ambiente));
    }

    [Fact]
    public void Validar_ChaveRepetidaEntreEscopos_DeveLancar()
    {
        var hash = ApiKeyHasher.Hash(TestTokenFactory.PortalApiKey);
        var settings = new ApiKeySettings
        {
            Portal = new ApiKeyClientSettings { PrimaryHash = hash },
            Automacao = new ApiKeyClientSettings { PrimaryHash = hash }
        };
        var ambiente = new Ambiente { EnvironmentName = "Production" };
        Assert.Throws<InvalidOperationException>(() => ApiKeyConfigurationValidator.Validar(settings, ambiente));
    }

    [Fact]
    public void Generator_DeveGerarChaveLongaEHashCorrespondente()
    {
        var saida = new StringWriter();

        var codigo = ApiKeyGenerator.Executar(new[] { "--generate-api-key", "--escopo", "portal" }, saida);
        var texto = saida.ToString();

        Assert.Equal(0, codigo);
        var linhaChave = texto.Split('\n').Single(linha => linha.StartsWith("Chave (exibida apenas uma vez):"));
        var chave = linhaChave.Split(':', 2)[1].Trim();
        Assert.True(chave.Length >= ApiKeyHasher.TamanhoMinimoChave);

        var linhaHash = texto.Split('\n').Single(linha => linha.StartsWith("Hash SHA-256:"));
        var hash = linhaHash.Split(':', 2)[1].Trim();
        Assert.Equal(ApiKeyHasher.Hash(chave), hash);
        Assert.True(ApiKeyHasher.HashValido(hash));
    }

    [Fact]
    public async Task Webhook_DeveEnviarApiKeyNoHeaderSemRegistrarSegredo()
    {
        var handler = new CapturaHandler();
        var factory = new FakeHttpClientFactory(handler);
        var settings = new WebhookSettings
        {
            PortalUrl = "https://example.test/webhook",
            ApiKey = TestTokenFactory.WebhookApiKey,
            TimeoutSeconds = 5,
            MaxAttempts = 1
        };
        var logger = new CapturaLogger<HttpWebhookDispatcher>();
        var dispatcher = new HttpWebhookDispatcher(
            factory,
            Options.Create(settings),
            logger);

        await dispatcher.EnviarAsync(new WebhookEvent(
            WebhookEvent.EventoCriado,
            "SA-000000001",
            "Título",
            "Portal",
            "Login",
            "Aberto",
            1,
            1,
            DateTime.UtcNow));

        Assert.NotNull(handler.Requisicao);
        Assert.Equal(HttpMethod.Post, handler.Requisicao!.Method);
        Assert.Equal(TestTokenFactory.WebhookApiKey, handler.Requisicao.Headers.GetValues("X-Api-Key").Single());
        Assert.Contains("chamado.criado", handler.Corpo);
        Assert.DoesNotContain(TestTokenFactory.WebhookApiKey, logger.Texto);
    }

    private sealed class CapturaHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Requisicao { get; private set; }
        public string Corpo { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicao = request;
            Corpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class CapturaLogger<T> : ILogger<T>
    {
        private readonly StringBuilder _texto = new();

        public string Texto => _texto.ToString();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _texto.AppendLine(formatter(state, exception));
            if (exception is not null)
            {
                _texto.AppendLine(exception.ToString());
            }
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Integration.Tests;

/// <summary>
/// Fábrica da aplicação sob teste apontando para o banco Postgres supportatos_test.
/// O banco é criado sob demanda no primeiro uso do host e as migrations aplicadas.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    /// <summary>Override via variável de ambiente SUPPORTATOS_TEST_CONNECTION, se desejado.
    /// O fallback aponta para o Postgres do docker-compose.yml raiz (porta 5433, usuário admin,
    /// senha padrão do container). Nunca colocar credenciais reais aqui — esse arquivo é versionado.</summary>
    public static readonly string ConexaoBanco =
        Environment.GetEnvironmentVariable("SUPPORTATOS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=supportatos_test;Username=admin;Password=supportatos";

    private static readonly SemaphoreSlim MutexBanco = new(1, 1);
    private static bool _bancoPronto;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConexaoBanco);
        builder.UseSetting("Sso:Issuer", TestTokenFactory.Issuer);
        builder.UseSetting("Sso:Audience", TestTokenFactory.Audience);
        builder.UseSetting("Sso:PublicKeyPem", TestTokenFactory.ChavePublicaPem);
        builder.UseSetting("Sso:ApiKey", TestTokenFactory.ApiKey);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var escopo = host.Services.CreateScope();
        var dbContext = escopo.ServiceProvider.GetRequiredService<SupportAtosDbContext>();
        GarantirBancoAsync(dbContext).GetAwaiter().GetResult();
        dbContext.Database.Migrate();

        if (!dbContext.SlaCategorias.Any())
        {
            dbContext.SlaCategorias.AddRange(
                SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4),
                SlaCategoria.Criar("Portal", "Erro de sistema", Prioridade.Media, 2, 8),
                SlaCategoria.Criar("Portal", "Dúvida", Prioridade.Baixa, 3, 24));
        }

        dbContext.SaveChanges();

        return host;
    }

    private static async Task GarantirBancoAsync(SupportAtosDbContext dbContext)
    {
        if (_bancoPronto)
        {
            return;
        }

        await MutexBanco.WaitAsync();
        try
        {
            if (_bancoPronto)
            {
                return;
            }

            var conexaoBanco = dbContext.Database.GetDbConnection().ConnectionString
                                ?? throw new InvalidOperationException("Connection string não configurada para os testes de integração.");
            var nomeBanco = new NpgsqlConnectionStringBuilder(conexaoBanco).Database
                            ?? throw new InvalidOperationException("Nome do banco não informado na connection string.");
            var conexaoServidor = new NpgsqlConnectionStringBuilder(conexaoBanco) { Database = "postgres" }.ConnectionString;

            await using var conexao = new NpgsqlConnection(conexaoServidor);
            await conexao.OpenAsync();

            await using var verificar = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @nome", conexao);
            verificar.Parameters.AddWithValue("nome", (object)nomeBanco);
            var existe = await verificar.ExecuteScalarAsync();

            if (existe is null)
            {
                await using var criar = new NpgsqlCommand(
                    $"CREATE DATABASE \"{nomeBanco}\"", conexao);
                await criar.ExecuteNonQueryAsync();
            }

            _bancoPronto = true;
        }
        finally
        {
            MutexBanco.Release();
        }
    }
}
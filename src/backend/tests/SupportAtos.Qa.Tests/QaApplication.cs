using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SupportAtos.Api.Options;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Qa.Tests;

public record PortalIdentity(int UserId, int GroupId, string Name, string Email, string GroupName)
{
    public static PortalIdentity A => new(61001, 71001, "QA A", "a@qa.example.invalid", "QA Grupo A");
    public static PortalIdentity B => new(61002, 71002, "QA B", "b@qa.example.invalid", "QA Grupo B");
    public static PortalIdentity New => new(61090, 71090, "QA Novo", "novo@qa.example.invalid", "QA Grupo Novo");
}

public sealed class QaApplication(string connection) : WebApplicationFactory<Program>
{
    public string ApiKey { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public QaDatabaseErrors Errors { get; } = new();
    private readonly RSA _rsa = RSA.Create(2048);
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
        builder.UseSetting("ApiKeys:Portal:PrimaryHash", ApiKeyHasher.Hash(ApiKey));
        builder.UseSetting("Sso:Issuer", "atos-qa");
        builder.UseSetting("Sso:Audience", "atos-api-qa");
        builder.UseSetting("Sso:PublicKeyPem", _rsa.ExportSubjectPublicKeyInfoPem());
        // Evita EventLog do Windows e logs com parâmetros. Não substitui autenticação, DI ou persistência.
        builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddProvider(Errors); });
    }

    public string JwtOnly()
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken("atos-qa", "atos-api-qa", [
            new Claim("sub", "61001"), new Claim("email", PortalIdentity.A.Email),
            new Claim("name", PortalIdentity.A.Name), new Claim("grupo_empresa_id", "71001"),
            new Claim("grupo_empresa_nome", PortalIdentity.A.GroupName), new Claim("role", "Cliente")
        ], now.AddSeconds(-5), now.AddMinutes(10),
            new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _rsa.Dispose();
    }
}

public sealed class QaCase(QaPostgres postgres, Evidence evidence)
{
    private QaApplication? _factory;
    private HttpClient? _client;
    private string? _database;
    private string Connection => postgres.Connection(_database!);

    public async Task Start(bool existingUser = false, bool existingGroup = false)
    {
        _database = await postgres.CreateDatabase();
        evidence.Database = _database;
        // Migrations são executadas num contexto independente, antes de iniciar o host da API.
        await using (var db = new SupportAtosDbContext(new DbContextOptionsBuilder<SupportAtosDbContext>()
            .UseNpgsql(Connection).Options))
        {
            await db.Database.MigrateAsync();
            if (!(await db.Database.GetAppliedMigrationsAsync()).Any()) throw new InvalidOperationException("Migrations não aplicadas.");
            db.SlaCategorias.Add(SlaCategoria.Criar("QA", "Valid", Prioridade.Media, 60, 480));
            await db.SaveChangesAsync();
        }
        if (evidence.Scenario.Compatibility) await Seed(new(4, 3, "QA Compat", "compat@qa.example.invalid", "QA Compat Grupo"), true);
        if (existingUser || existingGroup) await Seed(PortalIdentity.A, existingUser);
        if (evidence.Scenario.Id == "RF01-13") await Seed(PortalIdentity.B, true);

        _factory = new QaApplication(Connection);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var scope = _factory.Services.CreateScope();
        var actual = scope.ServiceProvider.GetRequiredService<SupportAtosDbContext>().Database.GetConnectionString();
        if (new NpgsqlConnectionStringBuilder(actual).ConnectionString != new NpgsqlConnectionStringBuilder(Connection).ConnectionString)
            throw new InvalidOperationException("Host da API não usa a conexão exclusiva esperada; execução bloqueada.");
    }

    private async Task Seed(PortalIdentity id, bool user)
    {
        await using var c = new NpgsqlConnection(Connection);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO grupo_empresa(id,nome,codigo_externo,tipo,criado_em)
            VALUES (@gid,@gname,@gcode,'matriz',now());
            """ + (user ? """
            INSERT INTO usuario(id,nome,email,grupo_empresa_id,papel,codigo_externo,criado_em)
            VALUES (@uid,@name,@email,@gid,'Cliente',@ucode,now());
            """ : ""), c);
        cmd.Parameters.AddWithValue("gid", id.GroupId);
        cmd.Parameters.AddWithValue("gname", id.GroupName);
        // Para fixtures de existentes, o código externo coincide com o ID textual do portal.
        cmd.Parameters.AddWithValue("gcode", id.GroupId.ToString());
        if (user)
        {
            cmd.Parameters.AddWithValue("uid", id.UserId); cmd.Parameters.AddWithValue("name", id.Name);
            cmd.Parameters.AddWithValue("email", id.Email); cmd.Parameters.AddWithValue("ucode", id.UserId.ToString());
        }
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ApiObservation> Post(PortalIdentity identity, string credential = "valid", string? omit = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["produto"] = "QA", ["categoria"] = "Valid", ["titulo"] = evidence.Scenario.Id,
            ["descricao"] = "Descrição de QA", ["mensagemInicial"] = "Descrição de QA",
            ["usuario"] = new { id = identity.UserId, nome = identity.Name, email = identity.Email },
            ["grupoEmpresa"] = new { id = identity.GroupId, nome = identity.GroupName }
        };
        if (omit is not null) payload.Remove(omit);
        var before = await Snapshot();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chamados") { Content = JsonContent.Create(payload) };
        if (credential is "valid" or "invalid" or "empty" or "invalid+jwt")
            request.Headers.TryAddWithoutValidation("X-Api-Key", credential == "valid" ? _factory!.ApiKey : credential == "empty" ? "" : "qa-invalid");
        if (credential is "jwt" or "invalid+jwt") request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _factory!.JwtOnly());
        using var response = await _client!.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        var after = await Snapshot();
        var errors = _factory!.Errors.Drain();
        var observation = new ApiObservation((int)response.StatusCode, before, after, string.Join("; ", errors));
        // Nunca grava headers/valores de API Key, JWT, senha ou connection string.
        evidence.Requests.Add(new { Method = "POST", Path = "/api/chamados", CredentialMode = credential,
            Payload = payload, Status = observation.Status, Response = raw.Replace(_factory!.ApiKey, "[SEGREDO]"),
            DatabaseErrors = errors, Before = before, After = after });
        return observation;
    }

    private async Task<Dictionary<string, JsonElement>> Snapshot()
    {
        await using var c = new NpgsqlConnection(Connection);
        await c.OpenAsync();
        var result = new Dictionary<string, JsonElement>();
        foreach (var table in new[] { "chamado", "interacao", "usuario", "grupo_empresa", "provisionamento_audit" })
        {
            await using var cmd = new NpgsqlCommand($"SELECT COALESCE(jsonb_agg(to_jsonb(t) ORDER BY t.id),'[]'::jsonb)::text FROM {table} t", c);
            using var doc = JsonDocument.Parse((string)(await cmd.ExecuteScalarAsync())!);
            result[table] = doc.RootElement.Clone();
        }
        return result;
    }

    public async Task Stop()
    {
        try { _client?.Dispose(); if (_factory is not null) await _factory.DisposeAsync(); }
        finally { if (_database is not null) await postgres.DropDatabase(_database); }
    }
}

// Captura somente metadados de erros SQL; nunca mensagem formatada, parâmetros, senha ou stack com dados.
public sealed class QaDatabaseErrors : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _errors = new();
    public string[] Drain()
    {
        var result = new List<string>();
        while (_errors.TryDequeue(out var error)) result.Add(error);
        return result.Distinct().ToArray();
    }
    public ILogger CreateLogger(string categoryName) => new SqlLogger(_errors);
    public void Dispose() { }
    private sealed class SqlLogger(ConcurrentQueue<string> errors) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            for (var ex = exception; ex is not null; ex = ex.InnerException)
                if (ex is PostgresException pg)
                    errors.Enqueue($"SQLSTATE={pg.SqlState}; tabela={pg.TableName}; constraint={pg.ConstraintName}");
        }
    }
}

public record ApiObservation(int Status, Dictionary<string, JsonElement> Before, Dictionary<string, JsonElement> After, string DatabaseError)
{
    public IEnumerable<JsonElement> NewTickets => After["chamado"].EnumerateArray()
        .Where(row => !Before["chamado"].EnumerateArray().Any(old => old.GetProperty("id").GetInt32() == row.GetProperty("id").GetInt32()));
    public int Count(string table) => After[table].GetArrayLength();
}

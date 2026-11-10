using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace SupportAtos.Qa.Tests;

[CollectionDefinition("QA isolado", DisableParallelization = true)]
public sealed class QaCollection : ICollectionFixture<QaPostgres> { }

// Não aceita URL, connection string ou container externo: toda conexão deriva do container criado aqui.
public sealed class QaPostgres : IAsyncLifetime
{
    private readonly string _owner = Guid.NewGuid().ToString("N");
    private readonly string _password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private string Name => "atos-qa-" + _owner;
    private string Role => "qa_" + _owner;
    private string? _containerId;
    private int _port;
    private readonly HashSet<string> _databases = [];
    public QaReport Report { get; } = new();
    public string? InitializationError { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Recusar overrides de banco é intencional: não há fallback para bancos existentes.
            foreach (var key in new[] { "SUPPORTATOS_TEST_CONNECTION", "QA_CONNECTION_STRING", "ConnectionStrings__DefaultConnection" })
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    throw new InvalidOperationException($"Execução bloqueada: remova {key}; a suíte só usa banco que ela mesma cria.");

            await Docker(["info", "--format", "{{.ServerVersion}}"]);
            // Senha transmitida ao Docker via ambiente, não por argumento ou arquivo versionado.
            var id = await Docker(["run", "--detach", "--name", Name, "--label", "atos.qa.owner=" + _owner,
                "--label", "atos.qa.purpose=rf01-rf10", "--publish", "127.0.0.1::5432",
                "--tmpfs", "/var/lib/postgresql/data", "--env", "POSTGRES_USER=" + Role,
                "--env", "POSTGRES_DB=" + Role, "--env", "POSTGRES_PASSWORD", "postgres:17"], true);
            _containerId = id.Trim();
            using var inspected = JsonDocument.Parse(await Docker(["inspect", _containerId]));
            var root = inspected.RootElement[0];
            VerifyOwnership(root);
            var binding = root.GetProperty("NetworkSettings").GetProperty("Ports").GetProperty("5432/tcp")[0];
            if (binding.GetProperty("HostIp").GetString() != "127.0.0.1")
                throw new InvalidOperationException("Publicação Docker não está restrita a loopback.");
            _port = int.Parse(binding.GetProperty("HostPort").GetString()!);
            if (_port <= 0) throw new InvalidOperationException("Porta isolada inválida.");
            var ready = false;
            for (var i = 0; i < 60; i++)
            {
                try
                {
                    await using var connection = new NpgsqlConnection(Connection(Role));
                    await connection.OpenAsync();
                    await AssertIdentity(connection, Role);
                    ready = true;
                    break;
                }
                catch (NpgsqlException) { await Task.Delay(500); }
            }
            if (!ready) throw new InvalidOperationException("PostgreSQL exclusivo não ficou pronto em 30 segundos.");
            Report.Lifecycle = $"Container exclusivo {Name}; loopback; tmpfs; banco novo por cenário; migrations reais. Em execução.";
        }
        catch (Exception e) { InitializationError = Sanitize(e); Report.Lifecycle = "INFRAESTRUTURA: " + InitializationError; }
        Report.Save();
    }

    public string Connection(string database)
    {
        if (database != Role && !_databases.Contains(database)) throw new InvalidOperationException("Banco não pertence à execução.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1", Port = _port, Database = database, Username = Role,
            Password = _password, Pooling = false, Timeout = 3, CommandTimeout = 30,
            IncludeErrorDetail = false
        }.ConnectionString;
    }

    public async Task<string> CreateDatabase()
    {
        if (InitializationError is not null) throw new InvalidOperationException(InitializationError);
        await VerifyContainer();
        var database = "qa_" + Guid.NewGuid().ToString("N");
        await using var c = new NpgsqlConnection(Connection(Role));
        await c.OpenAsync();
        await AssertIdentity(c, Role);
        await using var cmd = new NpgsqlCommand($"CREATE DATABASE {database}", c);
        await cmd.ExecuteNonQueryAsync(); // Nome gerado internamente com alfabeto restrito; sem input externo.
        _databases.Add(database);
        await using var fresh = new NpgsqlConnection(Connection(database));
        await fresh.OpenAsync();
        await AssertIdentity(fresh, database);
        await using var tables = new NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema='public'", fresh);
        if (Convert.ToInt64(await tables.ExecuteScalarAsync()) != 0) throw new InvalidOperationException("Banco recém-criado não está vazio.");
        return database;
    }

    public async Task DropDatabase(string database)
    {
        if (!_databases.Contains(database) || !Regex.IsMatch(database, "^qa_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Descarte bloqueado: banco não pertence à execução.");
        await VerifyContainer();
        await using var c = new NpgsqlConnection(Connection(Role));
        await c.OpenAsync();
        await AssertIdentity(c, Role);
        await using var cmd = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", c);
        await cmd.ExecuteNonQueryAsync();
        _databases.Remove(database);
    }

    private async Task AssertIdentity(NpgsqlConnection c, string database)
    {
        await using var cmd = new NpgsqlCommand("SELECT current_database(), current_user", c);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.GetString(0) != database || reader.GetString(1) != Role)
            throw new InvalidOperationException("Identidade PostgreSQL não corresponde ao ambiente exclusivo.");
    }

    private void VerifyOwnership(JsonElement root)
    {
        if (root.GetProperty("Id").GetString() != _containerId || root.GetProperty("Name").GetString() != "/" + Name
            || root.GetProperty("Config").GetProperty("Labels").GetProperty("atos.qa.owner").GetString() != _owner
            || root.GetProperty("Config").GetProperty("Labels").GetProperty("atos.qa.purpose").GetString() != "rf01-rf10"
            || !root.GetProperty("HostConfig").GetProperty("Tmpfs").TryGetProperty("/var/lib/postgresql/data", out _))
            throw new InvalidOperationException("Container não satisfaz as proteções de propriedade e tmpfs.");
    }

    private async Task VerifyContainer()
    {
        if (_containerId is null || !Regex.IsMatch(_containerId, "^[a-f0-9]{64}$"))
            throw new InvalidOperationException("Container exclusivo não identificado.");
        using var doc = JsonDocument.Parse(await Docker(["inspect", _containerId]));
        VerifyOwnership(doc.RootElement[0]);
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_containerId is not null)
            {
                await VerifyContainer();
                await Docker(["rm", "--force", _containerId]);
                Report.Lifecycle += $" Encerramento: container {_containerId} removido; dados descartados.";
            }
            else Report.Lifecycle += " Encerramento: nenhum container chegou a ser criado pela suíte.";
        }
        catch (Exception e)
        {
            Report.Lifecycle += " FALHA DE LIMPEZA: " + Sanitize(e) + $". Verificar container {Name}.";
            throw new InvalidOperationException("Falha ao descartar container exclusivo de QA; consulte RESUMO.md.");
        }
        finally { Report.Save(); }
    }

    public string Sanitize(Exception e) => e.GetType().Name + ": " + e.Message.Replace(_password, "[SEGREDO]");

    private async Task<string> Docker(string[] args, bool password = false)
    {
        var info = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var arg in args) info.ArgumentList.Add(arg);
        if (password) info.Environment["POSTGRES_PASSWORD"] = _password;
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Docker CLI não iniciou.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Docker excedeu 90 segundos."); }
        var stdout = await output;
        var stderr = await error;
        if (process.ExitCode != 0) throw new InvalidOperationException("Docker: " + stderr.Replace(_password, "[SEGREDO]"));
        return stdout;
    }
}

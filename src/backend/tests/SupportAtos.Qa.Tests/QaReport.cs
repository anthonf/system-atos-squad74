using System.Text;
using System.Text.Json;

namespace SupportAtos.Qa.Tests;

public record Scenario(string Id, string Group, string Requirement, string Expected, bool Compatibility = false);
public record Verification(string Condition, string Expected, string Observed, string Result, string Category);

public sealed class Evidence(Scenario scenario)
{
    public Scenario Scenario { get; } = scenario;
    public string Database { get; set; } = "não criado";
    public List<object> Requests { get; } = [];
    public List<Verification> Checks { get; } = [];
    public void Check(string condition, object expected, object observed, bool passed) => Checks.Add(new(
        condition, expected.ToString()!, observed.ToString()!, passed ? "PASSOU" : "FALHOU", "REQUISITO"));
    public void Inconclusive(string condition, string reason) => Checks.Add(new(
        condition, "verificar pós-condição", reason, "NÃO EXECUTADO", "INCONCLUSIVO"));
    public void Infrastructure(string reason) => Checks.Add(new(
        "infraestrutura/isolamento/limpeza", "ambiente isolado disponível e descartado", reason,
        "NÃO EXECUTADO", "INFRAESTRUTURA"));
    public string Result => Checks.Any(c => c.Result == "FALHOU") ? "FALHOU"
        : Checks.Any(c => c.Result == "NÃO EXECUTADO") ? "NÃO EXECUTADO" : "PASSOU";
}

public sealed class QaReport
{
    public string DirectoryPath { get; } = Environment.GetEnvironmentVariable("QA_REPORT_DIRECTORY")
        ?? Path.Combine(AppContext.BaseDirectory, "qa-results", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, Evidence> _results = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public string Lifecycle { get; set; } = "Inicializando Docker exclusivo de QA.";

    public QaReport() => Save();
    public void Record(Evidence evidence)
    {
        _results[evidence.Scenario.Id] = evidence;
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, evidence.Scenario.Id + ".json"),
            JsonSerializer.Serialize(evidence, JsonOptions));
        Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        static string Cell(string text) => text.Replace("|", "\\|").Replace("\r", "").Replace("\n", "<br>");
        var b = new StringBuilder("# QA RF01 RF10 e API Key\n\n");
        b.AppendLine($"Execução UTC: {DateTimeOffset.UtcNow:O}\n\n{Lifecycle}\n");
        b.AppendLine("Fonte: Termo v3 §§2.1, 2.5, 4, 6.2 e 8. Formato JSON proposto pela suíte, não fixado pelo Termo.\n");
        b.AppendLine("| Cenário | Requisito | Esperado | Obtido | Resultado | Diagnóstico |\n|---|---|---|---|---|---|");
        foreach (var s in QaScenarios.All)
        {
            _results.TryGetValue(s.Id, out var e);
            var obtained = e is null ? "Não selecionado ou não alcançado pelo runner."
                : string.Join("; ", e.Checks.Select(c => $"{c.Condition}: {c.Observed}"));
            var categories = e is null ? "Sem evidência" : string.Join(", ", e.Checks
                .Where(c => c.Result != "PASSOU").Select(c => c.Category).Distinct());
            b.AppendLine($"| {s.Id}{(s.Compatibility ? " (compatibilidade)" : "")} | {s.Requirement} | {Cell(s.Expected)} | {Cell(obtained)} | {e?.Result ?? "NÃO EXECUTADO"} | {categories} |");
        }
        b.AppendLine("\nCada JSON contém verificações independentes, HTTP sem credenciais e estado SQL antes/depois. Um erro HTTP anterior pode reprovar a abertura e deixar verificações subsequentes inconclusivas; não comprova todas as falhas. Falha de infraestrutura faz o xUnit/TRX falhar, mas o requisito fica NÃO EXECUTADO neste resumo.\n");
        foreach (var group in _results.Values.GroupBy(e => e.Result)) b.AppendLine($"- {group.Key}: {group.Count()}");
        File.WriteAllText(Path.Combine(DirectoryPath, "RESUMO.md"), b.ToString());
    }
}

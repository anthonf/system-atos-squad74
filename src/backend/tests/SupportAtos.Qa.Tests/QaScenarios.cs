using System.Text.Json;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SupportAtos.Qa.Tests;

[Collection("QA isolado")]
public sealed class QaScenarios(QaPostgres postgres, ITestOutputHelper output)
{
    public static IReadOnlyList<Scenario> All { get; } = [
        new("AUTH-01", "AUTH", "§§2.1/6.2; RF10", "Chave válida permite criar (201), sem JWT.", true),
        new("AUTH-02", "AUTH", "§6.2; RF10", "Sem chave: 401, zero gravações.", true),
        new("AUTH-03", "AUTH", "§6.2; RF10", "Chave inválida: 401, zero gravações.", true),
        new("AUTH-04", "AUTH", "§6.2; RF10", "Chave vazia: 401, zero gravações.", true),
        new("AUTH-05", "AUTH", "§6.2; RF10", "Somente JWT: 401, zero gravações.", true),
        new("AUTH-06", "AUTH", "§6.2; RF10", "Chave inválida com JWT: 401, zero gravações.", true),
        new("RF01-01", "RF01", "§§2.1/6.2; RF01", "Chamado/interação vinculados a 61001/71001.", true),
        new("RF01-13", "RF01", "§§2.1/6.2; RF01", "Alternância A/B preserva cada identidade e não usa 4/3.", true),
        new("RF01-09", "RF01", "§§2.1/6.2; RF01", "Sem usuário: rejeitar, nenhum chamado/interação.", true),
        new("RF01-10", "RF01", "§§2.1/6.2; RF01", "Sem grupo: rejeitar, nenhum chamado/interação.", true),
        new("RF10-01", "RF10", "§§2.5/6.2/8; RF10", "Usuário/grupo novos: criar ambos com IDs 61090/71090 e criar chamado."),
        new("RF10-03", "RF10", "§§2.5/6.2; RF10", "Usuário novo 61090 em grupo existente 71001: criar usuário e chamado."),
        new("RF10-04", "RF10", "§§2.5/6.2; RF10", "Atualizar nome do usuário existente, preservar ID e vínculo.", true),
        new("RF10-05", "RF10", "§§2.5/6.2; RF10", "Atualizar e-mail do usuário existente, preservar ID e vínculo.", true),
        new("RF10-06", "RF10", "§§2.5/6.2; RF10", "Atualizar nome do grupo existente, preservar ID e vínculo.", true),
        new("RF10-07", "RF10", "§§2.5/6.2; RF10", "Repetir identidade existente sem duplicar usuário/grupo; dois chamados corretos.", true),
        new("RF10-10", "RF10", "§§2.5/6.2; RF10", "Novos IDs 61090/71090 são IDs reais, não somente codigo_externo."),
        new("RF10-11-COMPAT", "RF10", "§§2.5/6.2; RF10", "Diagnóstico separado com 4/3: ainda deve criar identidade nova e usá-la.", true)
    ];

    [Fact(DisplayName = "AUTH-01 API Key válida sem JWT"), Trait("Group", "AUTH")]
    public Task Auth01() => Run("AUTH-01", async (c, e) => { Created(e, await c.Post(PortalIdentity.A)); }, existingUser: true);
    [Fact(DisplayName = "AUTH-02 API Key ausente"), Trait("Group", "AUTH")]
    public Task Auth02() => NegativeAuth("AUTH-02", "absent");
    [Fact(DisplayName = "AUTH-03 API Key inválida"), Trait("Group", "AUTH")]
    public Task Auth03() => NegativeAuth("AUTH-03", "invalid");
    [Fact(DisplayName = "AUTH-04 API Key vazia"), Trait("Group", "AUTH")]
    public Task Auth04() => NegativeAuth("AUTH-04", "empty");
    [Fact(DisplayName = "AUTH-05 somente JWT"), Trait("Group", "AUTH")]
    public Task Auth05() => NegativeAuth("AUTH-05", "jwt");
    [Fact(DisplayName = "AUTH-06 API Key inválida e JWT"), Trait("Group", "AUTH")]
    public Task Auth06() => NegativeAuth("AUTH-06", "invalid+jwt");

    private Task NegativeAuth(string id, string mode) => Run(id, async (c, e) =>
    {
        var r = await c.Post(PortalIdentity.A, mode);
        e.Check("HTTP autenticação", "401", r.Status, r.Status == 401);
        NoWrites(e, r);
    }, existingUser: true);

    [Fact(DisplayName = "RF01-01 associação A"), Trait("Group", "RF01")]
    public Task Rf0101() => Run("RF01-01", async (c, e) =>
    {
        var r = await c.Post(PortalIdentity.A);
        if (Created(e, r)) Association(e, r, PortalIdentity.A);
    }, existingUser: true);

    [Fact(DisplayName = "RF01-13 alternar clientes A e B"), Trait("Group", "RF01")]
    public Task Rf0113() => Run("RF01-13", async (c, e) =>
    {
        var a = await c.Post(PortalIdentity.A);
        var b = await c.Post(PortalIdentity.B);
        if (Created(e, a)) Association(e, a, PortalIdentity.A);
        if (Created(e, b)) Association(e, b, PortalIdentity.B);
    }, existingUser: true);

    [Fact(DisplayName = "RF01-09 ausência de usuário"), Trait("Group", "RF01")]
    public Task Rf0109() => MissingIdentity("RF01-09", "usuario");
    [Fact(DisplayName = "RF01-10 ausência de grupo"), Trait("Group", "RF01")]
    public Task Rf0110() => MissingIdentity("RF01-10", "grupoEmpresa");

    private Task MissingIdentity(string id, string omit) => Run(id, async (c, e) =>
    {
        var r = await c.Post(PortalIdentity.A, omit: omit);
        // Não exige 400 especificamente. 500 não é uma rejeição controlada de identidade.
        e.Check("Identidade ausente rejeitada", "4xx (sem status específico normativo)", r.Status, r.Status is >= 400 and < 500);
        NoWrites(e, r);
    }, existingUser: true);

    [Fact(DisplayName = "RF10-01 usuário e grupo novos sem compatibilidade"), Trait("Group", "RF10")]
    public Task Rf1001() => NewIdentity("RF10-01");
    [Fact(DisplayName = "RF10-10 preservar IDs reais sem compatibilidade"), Trait("Group", "RF10")]
    public Task Rf1010() => NewIdentity("RF10-10");
    [Fact(DisplayName = "RF10-11-COMPAT diagnóstico de upsert com fixture separada"), Trait("Group", "RF10")]
    public Task Rf1011Compat() => NewIdentity("RF10-11-COMPAT");

    private Task NewIdentity(string id) => Run(id, async (c, e) =>
    {
        var r = await c.Post(PortalIdentity.New);
        if (!Created(e, r)) return;
        Identity(e, r, PortalIdentity.New);
        Association(e, r, PortalIdentity.New);
    });

    [Fact(DisplayName = "RF10-03 usuário novo grupo existente sem compatibilidade"), Trait("Group", "RF10")]
    public Task Rf1003() => Run("RF10-03", async (c, e) =>
    {
        var id = PortalIdentity.New with { GroupId = PortalIdentity.A.GroupId, GroupName = PortalIdentity.A.GroupName };
        var r = await c.Post(id);
        if (!Created(e, r)) return;
        Identity(e, r, id);
        Association(e, r, id);
        e.Check("Não duplicar grupo existente", r.Before["grupo_empresa"].GetArrayLength(), r.Count("grupo_empresa"),
            r.Before["grupo_empresa"].GetArrayLength() == r.Count("grupo_empresa"));
    }, existingGroup: true);

    [Fact(DisplayName = "RF10-04 atualizar nome do usuário"), Trait("Group", "RF10")]
    public Task Rf1004() => Update("RF10-04", PortalIdentity.A with { Name = "QA A Atualizado" });
    [Fact(DisplayName = "RF10-05 atualizar e-mail do usuário"), Trait("Group", "RF10")]
    public Task Rf1005() => Update("RF10-05", PortalIdentity.A with { Email = "a-atualizado@qa.example.invalid" });
    [Fact(DisplayName = "RF10-06 atualizar nome do grupo"), Trait("Group", "RF10")]
    public Task Rf1006() => Update("RF10-06", PortalIdentity.A with { GroupName = "QA Grupo A Atualizado" });

    private Task Update(string code, PortalIdentity id) => Run(code, async (c, e) =>
    {
        var r = await c.Post(id);
        if (!Created(e, r)) return;
        Identity(e, r, id);
        Association(e, r, id);
        foreach (var table in new[] { "usuario", "grupo_empresa" })
            e.Check("Sem duplicação em " + table, r.Before[table].GetArrayLength(), r.Count(table),
                r.Before[table].GetArrayLength() == r.Count(table));
    }, existingUser: true);

    [Fact(DisplayName = "RF10-07 repetir identidade existente sem duplicar"), Trait("Group", "RF10")]
    public Task Rf1007() => Run("RF10-07", async (c, e) =>
    {
        var first = await c.Post(PortalIdentity.A);
        var second = await c.Post(PortalIdentity.A);
        if (Created(e, first)) { Identity(e, first, PortalIdentity.A); Association(e, first, PortalIdentity.A); }
        if (Created(e, second)) { Identity(e, second, PortalIdentity.A); Association(e, second, PortalIdentity.A); }
        foreach (var table in new[] { "usuario", "grupo_empresa" })
            e.Check("Repetição não duplica " + table, first.Before[table].GetArrayLength(), second.Count(table),
                first.Before[table].GetArrayLength() == second.Count(table));
    }, existingUser: true);

    private async Task Run(string id, Func<QaCase, Evidence, Task> test, bool existingUser = false, bool existingGroup = false)
    {
        var evidence = new Evidence(All.Single(s => s.Id == id));
        var context = new QaCase(postgres, evidence);
        try
        {
            await context.Start(existingUser, existingGroup);
            await test(context, evidence);
        }
        catch (Exception ex) { evidence.Infrastructure(postgres.Sanitize(ex)); }
        finally
        {
            try { await context.Stop(); }
            catch (Exception ex) { evidence.Infrastructure("Descarte: " + postgres.Sanitize(ex)); }
            postgres.Report.Record(evidence);
        }
        var explanation = string.Join(Environment.NewLine, evidence.Checks.Select(c =>
            $"[{c.Result}/{c.Category}] {c.Condition}: esperado {c.Expected}; obtido {c.Observed}"));
        output.WriteLine($"{id} | {evidence.Scenario.Requirement} | {evidence.Result}\n{explanation}\nRelatório: {postgres.Report.DirectoryPath}");
        if (evidence.Result != "PASSOU") throw new XunitException($"{id}: {evidence.Result}\n{explanation}");
    }

    private static bool Created(Evidence e, ApiObservation r)
    {
        e.Check("Abertura aceita", "201", r.Status + (r.DatabaseError.Length == 0 ? "" : " — " + r.DatabaseError), r.Status == 201);
        if (r.Status == 201) return true;
        e.Inconclusive("Pós-condições de abertura", $"HTTP {r.Status}: associação/upsert não considerados demonstrados por essa falha.");
        return false;
    }

    private static void NoWrites(Evidence e, ApiObservation r)
    {
        foreach (var table in new[] { "chamado", "interacao", "usuario", "grupo_empresa", "provisionamento_audit" })
            e.Check("Sem gravações em " + table, "estado idêntico", r.Before[table].GetRawText() == r.After[table].GetRawText() ? "idêntico" : "alterado",
                r.Before[table].GetRawText() == r.After[table].GetRawText());
    }

    private static void Association(Evidence e, ApiObservation r, PortalIdentity id)
    {
        var tickets = r.NewTickets.ToArray();
        e.Check("Um chamado persistido por abertura", 1, tickets.Length, tickets.Length == 1);
        if (tickets.Length != 1) { e.Inconclusive("FKs/autor", "Não há um único chamado novo para verificar."); return; }
        var ticket = tickets[0];
        var user = ticket.GetProperty("usuario_id").GetInt32();
        var group = ticket.GetProperty("grupo_empresa_id").GetInt32();
        e.Check("Identidade correta do chamado", $"{id.UserId}/{id.GroupId}", $"{user}/{group}", user == id.UserId && group == id.GroupId);
        e.Check("Não usar identidade fixa", "diferente de 4/3", $"{user}/{group}", user != 4 && group != 3);
        var interactions = r.After["interacao"].EnumerateArray().Where(x => x.GetProperty("chamado_id").GetInt32() == ticket.GetProperty("id").GetInt32()).ToArray();
        e.Check("Interação inicial única", 1, interactions.Length, interactions.Length == 1);
        if (interactions.Length == 1)
            e.Check("Autor da interação", id.UserId, interactions[0].GetProperty("autor_id").GetInt32(), interactions[0].GetProperty("autor_id").GetInt32() == id.UserId);
        var code = ticket.GetProperty("codigo_publico").GetString();
        e.Check("Código público persistido", "não vazio", code ?? "null", !string.IsNullOrWhiteSpace(code));
    }

    private static void Identity(Evidence e, ApiObservation r, PortalIdentity id)
    {
        var users = r.After["usuario"].EnumerateArray().Where(x => x.GetProperty("id").GetInt32() == id.UserId).ToArray();
        var groups = r.After["grupo_empresa"].EnumerateArray().Where(x => x.GetProperty("id").GetInt32() == id.GroupId).ToArray();
        e.Check("Usuário com ID real do portal", $"1 registro id={id.UserId}", users.Length, users.Length == 1);
        e.Check("Grupo com ID real do portal", $"1 registro id={id.GroupId}", groups.Length, groups.Length == 1);
        if (users.Length == 1)
        {
            var u = users[0];
            e.Check("Nome usuário", id.Name, u.GetProperty("nome").GetString()!, u.GetProperty("nome").GetString() == id.Name);
            e.Check("E-mail usuário", id.Email, u.GetProperty("email").GetString()!, u.GetProperty("email").GetString() == id.Email);
            e.Check("Grupo do usuário", id.GroupId, u.GetProperty("grupo_empresa_id").GetInt32(), u.GetProperty("grupo_empresa_id").GetInt32() == id.GroupId);
        }
        else e.Inconclusive("Atributos do usuário", "Usuário com ID esperado não encontrado.");
        if (groups.Length == 1)
            e.Check("Nome grupo", id.GroupName, groups[0].GetProperty("nome").GetString()!, groups[0].GetProperty("nome").GetString() == id.GroupName);
        else e.Inconclusive("Atributos do grupo", "Grupo com ID esperado não encontrado.");
    }
}

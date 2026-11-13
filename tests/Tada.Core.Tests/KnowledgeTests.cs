using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class KnowledgeTests
{
    private const string Record = """
        ---
        status: proposed
        kind: defect
        checked: {0}
        sources: Services/Payments.cs, docs/architecture.md#delivery
        ---
        # Duplicate payment outcomes must not double-release stock

        Supporting engineering context. Not a requirement.

        ## Verified
        - The consumer ignores outcomes for completed orders ([source](../../Services/Payments.cs)).

        ## Inferred
        - Retries come from at-least-once delivery.

        ## Unknown
        - Whether other consumers deduplicate.
        """;

    [TestMethod]
    public async Task RepositoryWithoutTadaKnowledgeUsesReferenceModeByDefault()
    {
        using var repo = await GitRepository.Create();
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", report.Status);
        Assert.AreEqual("reference", report.KnowledgeMode);
        Assert.AreEqual("default", report.ModeSource);
        Assert.IsNull(report.Index);
        Assert.IsNull(report.Context);
        Assert.AreEqual(0, report.Records.Length);
    }

    [TestMethod]
    [DataRow("{\"schemaVersion\":1,\"mode\":\"auto\"}")]
    [DataRow("{\"schemaVersion\":2,\"mode\":\"reference\"}")]
    [DataRow("{\"schemaVersion\":1,\"mode\":\"augment\",\"writePolicy\":\"direct\"}")]
    [DataRow("{\"schemaVersion\":1,\"mode\":\"augment\",\"mode\":\"reference\"}")]
    public async Task UnsupportedKnowledgeSettingsAreRejected(string settings)
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", settings);
        await Assert.ThrowsAsync<Exception>(() => Knowledge.Check(new Repository(repo.Root)));
    }

    [TestMethod]
    public void ChangingKnowledgeModeDoesNotInvalidateAnApprovedBaseline()
    {
        using var project = new CoreTests.Project();
        string baseline = project.Store.Prepare();
        project.Store.Approve(project.Store.Review(baseline).Id, "automated knowledge-mode control; not human acceptance");
        File.WriteAllText(Path.Combine(project.Root, ".tada", "knowledge.json"), "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        Directory.CreateDirectory(Path.Combine(project.Root, ".tada", "knowledge"));
        File.WriteAllText(Path.Combine(project.Root, ".tada", "knowledge", "note.md"), "---\nstatus: proposed\n---\n");
        File.WriteAllText(Path.Combine(project.Root, ".tada", "knowledge.md"), "# Knowledge index\n");
        project.Store.CheckCurrent(baseline);
        Assert.AreEqual(baseline, project.Store.Prepare());
        Assert.AreEqual("augment", Knowledge.ReadSettings(new Repository(project.Root)).Settings.Mode);
    }

    [TestMethod]
    public async Task ReferenceModeBlocksANewStandaloneRecordButAllowsIndexAndContext()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/context.md", "# Context\n\nCurrent objective. See [architecture](../docs/architecture.md).\n");
        repo.Write(".tada/knowledge.md", "# Knowledge index\n\n- Delivery: [architecture](../docs/architecture.md#delivery), [payments](../Services/Payments.cs)\n");
        var linked = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", linked.Status, Describe(linked));
        Assert.AreEqual(".tada/knowledge.md", linked.Index);

        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        var blocked = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("BLOCKED", blocked.Status);
        Assert.AreEqual(1, blocked.ExitCode);
        var finding = blocked.Findings.Single(f => f.Code == "reference-mode-record");
        Assert.AreEqual(".tada/knowledge/duplicate-outcomes.md", finding.Path);
        await repo.Git("add", "--all");
        await repo.Git("commit", "-m", "test: stage unsolicited record");
        // Committing the record does not legitimise it against the target branch.
        var againstBase = await Knowledge.Check(new Repository(repo.Root), "HEAD~1");
        Assert.AreEqual("BLOCKED", againstBase.Status);
    }

    [TestMethod]
    public async Task ReferenceModeKeepsImprovingRecordsCreatedEarlier()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        await repo.Commit("docs: record reviewed learning");
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"reference\"}");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head) + "\n- Payments retries every 50 ms.\n");
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", report.Status, Describe(report));
        Assert.AreEqual(".tada/knowledge.json", report.ModeSource);
    }

    [TestMethod]
    public async Task AugmentModeAcceptsAProposedEvidenceBackedRecord()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", report.Status, Describe(report));
        var record = report.Records.Single();
        Assert.AreEqual("proposed", record.Status);
        Assert.AreEqual("defect", record.Kind);
        Assert.AreEqual("current", record.State);
        CollectionAssert.AreEqual(new[] { "Services/Payments.cs", "docs/architecture.md#delivery" }, record.Sources);
    }

    [TestMethod]
    public async Task RecordBecomesStaleWhenItsSourcesChangeUntilRevalidated()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        await repo.Commit("docs: record learning");

        repo.Write("Services/Payments.cs", "class Payments { /* deduplication moved to the broker */ }\n");
        var uncommitted = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("REVIEW", uncommitted.Status);
        Assert.AreEqual(0, uncommitted.ExitCode);
        StringAssert.Contains(uncommitted.Findings.Single(f => f.Code == "source-changed").Detail, "Services/Payments.cs");
        Assert.AreEqual("stale", uncommitted.Records.Single().State);

        await repo.Commit("refactor: move deduplication");
        Assert.AreEqual("REVIEW", (await Knowledge.Check(new Repository(repo.Root))).Status);

        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        await repo.Commit("docs: revalidate learning");
        var revalidated = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", revalidated.Status, Describe(revalidated));
    }

    [TestMethod]
    public async Task IgnoredUntrackedSourceIsNotReportedAsCurrent()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".gitignore", ".git-test-hooks/\nlocal/\n");
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        await repo.Commit("chore: ignore local notes");
        repo.Write("local/notes.md", "Retries stop after ten attempts.\n");
        repo.Write(".tada/knowledge/local-notes.md", string.Format(Record, repo.Head).Replace("sources: Services/Payments.cs, docs/architecture.md#delivery", "sources: local/notes.md", StringComparison.Ordinal));
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("REVIEW", report.Status, Describe(report));
        StringAssert.Contains(report.Findings.Single(f => f.Code == "source-changed").Detail, "not tracked by Git");
        Assert.AreEqual("stale", report.Records.Single().State);
    }

    [TestMethod]
    public async Task ReferenceModeBlocksANewRecordEvenWhenGitIgnoresIt()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".gitignore", ".git-test-hooks/\n.tada/knowledge/\n");
        await repo.Commit("chore: ignore knowledge records");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("BLOCKED", report.Status, Describe(report));
        Assert.AreEqual(".tada/knowledge/duplicate-outcomes.md", report.Findings.Single(f => f.Code == "reference-mode-record").Path);
    }

    [TestMethod]
    public async Task EditingAReviewedRecordWithoutReturningToProposedNeedsReview()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/duplicate-outcomes.md", string.Format(Record, repo.Head));
        await repo.Commit("docs: propose learning");
        string reviewed = string.Format(Record, repo.Head).Replace("status: proposed", "status: reviewed", StringComparison.Ordinal);
        repo.Write(".tada/knowledge/duplicate-outcomes.md", reviewed);
        // A reviewer promoting a proposal is not an unreviewed edit.
        Assert.AreEqual("CURRENT", (await Knowledge.Check(new Repository(repo.Root))).Status);
        await repo.Commit("docs: review learning");

        const string edit = "\n- Retries stop after ten attempts.\n";
        repo.Write(".tada/knowledge/duplicate-outcomes.md", reviewed + edit);
        var edited = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("REVIEW", edited.Status);
        Assert.AreEqual("reviewed-record-edited", edited.Findings.Single().Code);
        StringAssert.Contains(string.Join(" ", edited.Next), "status: proposed");

        repo.Write(".tada/knowledge/duplicate-outcomes.md", reviewed.Replace("status: reviewed", "status: proposed", StringComparison.Ordinal) + edit);
        Assert.AreEqual("CURRENT", (await Knowledge.Check(new Repository(repo.Root))).Status);
    }

    [TestMethod]
    public async Task MissingSourcesUnknownCheckpointsAndBrokenLinksNeedReview()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/old.md", string.Format(Record, "abcdef1234567"));
        repo.Write(".tada/context.md", "# Context\n\nContinue on [the old plan](../docs/removed-plan.md).\n\n```\n[ignored](../not/a/link.md)\n```\n");
        File.Delete(Path.Combine(repo.Root, "docs", "architecture.md"));
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("REVIEW", report.Status, Describe(report));
        CollectionAssert.AreEquivalent(new[] { "source-missing", "unknown-checkpoint", "broken-link" }, report.Findings.Select(f => f.Code).ToArray());
        Assert.AreEqual(".tada/context.md", report.Findings.Single(f => f.Code == "broken-link").Path);
    }

    [TestMethod]
    [DataRow("---\nstatus: accepted\nkind: defect\nchecked: {0}\nsources: Services/Payments.cs\n---\n", "status")]
    [DataRow("---\nstatus: proposed\nkind: requirement\nchecked: {0}\nsources: Services/Payments.cs\n---\n", "kind")]
    [DataRow("---\nstatus: proposed\nkind: defect\nchecked: {0}\nsources:\n---\n", "sources")]
    [DataRow("---\nstatus: proposed\nkind: defect\nsources: Services/Payments.cs\n---\n", "checked")]
    [DataRow("---\nstatus: proposed\nkind: defect\nchecked: {0}\nsources: ../outside.cs\n---\n", "canonical")]
    [DataRow("---\nstatus: proposed\nkind: defect\nchecked: {0}\nsources: Services/Payments.cs\napproved: true\n---\n", "Unknown")]
    [DataRow("# No front matter\n", "front-matter")]
    [DataRow("---\nstatus: superseded\nkind: defect\nchecked: {0}\nsources: Services/Payments.cs\n---\n", "supersededBy")]
    public async Task RecordsWithoutProvenanceOrWithAuthorityClaimsAreRejected(string record, string expected)
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        repo.Write(".tada/knowledge/bad.md", string.Format(record, repo.Head));
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("BLOCKED", report.Status);
        StringAssert.Contains(report.Findings.Single(f => f.Code == "invalid-record").Detail, expected);
    }

    [TestMethod]
    public async Task SupersededRecordsAreRetainedWithoutStalenessButNeedAResolvableSuccessor()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/knowledge.json", "{\"schemaVersion\":1,\"mode\":\"augment\"}");
        string superseded = string.Format(Record, repo.Head).Replace("status: proposed", "status: superseded\nsupersededBy: docs/decisions/0002-broker-deduplication.md", StringComparison.Ordinal);
        repo.Write(".tada/knowledge/duplicate-outcomes.md", superseded);
        repo.Write("docs/decisions/0002-broker-deduplication.md", "# ADR 0002\n");
        await repo.Commit("docs: supersede learning");
        repo.Write("Services/Payments.cs", "class Payments { }\n");
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual("CURRENT", report.Status, Describe(report));
        Assert.AreEqual("superseded", report.Records.Single().State);
        File.Delete(Path.Combine(repo.Root, "docs", "decisions", "0002-broker-deduplication.md"));
        Assert.AreEqual("superseded-target-missing", (await Knowledge.Check(new Repository(repo.Root))).Findings.Single().Code);
    }

    [TestMethod]
    public async Task ContextHandoffReportsLaterCommitsSoAFreshSessionChecksCurrency()
    {
        using var repo = await GitRepository.Create();
        repo.Write(".tada/context.md", "# Context\n\nObjective: add cancellation. Branch: feat/cancel.\n");
        await repo.Commit("docs: record handoff");
        repo.Write("Services/Payments.cs", "class Payments { int Version = 2; }\n");
        await repo.Commit("feat: later work");
        repo.Write("Services/Payments.cs", "class Payments { int Version = 3; }\n");
        await repo.Commit("feat: more work");
        var report = await Knowledge.Check(new Repository(repo.Root));
        Assert.AreEqual(2, report.Context!.LaterCommits);
        StringAssert.Contains(string.Join("\n", report.Next), "predates 2 later commit(s)");
        repo.Write(".tada/context.md", "# Context\n\nObjective: none in progress.\n");
        Assert.AreEqual("working tree", (await Knowledge.Check(new Repository(repo.Root))).Context!.LastChanged);
    }

    [TestMethod]
    public async Task CliReportsModeAndBlocksOnlyPolicyViolations()
    {
        using var repo = await GitRepository.Create();
        var current = await Invoke(repo.Root, "knowledge", "check", "--json");
        Assert.AreEqual(0, current.ExitCode, current.Stderr);
        using (var json = JsonDocument.Parse(current.Stdout))
        {
            Assert.AreEqual("CURRENT", json.RootElement.GetProperty("status").GetString());
            Assert.AreEqual("reference", json.RootElement.GetProperty("knowledgeMode").GetString());
        }
        repo.Write(".tada/knowledge/new.md", string.Format(Record, repo.Head));
        var blocked = await Invoke(repo.Root, "knowledge", "check");
        Assert.AreEqual(1, blocked.ExitCode, blocked.Stderr);
        StringAssert.StartsWith(blocked.Stdout, "Knowledge BLOCKED");
        StringAssert.Contains(blocked.Stdout, "mode: reference (default)");
        StringAssert.Contains(blocked.Stdout, "reference-mode-record");
        StringAssert.Contains(blocked.Stdout, Knowledge.Notice);
        repo.Write(".tada/knowledge.json", "{\"mode\":\"sometimes\",\"schemaVersion\":1}");
        Assert.AreEqual(3, (await Invoke(repo.Root, "knowledge", "check")).ExitCode);
    }

    private static string Describe(KnowledgeReport report) =>
        string.Join("; ", report.Findings.Select(f => $"{f.Code} {f.Path}: {f.Detail}"));

    private static Task<CommandEvidence> Invoke(string repository, params string[] arguments)
    {
        DirectoryInfo? source = new(AppContext.BaseDirectory);
        while (source != null && !Directory.Exists(Path.Combine(source.FullName, "src", "Tada.Cli"))) source = source.Parent;
        if (source == null) throw new InvalidOperationException("Cannot locate built CLI source for black-box tests.");
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string cli = Path.Combine(source.FullName, "src", "Tada.Cli", "bin", configuration, "net10.0", "Tada.Cli.dll");
        string host = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
        if (!File.Exists(host)) host = "dotnet";
        return Processes.Execute(host, [cli, .. arguments, "--repo", repository], repository, 60);
    }

    /// <summary>A disposable Git repository with isolated configuration and no hooks.</summary>
    internal sealed class GitRepository : IDisposable
    {
        private readonly CoreTests.Project owner = new();
        private readonly Dictionary<string, string> environment;
        private readonly string hooks;
        public string Root => owner.Root;
        public string Head { get; private set; } = "";

        private GitRepository()
        {
            hooks = Path.Combine(Root, ".git-test-hooks");
            Directory.CreateDirectory(hooks);
            string config = Path.Combine(hooks, "gitconfig");
            File.WriteAllText(config, "");
            environment = new() { ["GIT_CONFIG_NOSYSTEM"] = "1", ["GIT_CONFIG_GLOBAL"] = config };
        }

        public static async Task<GitRepository> Create()
        {
            var repo = new GitRepository();
            repo.Write(".gitignore", ".git-test-hooks/\n");
            repo.Write("Services/Payments.cs", "class Payments { }\n");
            repo.Write("docs/architecture.md", "# Architecture\n\n## Delivery\n\nPayments delivers outcomes at least once.\n");
            await repo.Git("init", "--initial-branch=main", "--template=" + repo.hooks);
            await repo.Commit("chore: initial project");
            return repo;
        }

        public void Write(string relative, string content)
        {
            string path = Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content.Replace("\r\n", "\n", StringComparison.Ordinal));
        }

        public async Task Commit(string message)
        {
            await Git("add", "--all");
            await Git("commit", "-m", message);
            Head = (await Git("rev-parse", "--short=12", "HEAD")).Trim();
        }

        public async Task<string> Git(params string[] args)
        {
            var command = await Processes.Execute("git", ["-c", "core.autocrlf=false", "-c", "commit.gpgsign=false", "-c", "core.hooksPath=" + hooks,
                "-c", "user.name=TADA knowledge control", "-c", "user.email=tada-control@example.invalid", .. args], Root, 30, environment);
            Assert.AreEqual(0, command.ExitCode, command.Stdout + command.Stderr);
            return command.Stdout;
        }

        public void Dispose() => owner.Dispose();
    }
}

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tada.BuildLogger;

namespace Tada.Tests;

[TestClass]
public sealed class TaskJournalTests
{
    [TestMethod]
    public void UnselectedReadLinesInputCannotHideBehindAReviewedTarget()
    {
        using var fixture = new Fixture();
        fixture.Write("ReadLinesFromFile", fixture.Input);
        StringAssert.Contains(Assert.ThrowsExactly<TadaException>(() => fixture.Audit()).Message, "Unreviewed harness task input");
    }

    [TestMethod]
    public void DeclaredTaskReadRootsBecomeApprovedOracleInputs()
    {
        using var fixture = new Fixture();
        fixture.Contract("ReadLinesFromFile");
        fixture.Write("ReadLinesFromFile", fixture.Input);
        var input = fixture.Audit().Single(item => item.Path == fixture.Input);
        Assert.AreEqual("oracle", input.Category);
        var review = fixture.Project.Store.Review(fixture.Baseline);
        fixture.Project.Store.Approve(review.Id, "automated task-input control; not human acceptance");
        File.AppendAllText(fixture.Input, "changed data");
        Assert.ThrowsExactly<TadaException>(() => fixture.Project.Store.CheckCurrent(fixture.Baseline));
    }

    [TestMethod]
    public void OpaqueCustomCodeRequiresAnExactContract()
    {
        using var fixture = new Fixture();
        fixture.Write("OpaqueGenerator", null);
        StringAssert.Contains(Assert.ThrowsExactly<TadaException>(() => fixture.Audit()).Message, "buildTaskContracts");
        fixture.Contract("OpaqueGenerator");
        Assert.IsGreaterThan(0, fixture.Audit().Length);
        fixture.Contract("DifferentTask");
        Assert.ThrowsExactly<TadaException>(() => fixture.Audit());
    }

    [TestMethod]
    public void PropertyFileReadsNeedExplicitEvaluationTrust()
    {
        using var fixture = new Fixture();
        File.WriteAllText(fixture.Origin, "<Project><PropertyGroup><Expected>$([System.IO.File]::ReadAllText('../task-data/expected.txt'))</Expected></PropertyGroup></Project>");
        var repository = new Repository(fixture.Project.Root);
        Assert.ThrowsExactly<TadaException>(() => TaskJournal.CheckEvaluation(repository, fixture.Config, ["acceptance/Custom.targets"]));
        fixture.Contract("property-functions", "@evaluation");
        TaskJournal.CheckEvaluation(repository, fixture.Config, ["acceptance/Custom.targets"]);
        Assert.IsTrue(fixture.Inventory.Files.Any(input => input.Path == "task-data/expected.txt"));
    }

    [TestMethod]
    public void MissingTaskCompletionCannotBeReplacedWithASuccessSummary()
    {
        using var fixture = new Fixture();
        fixture.Write("Message", null, complete: false);
        StringAssert.Contains(Assert.ThrowsExactly<TadaException>(() => TaskJournal.Read(fixture.Report!, fixture.Run)).Message, "completion");
    }

    [TestMethod]
    public void ChangedTaskAssemblyCannotRetainItsOldIdentity()
    {
        using var fixture = new Fixture();
        fixture.Write("Message", null);
        File.AppendAllText(fixture.Assembly, "changed tool");
        StringAssert.Contains(Assert.ThrowsExactly<TadaException>(() => fixture.Audit()).Message, "changed after consumption");
    }

    [TestMethod]
    [DataRow("ReadLinesFromFile")]
    [DataRow("readlinesfromfile")]
    [DataRow("Microsoft.Build.Tasks.ReadLinesFromFile")]
    public void SuccessfulFileTaskWithoutInputEventsCannotPass(string task)
    {
        using var fixture = new Fixture();
        fixture.Write(task, null);
        StringAssert.Contains(Assert.ThrowsExactly<TadaException>(() => fixture.Audit()).Message, "structured task inputs are missing");
    }

    [TestMethod]
    [DataRow("Copy")]
    [DataRow("Csc")]
    [DataRow("GetFileHash")]
    [DataRow("GetAssemblyIdentity")]
    public void EmptyItemArraysDoNotRequireAnInputEvent(string task)
    {
        using var fixture = new Fixture();
        fixture.Write(task, null);
        Assert.IsGreaterThan(0, fixture.Audit().Length);
    }

    [TestMethod]
    public void ContractWildcardsCannotSilentlyTrustNewTasks()
    {
        using var fixture = new Fixture();
        fixture.Save(fixture.Config with { BuildTaskContracts = [new BuildTaskContract("acceptance/Custom.targets", "Build", "*", [])] });
        Assert.ThrowsExactly<TadaException>(() => fixture.Project.Store.Prepare());
    }

    private sealed class Fixture : IDisposable
    {
        public CoreTests.Project Project { get; } = new();
        public string Origin => Path.Combine(Project.Root, "acceptance", "Custom.targets");
        public string Input => Path.Combine(Project.Root, "task-data", "expected.txt");
        public string Sdk => Path.Combine(Project.Root, "sdk");
        public string Assembly => Path.Combine(Sdk, "Microsoft.Build.Tasks.Core.dll");
        public string Run => Path.Combine(Project.Root, ".tada", "runs", Guid.Empty.ToString("N"));
        public string Baseline { get; private set; }
        public Inventory Inventory => Project.Store.Read(Baseline);
        public Config Config => Data.Read<Config>(Path.Combine(Project.Root, "tada.json"));
        public TaskReport? Report { get; private set; }
        public Fixture()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Input)!);
            Directory.CreateDirectory(Sdk);
            Directory.CreateDirectory(Path.Combine(Run, "build"));
            File.WriteAllText(Origin, "<Project/>");
            File.WriteAllText(Input, "expected data");
            File.WriteAllText(Assembly, "synthetic SDK task code");
            Baseline = Project.Store.Prepare();
        }
        public void Save(Config config) => File.WriteAllText(Path.Combine(Project.Root, "tada.json"), JsonSerializer.Serialize(config, Data.Json));
        public void Contract(string task, string target = "Build")
        {
            Save(Config with { BuildTaskContracts = [new BuildTaskContract("acceptance/Custom.targets", target, task, ["task-data"])] });
            Baseline = Project.Store.Prepare();
        }
        public void Write(string task, string? input, bool complete = true)
        {
            DateTimeOffset time = DateTimeOffset.UtcNow.AddSeconds(-1);
            var start = new TaskEvent(1, 2, time, "task-start", "task-1", Path.Combine(Project.Root, "acceptance", "Acceptance.csproj"), Origin,
                "Build", task, Assembly, CanonicalJson.FileDigest(Assembly), "", [], true);
            var rows = new List<TaskEvent> { start with { Sequence = 1, Kind = "logger-start", Key = "" }, start };
            if (input != null) rows.Add(start with { Kind = "task-input", Parameter = "File", Values = [new TaskValue(input, input, CanonicalJson.FileDigest(input))] });
            if (complete)
            {
                rows.Add(start with { Kind = "task-finish" });
                rows.Add(start with { Kind = "logger-finish", Key = "" });
            }
            string path = Path.Combine(Run, "build", "control.tasks.ndjson");
            File.WriteAllLines(path, rows.Select((item, index) => JsonSerializer.Serialize(item with { Sequence = index + 1 }, Data.Json).Replace("\r", "").Replace("\n", "")));
            Report = new TaskReport("build/control.tasks.ndjson", CanonicalJson.FileDigest(path), "harness", "build", time.AddSeconds(-1), time.AddSeconds(1));
        }
        public BuildInput[] Audit() => TaskJournal.Audit(new Repository(Project.Root), Inventory, Config, Sdk, Run, Report!);
        public void Dispose() => Project.Dispose();
    }
}

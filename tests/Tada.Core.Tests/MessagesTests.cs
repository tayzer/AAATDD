using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tada.Tests;

[TestClass]
public sealed class MessagesTests
{
    [TestMethod]
    public void RealReqnrollStreamMapsToBothApprovedCases()
    {
        using var fixture = new Fixture();
        var result = fixture.Verify();
        Assert.AreEqual("PASS", result.Outcome, string.Join(";", result.Reasons.Select(r => r.Detail)));
        Assert.AreEqual(2, result.PassedCases.Length);
    }

    [TestMethod]
    [DataRow("meta")]
    [DataRow("source")]
    [DataRow("gherkinDocument")]
    [DataRow("testRunFinished")]
    [DataRow("testStepFinished")]
    [DataRow("testCaseFinished")]
    public void MissingEvidenceNeverPasses(string kind)
    {
        using var fixture = new Fixture();
        fixture.Events.Remove(fixture.Events.First(e => e.ContainsKey(kind)));
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void DuplicateNativeIdentityIsRejected()
    {
        using var fixture = new Fixture();
        fixture.Events.Add((JsonObject)fixture.Events.First(e => e.ContainsKey("testCaseStarted")).DeepClone());
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void UnsupportedProtocolIsNotInterpretedOptimistically()
    {
        using var fixture = new Fixture();
        fixture.Events.Single(e => e.ContainsKey("meta"))["meta"]!["protocolVersion"] = "999.0.0";
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void SkippedRequiredStepCannotPass()
    {
        using var fixture = new Fixture();
        fixture.Events.First(e => e.ContainsKey("testStepFinished"))["testStepFinished"]!["testStepResult"]!["status"] = "SKIPPED";
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void IntendedOutcomeAssertionEstablishesFailure()
    {
        using var fixture = new Fixture();
        var result = fixture.OutcomeResult();
        result["status"] = "FAILED";
        result["message"] = "Selected business defect";
        result["exception"] = new JsonObject { ["type"] = "AssertFailedException" };
        fixture.Events.Single(e => e.ContainsKey("testRunFinished"))["testRunFinished"]!["success"] = false;
        Assert.AreEqual("FAIL", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void SetupAssertionIsNotMisreportedAsBusinessRed()
    {
        using var fixture = new Fixture();
        var result = fixture.Events.First(e => e.ContainsKey("testStepFinished"))["testStepFinished"]!["testStepResult"]!;
        result["status"] = "FAILED";
        result["exception"] = new JsonObject { ["type"] = "AssertFailedException" };
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void SourceMismatchInvalidatesOtherwisePassingResults()
    {
        using var fixture = new Fixture();
        fixture.Events.Single(e => e.ContainsKey("source"))["source"]!["data"] = "Feature: Something else";
        Assert.AreEqual("INDETERMINATE", fixture.Verify().Outcome);
    }

    [TestMethod]
    public void StaleStreamIsRejectedAgainstExecutionWindow()
    {
        using var fixture = new Fixture();
        var command = new CommandEvidence("dotnet", [], 0, false, DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch.AddSeconds(1), "", "");
        Assert.AreEqual("INDETERMINATE", fixture.Verify(command).Outcome);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly CoreTests.Project project = new();
        public List<JsonObject> Events { get; } = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "orders-pass.ndjson"))
            .Select(line => JsonNode.Parse(line)!.AsObject()).ToList();
        public Fixture() => File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "orders.feature"), project.Feature, overwrite: true);
        public Verification Verify(CommandEvidence? command = null)
        {
            string stream = Path.Combine(project.Root, "messages.ndjson");
            File.WriteAllLines(stream, Events.Select(e => e.ToJsonString()));
            var inventory = new Repository(project.Root).Capture();
            return MessagesVerifier.Verify(inventory, CanonicalJson.Identity(inventory), "replay-control", stream, new Repository(project.Root), command);
        }
        public JsonNode OutcomeResult()
        {
            string pickleStep = Events.Where(e => e.ContainsKey("pickle")).First()["pickle"]!["steps"]!.AsArray()
                .First(s => s!["type"]!.GetValue<string>() == "Outcome")!["id"]!.GetValue<string>();
            string testStep = Events.Where(e => e.ContainsKey("testCase")).SelectMany(e => e["testCase"]!["testSteps"]!.AsArray())
                .First(s => s!["pickleStepId"]?.GetValue<string>() == pickleStep)!["id"]!.GetValue<string>();
            return Events.First(e => e["testStepFinished"]?["testStepId"]?.GetValue<string>() == testStep)["testStepFinished"]!["testStepResult"]!;
        }
        public void Dispose() => project.Dispose();
    }
}

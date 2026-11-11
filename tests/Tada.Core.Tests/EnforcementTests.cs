using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tada.Tests.CoreTests;

namespace Tada.Tests;

[TestClass]
public sealed class EnforcementTests
{
    private const string Base = "sha256:1111111111111111111111111111111111111111111111111111111111111111";
    private const string Current = "sha256:2222222222222222222222222222222222222222222222222222222222222222";
    private static readonly DateOnly Today = new(2026, 11, 11);

    private static EnforcementLedger Ledger(string baseline, string[] active, params Candidate[] candidates) =>
        new(1, baseline, active, candidates);

    private static Candidate Pending(string id, string since = "2026-11-10") => new(id, since, "Awaiting implementation increment");

    private static Verification Run(string baseline, string[] passed, params Reason[] reasons) =>
        new(reasons.Any(r => r.Code == "requirement-failure") ? "FAIL" : reasons.Length > 0 ? "INDETERMINATE" : "PASS",
            "L1-advisory", baseline, "run", passed, reasons);

    private static GateResult Gate(EnforcementLedger ledger, Verification? run, EnforcementLedger? baseLedger = null,
        string[]? expected = null, string[]? earlier = null, bool lineage = true, string[]? amended = null, Reason[]? gaps = null) =>
        Enforcement.Evaluate(new GateInputs(ledger, baseLedger, expected ?? ledger.Active.Concat(ledger.Candidates.Select(c => c.Scenario)).ToArray(),
            run, earlier ?? [], gaps ?? [], lineage, amended ?? [], [], [], Today));

    [TestMethod]
    public void ApprovedCandidateMayBeBehaviourallyRedWhileActiveObligationsGate()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"], Pending("AC_NEW")),
            Run(Current, ["AC_OLD"], new Reason("requirement-failure", "Expected 409", "AC_NEW")));
        Assert.AreEqual("PASS", result.Outcome);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("behavioural-red", result.Obligations.Single(o => o.Scenario == "AC_NEW").State);
        Assert.AreEqual(1, result.Obligations.Single(o => o.Scenario == "AC_NEW").AgeDays);
    }

    [TestMethod]
    public void CandidateWithoutBindingsIsPendingButNotBehaviouralRed()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"], Pending("AC_NEW")),
            Run(Current, ["AC_OLD"], new Reason("binding", "Step is unbound or ambiguous.", "AC_NEW")));
        Assert.AreEqual("PASS", result.Outcome);
        StringAssert.StartsWith(result.Obligations.Single(o => o.Scenario == "AC_NEW").Detail, "binding");
        Assert.AreEqual("incomplete", result.Obligations.Single(o => o.Scenario == "AC_NEW").State);
    }

    [TestMethod]
    public void ActiveFailureBlocksAsFail()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, [], new Reason("requirement-failure", "Expected 409", "AC_OLD")));
        Assert.AreEqual("FAIL", result.Outcome);
        Assert.AreEqual(1, result.ExitCode);
    }

    [TestMethod]
    public void EarlierFailureOfActiveObligationCannotBeDiscardedByRerun()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, ["AC_OLD"]), earlier: ["AC_OLD"]);
        Assert.AreEqual("FAIL", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "unresolved-earlier-failure"));
    }

    [TestMethod]
    public void PassingCandidateMustBePromotedBeforeCompletion()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"], Pending("AC_NEW")), Run(Current, ["AC_OLD", "AC_NEW"]));
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "candidate-satisfied-unpromoted" && r.CaseId == "AC_NEW"));
        Assert.IsTrue(result.Next.Any(n => n.Contains("Move AC_NEW")));
        Assert.AreEqual("PASS", Gate(Ledger(Current, ["AC_OLD", "AC_NEW"]), Run(Current, ["AC_OLD", "AC_NEW"])).Outcome);
    }

    [TestMethod]
    public void EveryApprovedScenarioNeedsVisibleEnforcementStatus()
    {
        var result = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, ["AC_OLD", "AC_NEW"]), expected: ["AC_OLD", "AC_NEW"]);
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "unclassified-obligation" && r.CaseId == "AC_NEW"));
        var unknown = Gate(Ledger(Current, ["AC_OLD", "AC_GONE"]), Run(Current, ["AC_OLD"]), expected: ["AC_OLD"]);
        Assert.IsTrue(unknown.Reasons.Any(r => r.Code == "unknown-obligation" && r.CaseId == "AC_GONE"));
    }

    [TestMethod]
    public void UnchangedActiveObligationCannotBeDemotedRelativeToBase()
    {
        var result = Gate(Ledger(Current, [], Pending("AC_OLD")), Run(Current, []), Ledger(Base, ["AC_OLD"]));
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "active-obligation-demoted" && r.CaseId == "AC_OLD"));
        var removed = Gate(Ledger(Current, ["AC_OTHER"]), Run(Current, ["AC_OTHER"]), Ledger(Base, ["AC_OLD"]), expected: ["AC_OTHER"]);
        Assert.IsTrue(removed.Reasons.Any(r => r.Code == "active-obligation-demoted" && r.CaseId == "AC_OLD"));
    }

    [TestMethod]
    public void ApprovedAmendmentMayReturnChangedActiveScenarioToCandidate()
    {
        var result = Gate(Ledger(Current, [], Pending("AC_OLD")),
            Run(Current, [], new Reason("requirement-failure", "New rule", "AC_OLD")), Ledger(Base, ["AC_OLD"]), amended: ["AC_OLD"]);
        Assert.AreEqual("PASS", result.Outcome);
        Assert.IsTrue(result.ProtectedChanges.Any(c => c.Kind == Obligations.Enforcement && c.Change == "returned-to-candidate"));
        var unapproved = Gate(Ledger(Current, [], Pending("AC_OLD")), Run(Current, []), Ledger(Base, ["AC_OLD"]), amended: ["AC_OLD"], lineage: false);
        Assert.IsTrue(unapproved.Reasons.Any(r => r.Code == "unapproved-lineage"));
        Assert.IsTrue(unapproved.Reasons.Any(r => r.Code == "active-obligation-demoted"));
    }

    [TestMethod]
    public void CandidateHistoryAndPolicyChangesAreVisible()
    {
        var reset = Gate(Ledger(Current, [], Pending("AC_NEW", "2026-11-09")), Run(Current, []),
            Ledger(Current, [], Pending("AC_NEW", "2026-10-01")));
        Assert.IsTrue(reset.Reasons.Any(r => r.Code == "candidate-history-changed"));
        var weakened = Gate(Ledger(Current, ["AC_OLD"]) with { Policy = new EnforcementPolicy(90) }, Run(Current, ["AC_OLD"]),
            Ledger(Current, ["AC_OLD"]) with { Policy = new EnforcementPolicy(30) });
        Assert.AreEqual("PASS", weakened.Outcome);
        Assert.IsTrue(weakened.ProtectedChanges.Any(c => c.Kind == Obligations.EnforcementPolicy && c.Change == "weakened"));
        var future = Gate(Ledger(Current, [], Pending("AC_NEW", "2027-01-01")), Run(Current, []));
        Assert.IsTrue(future.Reasons.Any(r => r.Code == "candidate-history-changed"));
    }

    [TestMethod]
    public void OverdueCandidateBlocksUnderPolicy()
    {
        var ledger = Ledger(Current, ["AC_OLD"], Pending("AC_NEW", "2026-09-01")) with { Policy = new EnforcementPolicy(30) };
        var result = Gate(ledger, Run(Current, ["AC_OLD"], new Reason("requirement-failure", "x", "AC_NEW")));
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "candidate-overdue" && r.CaseId == "AC_NEW"));
    }

    [TestMethod]
    public void WeakenedPolicyCannotReleaseOverdueCandidateInTheSameChange()
    {
        var baseLedger = Ledger(Current, ["AC_OLD"], Pending("AC_NEW", "2026-09-01")) with { Policy = new EnforcementPolicy(30) };
        var result = Gate(Ledger(Current, ["AC_OLD"], Pending("AC_NEW", "2026-09-01")),
            Run(Current, ["AC_OLD"], new Reason("requirement-failure", "x", "AC_NEW")), baseLedger);
        Assert.AreEqual("INDETERMINATE", result.Outcome);
        Assert.IsTrue(result.Reasons.Any(r => r.Code == "candidate-overdue"));
        Assert.IsTrue(result.ProtectedChanges.Any(c => c.Kind == Obligations.EnforcementPolicy));
    }

    [TestMethod]
    public void MissingExecutionIsNeverBehaviouralPass()
    {
        var withActive = Gate(Ledger(Current, ["AC_OLD"]), null);
        Assert.AreEqual("INDETERMINATE", withActive.Outcome);
        Assert.IsTrue(withActive.Reasons.Any(r => r.Code == "not-executed"));
        var nothingEnforced = Gate(Ledger(Current, [], Pending("AC_NEW")), null);
        Assert.AreEqual("NO_CHECKS", nothingEnforced.Outcome);
        Assert.AreEqual(0, nothingEnforced.ExitCode);
    }

    [TestMethod]
    public void InvalidEvidenceAndMismatchedRunsBlock()
    {
        var invalid = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, [], new Reason("invalid-evidence", "tampered")));
        Assert.AreEqual("INDETERMINATE", invalid.Outcome);
        Assert.AreEqual("unverified", invalid.Obligations.Single().State);
        var global = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, ["AC_OLD"], new Reason("runner-status", "Native run failed")));
        Assert.AreEqual("INDETERMINATE", global.Outcome);
        var mismatch = Gate(Ledger(Current, ["AC_OLD"]), Run(Base, ["AC_OLD"]));
        Assert.IsTrue(mismatch.Reasons.Any(r => r.Code == "run-baseline-mismatch"));
        var history = Gate(Ledger(Current, ["AC_OLD"]), Run(Current, ["AC_OLD"]), gaps: [new Reason("history-gap", "lost")]);
        Assert.AreEqual("INDETERMINATE", history.Outcome);
    }

    [TestMethod]
    public void LedgerRejectsAmbiguousOrHiddenEntries()
    {
        string Json(string body) => "{\"schemaVersion\":1,\"baseline\":\"" + Current + "\"," + body + "}";
        Assert.ThrowsExactly<TadaException>(() => Enforcement.Parse(Encoding.UTF8.GetBytes(Json("\"active\":[\"AC_A\"],\"candidates\":[{\"scenario\":\"AC_A\",\"since\":\"2026-11-01\",\"reason\":\"x\"}]")), "test"));
        Assert.ThrowsExactly<TadaException>(() => Enforcement.Parse(Encoding.UTF8.GetBytes(Json("\"active\":[],\"candidates\":[{\"scenario\":\"AC_A\",\"since\":\"soon\",\"reason\":\"x\"}]")), "test"));
        Assert.ThrowsExactly<TadaException>(() => Enforcement.Parse(Encoding.UTF8.GetBytes(Json("\"active\":[],\"candidates\":[{\"scenario\":\"AC_A\",\"since\":\"2026-11-01\",\"reason\":\" \"}]")), "test"));
        Assert.ThrowsExactly<System.Text.Json.JsonException>(() => Enforcement.Parse(Encoding.UTF8.GetBytes(Json("\"active\":[],\"candidates\":[],\"skip\":[\"AC_A\"]")), "test"));
        var parsed = Enforcement.Parse(Encoding.UTF8.GetBytes(Json("\"active\":[\"AC_A\"],\"candidates\":[],\"policy\":{\"candidateMaxDays\":30}")), "test");
        Assert.AreEqual(30, parsed.Policy!.CandidateMaxDays);
    }

    [TestMethod]
    public void ApprovedReplacementChainEstablishesLineage()
    {
        using var project = new Project();
        var store = project.Store;
        string first = store.Prepare();
        store.Approve(store.Review(first).Id, "human");
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("result is correct", "result is exact"));
        string second = store.Prepare();
        store.Approve(store.Review(second, first).Id, "human", first);
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("result is correct", "result is final"));
        string third = store.Prepare();
        store.Approve(store.Review(third, second).Id, "human", second);
        Assert.IsTrue(store.Supersedes(third, first));
        Assert.IsFalse(store.Supersedes(first, third));
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("result is correct", "result is unrelated"));
        string unrelated = store.Prepare();
        store.Approve(store.Review(unrelated).Id, "human");
        Assert.IsFalse(store.Supersedes(unrelated, first));
    }

    [TestMethod]
    public void DeclaredContractAndFitnessChangesAreClassifiedSeparatelyFromSupportingEdits()
    {
        using var project = new Project();
        Directory.CreateDirectory(Path.Combine(project.Root, "acceptance", "contracts"));
        Directory.CreateDirectory(Path.Combine(project.Root, "acceptance", "fitness"));
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "contracts", "order.json"), "{\"status\":201}");
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "fitness", "budgets.json"), "{\"p95Milliseconds\":500}");
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "Steps.cs"), "// binding");
        File.Delete(Path.Combine(project.Root, "tada.json"));
        Data.WriteNew(Path.Combine(project.Root, "tada.json"), new Config(1, ["features", "acceptance"],
            new Execution("acceptance/Acceptance.csproj", "Acceptance.dll", "messages.ndjson"),
            Obligations: [new ObligationRoot("contract", ["acceptance/contracts"]), new ObligationRoot("fitness", ["acceptance/fitness"])]));
        var store = project.Store;
        string first = store.Prepare();
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "contracts", "order.json"), "{\"status\":200}");
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "fitness", "budgets.json"), "{\"p95Milliseconds\":5000}");
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "Steps.cs"), "// weakened binding");
        string second = store.Prepare();
        var changes = Obligations.Compare(store, first, second);
        CollectionAssert.AreEquivalent(new[] { "contract", "fitness", "executable-interpretation" }, changes.Select(c => c.Kind).ToArray());
        Assert.IsTrue(changes.All(c => c.Change == "changed"));
        Assert.AreEqual("specification", Obligations.Kind("features/sample.feature", new Config(1, ["features"], new Execution("a.csproj", "a.dll", "m"))));
        Assert.AreEqual("verification-configuration", Obligations.Kind("acceptance/packages.lock.json", new Config(1, ["features"], new Execution("a.csproj", "a.dll", "m"))));
    }

    [TestMethod]
    public void ObligationRootsMustBeInsideTheApprovedOracle()
    {
        using var project = new Project();
        Directory.CreateDirectory(Path.Combine(project.Root, "contracts"));
        File.WriteAllText(Path.Combine(project.Root, "contracts", "order.json"), "{}");
        File.Delete(Path.Combine(project.Root, "tada.json"));
        Data.WriteNew(Path.Combine(project.Root, "tada.json"), new Config(1, ["features", "acceptance"],
            new Execution("acceptance/Acceptance.csproj", "Acceptance.dll", "messages.ndjson"),
            Obligations: [new ObligationRoot("contract", ["contracts"])]));
        Assert.ThrowsExactly<TadaException>(() => project.Store.Prepare());
    }

    [TestMethod]
    public void ScenarioFingerprintsIgnoreBindingsButTrackAgreedSteps()
    {
        using var project = new Project();
        var config = new Config(1, ["features"], new Execution("acceptance/Acceptance.csproj", "a.dll", "m"));
        var repository = new Repository(project.Root);
        FileInput[] files = [new("features/sample.feature", ["specification"], "")];
        string original = Features.Fingerprints(repository, files, config)["AC_ONE"];
        File.WriteAllText(Path.Combine(project.Root, "acceptance", "Steps.cs"), "// binding edit");
        Assert.AreEqual(original, Features.Fingerprints(repository, files, config)["AC_ONE"]);
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("Scenario: Check", "Scenario: Renamed check\n  Narrative only."));
        Assert.AreEqual(original, Features.Fingerprints(repository, files, config)["AC_ONE"]);
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("Then result is correct", "Then result is approximately correct"));
        Assert.AreNotEqual(original, Features.Fingerprints(repository, files, config)["AC_ONE"]);
        File.WriteAllText(project.Feature, Project.FeatureText.Replace("Feature: Example\n", "Feature: Example\n Background:\n  Given a shared precondition\n"));
        Assert.AreNotEqual(original, Features.Fingerprints(repository, files, config)["AC_ONE"]);
    }

    [TestMethod]
    public void GateWithoutRunReportsApprovalAndOracleDrift()
    {
        using var project = new Project();
        var store = project.Store;
        string baseline = store.Prepare();
        Data.WriteNew(Path.Combine(project.Root, ".tada", "enforcement.json"), Ledger(baseline, ["AC_ONE"]));
        var unapproved = Enforcement.Assess(store, new RunEngine(store), null, false, null, Today);
        Assert.IsTrue(unapproved.Reasons.Any(r => r.Code == "unapproved-baseline"));
        store.Approve(store.Review(baseline).Id, "automated control; not human acceptance");
        var approved = Enforcement.Assess(store, new RunEngine(store), null, false, null, Today);
        CollectionAssert.AreEqual(new[] { "not-executed" }, approved.Reasons.Select(r => r.Code).ToArray());
        File.AppendAllText(Path.Combine(project.Root, "acceptance", "packages.lock.json"), " ");
        var drift = Enforcement.Assess(store, new RunEngine(store), null, false, null, Today);
        Assert.IsTrue(drift.Reasons.Any(r => r.Code == "oracle-changed"));
    }
}

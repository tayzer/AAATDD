using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Tada;

/// <summary>An approved scenario that is not yet mandatory for merge.</summary>
public sealed record Candidate(string Scenario, string Since, string Reason);
public sealed record EnforcementPolicy(int? CandidateMaxDays = null);

/// <summary>
/// Records which approved scenarios are mandatory merge obligations. Approval authority stays in the
/// baseline review/approval records; this ledger only records enforcement status for that approved revision.
/// </summary>
public sealed record EnforcementLedger(int SchemaVersion, string Baseline, string[] Active, Candidate[] Candidates,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] EnforcementPolicy? Policy = null);

public sealed record ObligationState(string Scenario, string Enforcement, string State, string Detail,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Since = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? AgeDays = null);

public sealed record GateResult(string Outcome, string Assurance, string Baseline, string? RunId, string BaseBaseline,
    ObligationState[] Obligations, ProtectedChange[] ProtectedChanges, Reason[] Reasons, string[] Next)
{
    public int ExitCode => Outcome switch { "PASS" or "NO_CHECKS" => 0, "FAIL" => 1, _ => 2 };
}

/// <summary>Evidence gathered for one gate assessment. Kept separate so the rules are testable without execution.</summary>
public sealed record GateInputs(EnforcementLedger Ledger, EnforcementLedger? Base, string[] ExpectedScenarios,
    Verification? Run, string[] EarlierFailedScenarios, Reason[] HistoryGaps, bool LineageApproved,
    string[] AmendedScenarios, ProtectedChange[] FileChanges, Reason[] Preconditions, DateOnly Today);

public static partial class Enforcement
{
    public const string LedgerPath = ".tada/enforcement.json";
    public const string NoBase = "none";

    public static EnforcementLedger Read(Repository repository)
    {
        if (!File.Exists(Path.Combine(repository.State, "enforcement.json")))
            throw new TadaException($"No {LedgerPath}. Record the approved baseline and classify its scenarios as active or candidate.");
        var ledger = Data.Read<EnforcementLedger>(repository.Resolve(LedgerPath));
        Validate(ledger, LedgerPath);
        return ledger;
    }

    public static EnforcementLedger Parse(byte[] bytes, string source)
    {
        var ledger = Data.Parse<EnforcementLedger>(bytes, source);
        Validate(ledger, source);
        return ledger;
    }

    /// <summary>Reads the ledger committed at a Git revision, such as the pull request target branch.</summary>
    public static async Task<(bool Found, EnforcementLedger? Ledger)> FromGit(string root, string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.StartsWith('-') || reference.Contains(':') || reference.Any(char.IsControl))
            throw new TadaException("Expected a Git revision such as origin/main for --base.");
        var prefix = await Git(root, ["rev-parse", "--show-prefix"]);
        await Git(root, ["rev-parse", "--verify", "--quiet", reference + "^{commit}"]);
        string path = prefix.Trim() + LedgerPath;
        // Only an explicit empty listing means absence. Any other Git failure fails closed.
        var listing = await Git(root, ["ls-tree", "--name-only", reference, "--", path]);
        if (listing.Trim().Length == 0) return (false, null);
        var content = await Processes.Execute("git", ["-C", root, "cat-file", "blob", $"{reference}:{path}"], root, 60);
        if (content.ExitCode != 0 || content.TimedOut) throw new TadaException("Cannot read the base enforcement ledger from Git.");
        return (true, Parse(System.Text.Encoding.UTF8.GetBytes(content.Stdout), $"{reference}:{path}"));
    }

    private static async Task<string> Git(string root, string[] arguments)
    {
        var result = await Processes.Execute("git", ["-C", root, .. arguments], root, 60);
        if (result.ExitCode != 0 || result.TimedOut) throw new TadaException($"Git could not resolve the requested base revision: {result.Stderr.Trim()}");
        return result.Stdout;
    }

    private static void Validate(EnforcementLedger ledger, string source)
    {
        if (ledger.SchemaVersion != 1 || !Digest().IsMatch(ledger.Baseline) || ledger.Active.Any(id => id == null) || ledger.Candidates.Any(c => c == null))
            throw new TadaException($"Unsupported enforcement ledger: {source}");
        var ids = ledger.Active.Concat(ledger.Candidates.Select(c => c.Scenario)).ToArray();
        if (ids.Any(id => !Id().IsMatch(id)) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new TadaException("Enforcement entries need unique scenario identities, each either active or candidate.");
        foreach (var candidate in ledger.Candidates)
        {
            if (!DateOnly.TryParseExact(candidate.Since, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                string.IsNullOrWhiteSpace(candidate.Reason))
                throw new TadaException($"Candidate {candidate.Scenario} needs a yyyy-MM-dd 'since' date and a visible reason.");
        }
        if (ledger.Policy?.CandidateMaxDays is < 1 or > 3650)
            throw new TadaException("candidateMaxDays must be between 1 and 3650.");
    }

    /// <summary>Gathers approval, lineage, change and run evidence, then applies the enforcement rules.</summary>
    public static GateResult Assess(BaselineStore store, RunEngine engine, string? runId, bool baseRequested,
        EnforcementLedger? baseLedger, DateOnly today)
    {
        var ledger = Read(store.Repository);
        var inventory = store.Read(ledger.Baseline);
        var preconditions = new List<Reason>();
        try { store.Approved(ledger.Baseline); }
        catch (TadaException error) { preconditions.Add(new Reason("unapproved-baseline", error.Message)); }
        try { store.CheckCurrent(ledger.Baseline); }
        catch (TadaException error) { preconditions.Add(new Reason("oracle-changed", error.Message)); }

        bool lineage = true;
        string[] amended = [];
        ProtectedChange[] changes = [];
        if (baseLedger != null && baseLedger.Baseline != ledger.Baseline)
        {
            try
            {
                lineage = store.Supersedes(ledger.Baseline, baseLedger.Baseline);
                changes = Obligations.Compare(store, baseLedger.Baseline, ledger.Baseline);
                var before = Fingerprints(store, baseLedger.Baseline);
                var after = Fingerprints(store, ledger.Baseline);
                amended = before.Where(pair => after.TryGetValue(pair.Key, out var current) && current != pair.Value)
                    .Select(pair => pair.Key).Order(StringComparer.Ordinal).ToArray();
            }
            catch (Exception error) when (error is TadaException or IOException or System.Text.Json.JsonException)
            {
                lineage = false;
                preconditions.Add(new Reason("base-unavailable", "Cannot verify the base revision's approved obligations: " + error.Message));
            }
        }

        Verification? run = null;
        string[] earlier = [];
        Reason[] gaps = [];
        if (runId != null) (run, earlier, gaps) = engine.Evidence(runId);
        var result = Evaluate(new GateInputs(ledger, baseLedger, inventory.ExpectedCases.Select(c => c.ScenarioId).ToArray(),
            run, earlier, gaps, lineage, amended, changes, preconditions.ToArray(), today));
        return baseRequested ? result : result with { BaseBaseline = "not-compared" };
    }

    private static Dictionary<string, string> Fingerprints(BaselineStore store, string baseline)
    {
        var snapshot = new Repository(store.Snapshot(baseline));
        return Features.Fingerprints(snapshot, store.Read(baseline).Files, Data.Read<Config>(snapshot.Resolve("tada.json")));
    }

    /// <summary>Applies the enforcement rules. Only PASS or NO_CHECKS permit merge.</summary>
    public static GateResult Evaluate(GateInputs inputs)
    {
        var ledger = inputs.Ledger;
        var reasons = new List<Reason>(inputs.Preconditions);
        var changes = new List<ProtectedChange>(inputs.FileChanges);
        var states = new List<ObligationState>();
        var next = new List<string>();
        bool failed = false;
        var expected = inputs.ExpectedScenarios.ToHashSet(StringComparer.Ordinal);
        var active = ledger.Active.ToHashSet(StringComparer.Ordinal);
        var candidates = ledger.Candidates.ToDictionary(c => c.Scenario, StringComparer.Ordinal);

        foreach (var id in inputs.ExpectedScenarios.Where(id => !active.Contains(id) && !candidates.ContainsKey(id)))
        {
            reasons.Add(new Reason("unclassified-obligation", "Approved scenario has no enforcement status. Record it as active or as a visible candidate.", id));
            next.Add($"Classify {id} in {LedgerPath}.");
        }
        foreach (var id in active.Concat(candidates.Keys).Where(id => !expected.Contains(id)))
            reasons.Add(new Reason("unknown-obligation", "Enforcement entry does not identify a scenario in the approved baseline.", id));

        var baseLedger = inputs.Base;
        if (baseLedger != null)
        {
            if (baseLedger.Baseline != ledger.Baseline && !inputs.LineageApproved)
                reasons.Add(new Reason("unapproved-lineage", "The baseline does not supersede the base revision through an approved, compared replacement."));
            foreach (var id in baseLedger.Active.Where(id => !active.Contains(id)))
            {
                if (candidates.ContainsKey(id) && inputs.AmendedScenarios.Contains(id) && inputs.LineageApproved)
                    changes.Add(new ProtectedChange(Obligations.Enforcement, id, "returned-to-candidate",
                        "An approved amendment changed this active scenario. It is pending until its implementation passes."));
                else
                    reasons.Add(new Reason("active-obligation-demoted",
                        "An active obligation on the base revision cannot become pending or disappear without an approved amendment of that scenario.", id));
            }
            foreach (var prior in baseLedger.Candidates)
            {
                if (candidates.TryGetValue(prior.Scenario, out var current) && current.Since != prior.Since)
                    reasons.Add(new Reason("candidate-history-changed", "A candidate's recorded 'since' date cannot be reset.", prior.Scenario));
                if (!active.Contains(prior.Scenario) && !candidates.ContainsKey(prior.Scenario))
                    changes.Add(new ProtectedChange(Obligations.Enforcement, prior.Scenario, "candidate-removed",
                        "A pending approved obligation was withdrawn; confirm the approved amendment intends this."));
            }
            int? before = baseLedger.Policy?.CandidateMaxDays, after = ledger.Policy?.CandidateMaxDays;
            if (before != null && (after == null || after > before))
                changes.Add(new ProtectedChange(Obligations.EnforcementPolicy, "candidateMaxDays", "weakened",
                    $"Pending-candidate limit changed from {before} days to {(after == null ? "unlimited" : after + " days")}."));
        }

        // A weakened policy takes effect only after it merges; the base limit governs this change.
        int? limitDays = new[] { ledger.Policy?.CandidateMaxDays, inputs.Base?.Policy?.CandidateMaxDays }.Where(v => v != null).Min();
        foreach (var candidate in ledger.Candidates)
        {
            int age = inputs.Today.DayNumber - DateOnly.ParseExact(candidate.Since, "yyyy-MM-dd", CultureInfo.InvariantCulture).DayNumber;
            if (age < -1) reasons.Add(new Reason("candidate-history-changed", "A candidate cannot be recorded with a future 'since' date.", candidate.Scenario));
            if (limitDays is int limit && age > limit)
            {
                reasons.Add(new Reason("candidate-overdue", $"Candidate has been pending for {age} days; the policy limit is {limit}.", candidate.Scenario));
                next.Add($"Implement and promote {candidate.Scenario}, or take its amendment back to the approving humans.");
            }
        }

        var run = inputs.Run;
        if (run == null)
        {
            if (active.Count > 0)
            {
                reasons.Add(new Reason("not-executed", "Active obligations were not executed. No behavioural result is established."));
                next.Add($"Run tada run --baseline {ledger.Baseline}, then tada gate --run <run-id>.");
            }
            foreach (var id in ledger.Active) states.Add(new ObligationState(id, "active", "not-executed", "No run supplied."));
            foreach (var candidate in ledger.Candidates) states.Add(Pending(candidate, "not-executed", "No run supplied."));
        }
        else
        {
            if (run.Baseline != ledger.Baseline)
                reasons.Add(new Reason("run-baseline-mismatch", "The run assessed a different baseline from the enforcement ledger."));
            reasons.AddRange(run.Reasons.Where(reason => reason.CaseId == null));
            reasons.AddRange(inputs.HistoryGaps);
            foreach (var id in ledger.Active)
            {
                var own = run.Reasons.Where(reason => reason.CaseId == id).ToArray();
                if (inputs.EarlierFailedScenarios.Contains(id))
                {
                    failed = true;
                    reasons.Add(new Reason("unresolved-earlier-failure", "An earlier run of the same implementation and environment failed this obligation.", id));
                    states.Add(new ObligationState(id, "active", "failed", "Earlier failure is unresolved."));
                }
                else if (own.Any(reason => reason.Code == "requirement-failure"))
                {
                    failed = true;
                    reasons.AddRange(own);
                    states.Add(new ObligationState(id, "active", "failed", own.First(reason => reason.Code == "requirement-failure").Detail));
                }
                else if (own.Length > 0 || !run.PassedCases.Contains(id))
                {
                    reasons.AddRange(own.Length > 0 ? own : [new Reason("not-satisfied", "Active obligation has no verified passing result.", id)]);
                    states.Add(new ObligationState(id, "active", "unverified", own.FirstOrDefault()?.Code ?? "not-satisfied"));
                }
                else states.Add(new ObligationState(id, "active", "satisfied", "Verified PASS."));
            }
            foreach (var candidate in ledger.Candidates)
            {
                var own = run.Reasons.Where(reason => reason.CaseId == candidate.Scenario).ToArray();
                if (own.Length == 0 && run.PassedCases.Contains(candidate.Scenario) && !inputs.EarlierFailedScenarios.Contains(candidate.Scenario))
                {
                    reasons.Add(new Reason("candidate-satisfied-unpromoted", "Candidate now passes. Promote it to active in this change.", candidate.Scenario));
                    states.Add(Pending(candidate, "satisfied-unpromoted", "Verified PASS; enforcement status is stale."));
                    next.Add($"Move {candidate.Scenario} from candidates to active in {LedgerPath}.");
                }
                else if (own.Any(reason => reason.Code == "requirement-failure"))
                    states.Add(Pending(candidate, "behavioural-red", own.First(reason => reason.Code == "requirement-failure").Detail));
                else
                    states.Add(Pending(candidate, "incomplete", own.FirstOrDefault() is { } gap ? $"{gap.Code}: {gap.Detail}" : "No verified result."));
            }
        }

        if (states.Any(state => state.State is "behavioural-red" or "incomplete"))
            next.Add("Pending candidates do not block this gate. Implement them in small increments and promote each when it passes.");
        if (changes.Count > 0)
            next.Add("Protected obligations changed. Obtain independent human review of these changes before merge; local approval records are L1 declarations.");
        string outcome = failed ? "FAIL" : reasons.Count > 0 ? "INDETERMINATE" : run == null ? "NO_CHECKS" : "PASS";
        return new GateResult(outcome, "L1-advisory", ledger.Baseline, run?.RunId, baseLedger?.Baseline ?? NoBase,
            states.ToArray(), changes.ToArray(), reasons.ToArray(), next.Distinct().ToArray());

        ObligationState Pending(Candidate candidate, string state, string detail) =>
            new(candidate.Scenario, "candidate", state, detail, candidate.Since,
                inputs.Today.DayNumber - DateOnly.ParseExact(candidate.Since, "yyyy-MM-dd", CultureInfo.InvariantCulture).DayNumber);
    }

    [GeneratedRegex(@"\Asha256:[a-f0-9]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Digest();
    [GeneratedRegex(@"\A[A-Z][A-Z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex Id();
}

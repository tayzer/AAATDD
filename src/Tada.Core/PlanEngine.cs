namespace Tada;

public sealed class PlanEngine(BaselineStore baselines, string dotnet = "dotnet", Action<string>? progress = null)
{
    private readonly PlanStore plans = new(baselines);

    public async Task<PlanVerification> Run(string plan, CancellationToken cancellation = default)
    {
        var inventory = plans.Read(plan);
        string id = Guid.NewGuid().ToString("N");
        string directory = plans.Repository.StateDirectory("plan-runs", id);
        var started = DateTimeOffset.UtcNow;
        Data.WriteNew(Path.Combine(directory, "intent.json"), new { id, plan, started });
        var checks = new List<CheckRun>();
        var gaps = new List<Reason>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(inventory.Definition.TimeoutSeconds));
        foreach (var check in inventory.Checks)
        {
            if (deadline.IsCancellationRequested)
            {
                checks.Add(new CheckRun(check.Id, null));
                gaps.Add(new Reason("not-executed", "Cancellation or the plan deadline prevented this required check.", check.Id));
                continue;
            }
            progress?.Invoke($"Running {check.Id} ({check.Level}, {check.Purpose}).");
            var engine = new RunEngine(baselines, dotnet, progress);
            var result = check.Purpose == "acceptance"
                ? await engine.Run(check.Baseline, deadline.Token)
                : await engine.RunSupporting(check.Baseline, plan, deadline.Token);
            checks.Add(new CheckRun(check.Id, result.RunId));
        }
        Data.WriteNew(Path.Combine(directory, "run.json"), new PlanRun(id, plan, started, DateTimeOffset.UtcNow, checks.ToArray(), gaps.ToArray()));
        var verification = Verify(id);
        Data.WriteNew(Path.Combine(directory, "result.json"), verification);
        return verification;
    }

    public PlanVerification Verify(string id)
    {
        string plan = "unestablished";
        try
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new TadaException("Expected a retained plan run ID.");
            var run = Data.Read<PlanRun>(Path.Combine(plans.Repository.StateDirectory("plan-runs", id), "run.json"));
            plan = run.Plan;
            var inventory = plans.Read(plan);
            if (run.Id != id || run.Finished < run.Started || run.Checks.Any(check => check == null) || run.Checks.Length != inventory.Checks.Length ||
                !run.Checks.Select(check => check.Id).SequenceEqual(inventory.Checks.Select(check => check.Id)) ||
                run.Checks.Where(check => check.RunId != null).Select(check => check.RunId).Distinct().Count() != run.Checks.Count(check => check.RunId != null))
                throw new TadaException("Plan execution identity, timing or required check coverage is invalid.");
            var assessments = new List<CheckAssessment>();
            foreach (var selected in inventory.Checks)
            {
                var execution = run.Checks.Single(check => check.Id == selected.Id);
                if (execution.RunId == null)
                {
                    assessments.Add(new CheckAssessment(selected.Id, selected.Level, selected.Purpose,
                        new Verification("INDETERMINATE", "L1-advisory", selected.Baseline, "unexecuted", [], [new Reason("not-executed", "Required check did not execute.")])));
                    continue;
                }
                Verification result;
                try
                {
                    var child = Data.Read<RunRecord>(baselines.Repository.Resolve($".tada/runs/{execution.RunId}/run.json"));
                    if (child.Id != execution.RunId || child.Baseline != selected.Baseline || child.Started < run.Started || child.Finished > run.Finished ||
                        (selected.Purpose == "supporting" && child.SupportingPlan != plan))
                        throw new TadaException("Child evidence does not belong to the selected check and plan execution.");
                    result = new RunEngine(baselines, progress: progress).Verify(execution.RunId);
                }
                catch (Exception error) when (error is TadaException or IOException or System.Text.Json.JsonException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
                {
                    result = new Verification("INDETERMINATE", "L1-advisory", selected.Baseline, execution.RunId, [], [new Reason("check-integrity", error.Message)]);
                }
                assessments.Add(new CheckAssessment(selected.Id, selected.Level, selected.Purpose, result));
            }
            return Assess(plan, id, assessments.ToArray(), run.Gaps, inventory.Definition.NoChecksReason);
        }
        catch (Exception error) when (error is TadaException or IOException or System.Text.Json.JsonException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            return new PlanVerification("INDETERMINATE", "L1-advisory", plan, id, [], [new Reason("plan-integrity", error.Message)]);
        }
    }

    public static PlanVerification Assess(string plan, string id, CheckAssessment[] checks, Reason[] gaps, string? noChecksReason)
    {
        if (checks.Length == 0)
            return gaps.Length > 0 || string.IsNullOrWhiteSpace(noChecksReason)
                ? new PlanVerification("INDETERMINATE", "L1-advisory", plan, id, [], [.. gaps, new Reason("no-checks-unestablished", "An empty selection needs complete evidence and explicit rationale.")])
                : new PlanVerification("NO_CHECKS", "L1-advisory", plan, id, [], [new Reason("no-checks-selected", noChecksReason)]);
        string outcome = checks.Any(check => check.Result.Outcome == "FAIL") ? "FAIL"
            : gaps.Length > 0 || checks.Any(check => check.Result.Outcome != "PASS") ? "INDETERMINATE" : "PASS";
        return new PlanVerification(outcome, "L1-advisory", plan, id, checks, gaps);
    }
}

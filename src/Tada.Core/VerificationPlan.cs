using System.Text.RegularExpressions;

namespace Tada;

public sealed record PlannedCheck(string Id, string Level, string Purpose, string[] Obligations, string Rationale,
    string? Baseline = null, Engineering? Engineering = null);
public sealed record VerificationPlan(int SchemaVersion, string Objective, string[] Categories, bool BusinessChange,
    string[] Impact, PlannedCheck[] Checks, string[] Omissions, string? NoChecksReason = null,
    string? Previous = null, string? ChangeReason = null, int TimeoutSeconds = 3600);
public sealed record CapturedCheck(string Id, string Level, string Purpose, string Baseline);
public sealed record PlanInventory(int SchemaVersion, VerificationPlan Definition, CapturedCheck[] Checks);
public sealed record PlanPreparation(string Plan, PlanInventory Inventory, string[] ChangedChecks, string[] RemovedChecks);
public sealed record CheckRun(string Id, string? RunId);
public sealed record PlanRun(string Id, string Plan, DateTimeOffset Started, DateTimeOffset Finished, CheckRun[] Checks, Reason[] Gaps);
public sealed record CheckAssessment(string Id, string Level, string Purpose, Verification Result);
public sealed record PlanVerification(string Outcome, string Assurance, string Plan, string RunId, CheckAssessment[] Checks, Reason[] Reasons)
{
    public int ExitCode => Outcome switch { "PASS" or "NO_CHECKS" => 0, "FAIL" => 1, _ => 2 };
}

/// <summary>Captures engineering selections without granting or replacing business authority.</summary>
public sealed class PlanStore(BaselineStore baselines)
{
    public Repository Repository => baselines.Repository;

    public PlanPreparation Prepare(string file)
    {
        var definition = Data.Read<VerificationPlan>(Repository.Resolve(file));
        Validate(definition);
        var previous = definition.Previous == null ? null : Read(definition.Previous);
        if (previous != null && string.IsNullOrWhiteSpace(definition.ChangeReason))
            throw new TadaException("A revised plan needs a changeReason explaining additions, removals and changed checks.");
        var checks = definition.Checks.Select(check => new CapturedCheck(check.Id, check.Level, check.Purpose,
            check.Purpose == "acceptance" ? Acceptance(check.Baseline!) : baselines.PrepareEngineering(check.Engineering!))).ToArray();
        var inventory = new PlanInventory(1, definition, checks);
        string id = CanonicalJson.Identity(inventory);
        string path = Path(id);
        if (!File.Exists(path) && previous == null && Directory.EnumerateFiles(Repository.StateDirectory("plans"), "*.json").Any())
            throw new TadaException("An existing plan requires an explicit previous identity and changeReason for a new selection.");
        if (!File.Exists(path))
        {
            string temporary = System.IO.Path.Combine(Repository.StateDirectory("scratch"), Guid.NewGuid().ToString("N") + ".json");
            try
            {
                Data.WriteNew(temporary, inventory);
                try { File.Move(temporary, path, overwrite: false); }
                catch (IOException) when (File.Exists(path)) { Read(id); }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        Read(id);
        return new PlanPreparation(id, inventory,
            checks.Where(check => previous == null || !previous.Checks.Contains(check)).Select(check => check.Id).ToArray(),
            previous?.Checks.Where(check => !checks.Any(current => current.Id == check.Id)).Select(check => check.Id).ToArray() ?? []);

        string Acceptance(string baseline)
        {
            if (baselines.Read(baseline).Engineering != null) throw new TadaException("An engineering snapshot cannot supply business acceptance.");
            return baseline;
        }
    }

    public PlanInventory Read(string id)
    {
        var inventory = Data.Read<PlanInventory>(Path(id));
        if (inventory.SchemaVersion != 1 || CanonicalJson.Identity(inventory) != id)
            throw new TadaException("Plan identity does not match its captured content.");
        Validate(inventory.Definition);
        if (inventory.Checks.Length != inventory.Definition.Checks.Length) throw new TadaException("Captured plan check coverage differs from its definition.");
        for (int index = 0; index < inventory.Checks.Length; index++)
        {
            var captured = inventory.Checks[index];
            var check = inventory.Definition.Checks[index];
            if (captured == null) throw new TadaException("Captured check identity is missing.");
            var baseline = baselines.Read(captured.Baseline);
            if (captured.Id != check.Id || captured.Level != check.Level || captured.Purpose != check.Purpose ||
                (check.Purpose == "acceptance" ? captured.Baseline != check.Baseline || baseline.Engineering != null
                    : CanonicalJson.Identity(baseline.Engineering) != CanonicalJson.Identity(check.Engineering)))
                throw new TadaException("Captured check differs from the selected scope or purpose.");
        }
        return inventory;
    }

    private string Path(string id)
    {
        if (!Regex.IsMatch(id, @"\Asha256:[a-f0-9]{64}\z")) throw new TadaException("Expected a sha256 plan identity.");
        string path = System.IO.Path.Combine(Repository.StateDirectory("plans"), id[7..] + ".json");
        if (File.Exists(path)) Repository.NoLink(path);
        return path;
    }

    private static void Validate(VerificationPlan plan)
    {
        if (plan.SchemaVersion != 1 || string.IsNullOrWhiteSpace(plan.Objective) || plan.Checks.Any(check => check == null) || plan.Categories.Length == 0 ||
            plan.Categories.Distinct().Count() != plan.Categories.Length || plan.Categories.Any(category => category is not ("feat" or "fix" or "refactor" or "chore")) ||
            plan.Impact.Length == 0 || plan.Impact.Any(string.IsNullOrWhiteSpace) || plan.Omissions.Any(string.IsNullOrWhiteSpace) ||
            plan.TimeoutSeconds is < 1 or > 86400)
            throw new TadaException("A plan needs an objective, valid change categories, impact and a bounded deadline.");
        if (plan.Checks.Length == 0 && string.IsNullOrWhiteSpace(plan.NoChecksReason))
            throw new TadaException("Selecting no checks requires an explicit noChecksReason.");
        if (plan.BusinessChange && !plan.Checks.Any(check => check.Purpose == "acceptance"))
            throw new TadaException("A business change requires acceptance coverage regardless of its change label.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var check in plan.Checks)
        {
            if (check == null || string.IsNullOrWhiteSpace(check.Id) || !Regex.IsMatch(check.Id, @"\A[a-z][a-z0-9_-]*\z") || !ids.Add(check.Id) ||
                check.Level is not ("unit" or "service" or "contract" or "journey") ||
                check.Purpose is not ("acceptance" or "supporting") || string.IsNullOrWhiteSpace(check.Rationale) ||
                check.Obligations.Length == 0 || check.Obligations.Any(string.IsNullOrWhiteSpace))
                throw new TadaException("Checks require unique safe IDs, a level, purpose, obligations and selection rationale.");
            if (check.Purpose == "acceptance")
            {
                if (check.Baseline == null || check.Engineering != null) throw new TadaException("Acceptance checks reference a business baseline.");
            }
            else if (check.Baseline != null || check.Engineering == null || check.Engineering.RequiredTests.Length == 0 ||
                check.Engineering.RequiredTests.Any(string.IsNullOrWhiteSpace) ||
                check.Engineering.RequiredTests.Distinct(StringComparer.Ordinal).Count() != check.Engineering.RequiredTests.Length ||
                !check.Engineering.Configuration.Execution.MessagesFile.EndsWith(".trx", StringComparison.Ordinal))
                throw new TadaException("Supporting checks require captured configuration, unique required native test names and a TRX report filename.");
        }
    }
}

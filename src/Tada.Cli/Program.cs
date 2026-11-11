using System.Text.Json;
using Tada;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, signal) => { signal.Cancel = true; cancellation.Cancel(); };

try
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    var words = new List<string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal)) { words.Add(args[i]); continue; }
        string key = args[i][2..];
        string value;
        if (key is "json" or "help" or "version") value = "true";
        else
        {
            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                throw new TadaException($"Missing value for --{key}");
            value = args[++i];
        }
        if (!options.TryAdd(key, value)) throw new TadaException($"Duplicate option: --{key}");
    }
    var known = new[] { "repo", "baseline", "review", "by", "replaces", "against", "run", "dotnet", "base", "json", "help", "version", "plan", "file" };
    if (options.Keys.Except(known).Any()) throw new TadaException("Unknown option: " + options.Keys.Except(known).First());
    string Need(string name) => options.GetValueOrDefault(name) ?? throw new TadaException($"Explicit --{name} is required.");
    string? Maybe(string name) => options.GetValueOrDefault(name);
    if (options.ContainsKey("version"))
    {
        if (options.ContainsKey("json")) Console.WriteLine(JsonSerializer.Serialize(new { name = "TADA", version = BuildInfo.Version, assurance = "L1-advisory" }, Data.Json));
        else Console.WriteLine($"TADA {BuildInfo.Version} (L1-advisory)");
        return 0;
    }
    if (words.Count == 0 || options.ContainsKey("help"))
    {
        Console.WriteLine("""
        TADA — Test-Assured Development Assistance (local advisory L1)

        tada init [--repo DIRECTORY]
        tada baseline prepare [--repo DIRECTORY]
        tada baseline review --baseline sha256:... [--against sha256:...]
        tada baseline approve --review ID --by NAME [--replaces sha256:...]
        tada plan prepare --file .tada/verification-plan.json
        tada run --plan sha256:... [--dotnet PATH]
        tada run --baseline sha256:... [--dotnet PATH]
        tada verify --run ID
        tada gate [--run ID] [--base GIT-REVISION]

        --repo defaults to the current directory. --json prints structured output and suppresses progress.
        run/verify report phases and elapsed time to stderr every 10 seconds while busy.
        prepare/review never approve. Only a human may declare baseline approval.
        gate assesses mandatory (active) obligations from .tada/enforcement.json. Approved candidates
        are reported but do not block until promoted. --base compares with the target branch's ledger.
        Verification exits: PASS=0, FAIL=1, INDETERMINATE=2. Command errors=3. Gate NO_CHECKS=0.
        Read .tada/WORKFLOW.md for discovery, maintenance and session handoff.
        """);
        return 0;
    }
    string root = Path.GetFullPath(Maybe("repo") ?? Environment.CurrentDirectory);
    void Print<T>(T value)
    {
        if (options.ContainsKey("json")) Console.WriteLine(JsonSerializer.Serialize(value, Data.Json));
        else Console.WriteLine(CliOutput.Render(JsonSerializer.SerializeToElement(value, Data.Json)));
    }
    if (words.SequenceEqual(["init"]))
    {
        string? host = Environment.ProcessPath;
        if (!string.Equals(Path.GetFileNameWithoutExtension(host), "dotnet", StringComparison.OrdinalIgnoreCase)) host = null;
        var result = Onboarding.Initialise(root, host);
        Print(new { status = "initialised", created = result.Created, updated = result.Updated,
            next = "Project prepared. Enable the TADA plugin, start a fresh agent session and describe what you want to build or change normally." });
        return 0;
    }
    var repository = new Repository(root);
    var store = new BaselineStore(repository);
    switch (string.Join(' ', words))
    {
        case "plan prepare":
            Print(new PlanStore(store).Prepare(Need("file")));
            return 0;
        case "baseline prepare":
            Print(new { baseline = store.Prepare(), status = "DRAFT", next = "baseline review --baseline <digest>" });
            return 0;
        case "baseline review":
            string baseline = Need("baseline");
            var review = store.Review(baseline, Maybe("against"));
            var inventory = store.Read(baseline);
            var previous = Maybe("against") is { } old ? store.Read(old) : null;
            Print(new
            {
                review, inventory,
                buildTaskContracts = Data.Read<Config>(new Repository(store.Snapshot(baseline)).Resolve("tada.json")).BuildTaskContracts ?? [],
                changedFiles = inventory.Files.Where(f => previous == null || !previous.Files.Any(p => p.Path == f.Path && p.Digest == f.Digest)).Select(f => f.Path),
                removedFiles = previous?.Files.Where(p => !inventory.Files.Any(f => f.Path == p.Path)).Select(f => f.Path).ToArray() ?? [],
                removedCaseIds = previous?.ExpectedCases.Where(p => !inventory.ExpectedCases.Any(c => c.CaseId == p.CaseId)).Select(c => c.CaseId).ToArray() ?? [],
                protectedChanges = Maybe("against") is { } prior ? Obligations.Compare(store, prior, baseline) : [],
                specifications = inventory.Files.Where(f => f.Roles.Contains("specification")).Select(f => new
                {
                    path = f.Path,
                    content = File.ReadAllText(new Repository(store.Snapshot(baseline)).Resolve(f.Path))
                }).ToArray(),
                notice = "Review the captured specification, assertions, setup, observation and policy. This record is not approval."
            });
            return 0;
        case "baseline approve": Print(store.Approve(Need("review"), Need("by"), Maybe("replaces"))); return 0;
        case "run":
        {
            if (Maybe("plan") is { } plan)
            {
                if (Maybe("baseline") != null) throw new TadaException("Select either --plan or --baseline.");
                PlanVerification assessment;
                await using (var progress = options.ContainsKey("json") ? null : new CliProgress(Console.Error))
                    assessment = await new PlanEngine(store, Maybe("dotnet") ?? "dotnet", progress == null ? null : progress.Report).Run(plan, cancellation.Token);
                Print(assessment);
                return assessment.ExitCode;
            }
            string selectedBaseline = Need("baseline");
            Verification run;
            await using (var progress = options.ContainsKey("json") ? null : new CliProgress(Console.Error))
                run = await new RunEngine(store, Maybe("dotnet") ?? "dotnet", progress == null ? null : progress.Report).Run(selectedBaseline, cancellation.Token);
            Print(run);
            return run.ExitCode;
        }
        case "verify":
        {
            string id = Need("run");
            if (Guid.TryParseExact(id, "N", out _) && Directory.Exists(Path.Combine(repository.State, "plan-runs", id)))
            {
                PlanVerification assessment;
                await using (var progress = options.ContainsKey("json") ? null : new CliProgress(Console.Error))
                    assessment = new PlanEngine(store, progress: progress == null ? null : progress.Report).Verify(id);
                Print(assessment);
                return assessment.ExitCode;
            }
            Verification verified;
            await using (var progress = options.ContainsKey("json") ? null : new CliProgress(Console.Error))
                verified = new RunEngine(store, progress: progress == null ? null : progress.Report).Verify(id);
            Print(verified);
            return verified.ExitCode;
        }
        case "gate":
        {
            EnforcementLedger? baseLedger = null;
            if (Maybe("base") is { } reference) baseLedger = (await Enforcement.FromGit(root, reference)).Ledger;
            GateResult gate;
            await using (var progress = options.ContainsKey("json") ? null : new CliProgress(Console.Error))
                gate = Enforcement.Assess(store, new RunEngine(store, progress: progress == null ? null : progress.Report), Maybe("run"),
                    options.ContainsKey("base"), baseLedger, DateOnly.FromDateTime(DateTime.UtcNow));
            Print(gate);
            return gate.ExitCode;
        }
        default: throw new TadaException("Unknown command. Use tada --help.");
    }
}
catch (Exception error) when (error is TadaException or IOException or JsonException or UnauthorizedAccessException or Gherkin.ParserException)
{
    Console.Error.WriteLine(args.Contains("--json")
        ? JsonSerializer.Serialize(new { error = error.Message, exitCode = 3 }, Data.Json)
        : CliOutput.SafeText($"Error: {error.Message}"));
    return 3;
}

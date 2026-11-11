using System.Text.RegularExpressions;

namespace Tada;

/// <summary>
/// Knowledge creation policy. It lives outside the captured oracle, so changing it never invalidates an approved baseline.
/// </summary>
public sealed record KnowledgeSettings(int SchemaVersion, string Mode);

public sealed record KnowledgeFinding(string Code, string Severity, string Path, string Detail);

public sealed record KnowledgeRecordState(string Path, string? Status, string? Kind, string? Checked, string[] Sources,
    string? SupersededBy, string State);

/// <summary>When the working handoff last changed, so a fresh session can judge how far later work may have moved on.</summary>
public sealed record ContextState(string Path, string? LastChanged, int? LaterCommits);

public sealed record KnowledgeReport(string Status, string KnowledgeMode, string ModeSource, string? Index,
    ContextState? Context, KnowledgeRecordState[] Records, KnowledgeFinding[] Findings, string[] Next, string Notice)
{
    public int ExitCode => Status == "BLOCKED" ? 1 : 0;
}

/// <summary>
/// Checks persistent engineering knowledge for provenance and currency. Knowledge is supporting context: these checks never
/// approve, weaken or replace Gherkin, contracts, fitness obligations or accepted decisions.
/// </summary>
public static partial class Knowledge
{
    public const string SettingsPath = ".tada/knowledge.json";
    public const string IndexPath = ".tada/knowledge.md";
    public const string RecordsPath = ".tada/knowledge";
    public const string ContextPath = ".tada/context.md";
    public const string Reference = "reference";
    public const string Augment = "augment";
    public const string Notice = "Knowledge is supporting engineering context. Gherkin, approved contracts and fitness obligations, and accepted decisions keep their authority.";

    private static readonly string[] Statuses = ["proposed", "reviewed", "superseded"];
    private static readonly string[] Kinds = ["discovery", "defect", "invariant", "pitfall", "constraint", "trade-off"];
    private static readonly string[] Fields = ["status", "kind", "checked", "sources", "supersededBy"];

    /// <summary>Reads the knowledge mode. Without a settings file, TADA uses reference mode.</summary>
    public static (KnowledgeSettings Settings, string Source) ReadSettings(Repository repository)
    {
        if (!File.Exists(Path.Combine(repository.State, "knowledge.json")))
            return (new KnowledgeSettings(1, Reference), "default");
        var settings = Data.Read<KnowledgeSettings>(repository.Resolve(SettingsPath));
        if (settings.SchemaVersion != 1 || settings.Mode is not (Reference or Augment))
            throw new TadaException($"Unsupported {SettingsPath}. Use {{\"schemaVersion\":1,\"mode\":\"reference\"}} or mode \"augment\".");
        return (settings, SettingsPath);
    }

    public static async Task<KnowledgeReport> Check(Repository repository, string? baseRevision = null)
    {
        if (baseRevision != null && (string.IsNullOrWhiteSpace(baseRevision) || baseRevision.StartsWith('-') || baseRevision.Contains(':') || baseRevision.Any(char.IsControl)))
            throw new TadaException("Expected a Git revision such as origin/main for --base.");
        var (settings, source) = ReadSettings(repository);
        var findings = new List<KnowledgeFinding>();
        var git = new GitProbe(repository.Root);
        bool hasGit = await git.Available();
        if (!hasGit) findings.Add(new("git-unavailable", "review", ".", "Record currency and mode checks need a Git work tree. Check record sources manually."));
        if (baseRevision != null && hasGit && !await git.Succeeds(["rev-parse", "--verify", "--quiet", baseRevision + "^{commit}"]))
            throw new TadaException($"Git could not resolve the requested base revision: {baseRevision}");

        string? index = File.Exists(Path.Combine(repository.Root, ".tada", "knowledge.md")) ? IndexPath : null;
        if (index != null) CheckLinks(repository, index, findings);

        ContextState? context = null;
        if (File.Exists(Path.Combine(repository.Root, ".tada", "context.md")))
        {
            CheckLinks(repository, ContextPath, findings);
            context = new ContextState(ContextPath, null, null);
            if (hasGit && (await git.Output(["status", "--porcelain", "--", ContextPath])).Trim().Length > 0)
                context = context with { LastChanged = "working tree", LaterCommits = 0 };
            else if (hasGit && await git.Succeeds(["rev-parse", "--verify", "--quiet", "HEAD"]))
            {
                string last = (await git.Output(["log", "-1", "--format=%h %cs", "--", ContextPath])).Trim();
                if (last.Length > 0)
                {
                    string count = (await git.Output(["rev-list", "--count", last.Split(' ')[0] + "..HEAD"])).Trim();
                    context = context with { LastChanged = last, LaterCommits = int.Parse(count, System.Globalization.CultureInfo.InvariantCulture) };
                }
            }
        }

        var records = new List<KnowledgeRecordState>();
        string directory = Path.Combine(repository.Root, ".tada", "knowledge");
        if (Directory.Exists(directory))
        {
            Repository.NoLink(directory);
            foreach (string file in Directory.EnumerateFiles(directory, "*.md").Order(StringComparer.Ordinal))
                records.Add(await CheckRecord(repository, repository.Relative(file), git, hasGit, findings));
        }

        if (settings.Mode == Reference && hasGit && records.Count > 0)
        {
            foreach (string added in await NewRecords(git, baseRevision))
                findings.Add(new("reference-mode-record", "blocking", added,
                    "Reference mode does not create standalone knowledge records. Link existing canonical sources from the index or context, propose an update to an existing document, or ask a human to enable augment mode."));
        }

        if (hasGit)
        {
            string? revision = baseRevision ?? (await git.Succeeds(["rev-parse", "--verify", "--quiet", "HEAD"]) ? "HEAD" : null);
            foreach (var record in records.Where(r => r.Status == "reviewed" && r.State != "invalid"))
            {
                // A reviewed record whose claims changed without returning to proposed would present unreviewed edits as reviewed.
                if (revision == null || await git.Succeeds(["diff", "--quiet", revision, "--", record.Path])) continue;
                if (!await git.Succeeds(["cat-file", "-e", $"{revision}:./{record.Path}"])) continue;
                string previous = await git.Output(["show", $"{revision}:./{record.Path}"]);
                if (FrontMatter(previous).Fields.GetValueOrDefault("status") == "reviewed")
                    findings.Add(new("reviewed-record-edited", "review", record.Path,
                        $"This reviewed record changed since {revision} without returning to proposed. Set status: proposed so the edit is reviewed, or confirm a human reviewed it."));
            }
        }

        string status = findings.Any(f => f.Severity == "blocking") ? "BLOCKED" : findings.Count > 0 ? "REVIEW" : "CURRENT";
        var next = new List<string>();
        if (findings.Any(f => f.Code == "source-changed"))
            next.Add("Re-validate each changed record against current code and accepted decisions. Update its claims and `checked` commit, or mark it superseded. Do not rely on it as current until then.");
        if (findings.Any(f => f.Code is "source-missing" or "broken-link" or "superseded-target-missing"))
            next.Add("Repair or retire references to sources that no longer exist.");
        if (findings.Any(f => f.Code == "reference-mode-record"))
            next.Add("Remove the new record or ask a human to choose augment mode in .tada/knowledge.json.");
        if (findings.Any(f => f.Code == "reviewed-record-edited"))
            next.Add("Return edited reviewed records to status: proposed until a human reviews the change.");
        if (findings.Any(f => f.Code == "invalid-record"))
            next.Add("Fix record front matter: status, kind, checked commit and at least one repository source.");
        if (context?.LaterCommits > 0)
            next.Add($"The context handoff predates {context.LaterCommits} later commit(s). Confirm its objective, branch and next action against Git before relying on it.");
        return new KnowledgeReport(status, settings.Mode, source, index, context, records.ToArray(), findings.ToArray(), next.ToArray(), Notice);
    }

    private static async Task<KnowledgeRecordState> CheckRecord(Repository repository, string path, GitProbe git, bool hasGit, List<KnowledgeFinding> findings)
    {
        var (fields, error) = FrontMatter(File.ReadAllText(repository.Resolve(path)));
        string[] sources = fields.TryGetValue("sources", out var list)
            ? list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];
        var state = new KnowledgeRecordState(path, fields.GetValueOrDefault("status"), fields.GetValueOrDefault("kind"),
            fields.GetValueOrDefault("checked"), sources, fields.GetValueOrDefault("supersededBy"), "invalid");
        error ??= state.Status is not { } s || !Statuses.Contains(s) ? $"status must be one of {string.Join(", ", Statuses)}."
            : state.Kind is not { } k || !Kinds.Contains(k) ? $"kind must be one of {string.Join(", ", Kinds)}."
            : state.Checked is not { } c || !Commit().IsMatch(c) ? "checked must name the Git commit at which the claims were last validated."
            : sources.Length == 0 ? "sources must list at least one repository path that supports the record."
            : sources.FirstOrDefault(x => !SafeRelative(Strip(x))) is { } unsafePath ? $"source is not a canonical repository-relative path: {unsafePath}"
            : state.Status == "superseded" && string.IsNullOrWhiteSpace(state.SupersededBy) ? "superseded records must name supersededBy."
            : null;
        if (error != null)
        {
            findings.Add(new("invalid-record", "blocking", path, error));
            return state;
        }
        CheckLinks(repository, path, findings);
        if (state.Status == "superseded")
        {
            string target = Strip(state.SupersededBy!);
            bool exists = SafeRelative(target) && Exists(repository.Root, target);
            if (!exists) findings.Add(new("superseded-target-missing", "review", path, $"supersededBy does not resolve to a repository path: {state.SupersededBy}"));
            return state with { State = "superseded" };
        }
        string result = "current";
        var missing = sources.Select(Strip).Where(x => !Exists(repository.Root, x)).ToArray();
        foreach (string source in missing)
            findings.Add(new("source-missing", "review", path, $"Source no longer exists: {source}"));
        if (missing.Length > 0) result = "stale";
        if (!hasGit) return state with { State = missing.Length > 0 ? "stale" : "unchecked" };
        if (!await git.Succeeds(["rev-parse", "--verify", "--quiet", state.Checked + "^{commit}"]))
        {
            findings.Add(new("unknown-checkpoint", "review", path, $"Checked commit {state.Checked} is not in this repository. Re-validate the record."));
            return state with { State = "stale" };
        }
        var present = sources.Select(Strip).Where(x => Exists(repository.Root, x)).ToArray();
        if (present.Length > 0)
        {
            string changed = (await git.Output(["diff", "--name-only", "--relative", state.Checked!, "--", .. present])).Trim();
            string untracked = (await git.Output(["ls-files", "--others", "--exclude-standard", "--", .. present])).Trim();
            var paths = (changed + "\n" + untracked).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            if (paths.Length > 0)
            {
                findings.Add(new("source-changed", "review", path, $"Sources changed since {state.Checked}: {string.Join(", ", paths)}"));
                result = "stale";
            }
        }
        return state with { State = result };
    }

    /// <summary>Records added since the base revision, or since HEAD when no base is given, including uncommitted files.</summary>
    private static async Task<string[]> NewRecords(GitProbe git, string? baseRevision)
    {
        bool hasHead = await git.Succeeds(["rev-parse", "--verify", "--quiet", "HEAD"]);
        string untracked = await git.Output(["ls-files", "--others", "--exclude-standard", "--", RecordsPath]);
        string added = baseRevision != null || hasHead
            ? await git.Output(["diff", "--name-only", "--relative", "--diff-filter=A", baseRevision ?? "HEAD", "--", RecordsPath])
            : await git.Output(["ls-files", "--", RecordsPath]);
        return (untracked + "\n" + added).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.EndsWith(".md", StringComparison.Ordinal) && !x[(RecordsPath.Length + 1)..].Contains('/'))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Parses the small front-matter subset used by knowledge records: one <c>key: value</c> per line.</summary>
    public static (Dictionary<string, string> Fields, string? Error) FrontMatter(string text)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---") return (fields, "Records start with a front-matter block delimited by ---.");
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Trim() == "---") return (fields, null);
            if (line.Trim().Length == 0) continue;
            int colon = line.IndexOf(':');
            if (colon <= 0) return (fields, $"Unsupported front-matter line: {line.Trim()}");
            string key = line[..colon].Trim(), value = line[(colon + 1)..].Trim();
            if (!Fields.Contains(key)) return (fields, $"Unknown front-matter field: {key}");
            if (!fields.TryAdd(key, value)) return (fields, $"Duplicate front-matter field: {key}");
        }
        return (fields, "Front matter is not closed with ---.");
    }

    private static void CheckLinks(Repository repository, string relative, List<KnowledgeFinding> findings)
    {
        string full = repository.Resolve(relative);
        string directory = Path.GetDirectoryName(full)!;
        bool fenced = false;
        foreach (string line in File.ReadAllLines(full))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = !fenced; continue; }
            if (fenced) continue;
            foreach (Match match in Link().Matches(line))
            {
                string target = match.Groups["target"].Value.Trim();
                if (target.Length == 0 || target.StartsWith('#') || target.StartsWith('<') || Scheme().IsMatch(target)) continue;
                target = Uri.UnescapeDataString(Strip(target.Split(' ')[0]));
                if (target.Length == 0) continue;
                string resolved = Path.GetFullPath(Path.Combine(directory, target.Replace('/', Path.DirectorySeparatorChar)));
                if (!resolved.StartsWith(repository.Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                    findings.Add(new("broken-link", "review", relative, $"Link target does not exist: {match.Groups["target"].Value}"));
            }
        }
    }

    private static string Strip(string reference)
    {
        int hash = reference.IndexOf('#');
        return (hash >= 0 ? reference[..hash] : reference).Trim();
    }

    private static bool SafeRelative(string path) =>
        path.Length > 0 && !path.Contains('\\') && !Path.IsPathRooted(path) && !path.Contains(':') &&
        path.TrimEnd('/').Split('/').All(segment => segment is not ("" or "." or ".."));

    private static bool Exists(string root, string relative)
    {
        string full = Path.Combine(root, relative.TrimEnd('/').Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(full) || Directory.Exists(full);
    }

    private sealed class GitProbe(string root)
    {
        public async Task<bool> Available() => await Succeeds(["rev-parse", "--is-inside-work-tree"]);

        public async Task<bool> Succeeds(string[] arguments)
        {
            try
            {
                var result = await Processes.Execute("git", ["-C", root, .. arguments], root, 60);
                return result.ExitCode == 0 && !result.TimedOut;
            }
            catch (System.ComponentModel.Win32Exception) { return false; }
        }

        public async Task<string> Output(string[] arguments)
        {
            var result = await Processes.Execute("git", ["-c", "core.quotePath=false", "-C", root, .. arguments], root, 60);
            if (result.ExitCode != 0 || result.TimedOut) throw new TadaException($"Git could not inspect knowledge records: {result.Stderr.Trim()}");
            return result.Stdout;
        }
    }

    [GeneratedRegex(@"\A[0-9a-f]{7,64}\z")]
    private static partial Regex Commit();

    [GeneratedRegex(@"\]\((?<target>[^)\s]+(?:\s+""[^""]*"")?)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"\A[a-zA-Z][a-zA-Z0-9+.-]*:")]
    private static partial Regex Scheme();
}

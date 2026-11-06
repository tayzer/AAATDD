using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tada.BuildLogger;

namespace Tada;

public sealed record TaskReport(string Path, string Digest, string Role, string Operation, DateTimeOffset Started, DateTimeOffset Finished);
public sealed record TaskInputEvidence(int SchemaVersion, string Policy, string RepositoryRoot, string SdkDirectory, TaskReport[] Reports, BuildInput[] Inputs);

public static class TaskJournal
{
    public const string FileName = "task-inputs.json";
    public const string LoggerPath = "observer/Tada.BuildLogger.dll";
    public const string Policy = "msbuild-tasks-and-declared-inputs-v2";

    public static string Install(string run)
    {
        string destination = Path.GetFullPath(Path.Combine(run, LoggerPath));
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(typeof(Marker).Assembly.Location, destination, overwrite: false);
        if (destination.IndexOfAny([',',';']) >= 0) throw new TadaException("MSBuild logger paths containing comma or semicolon are unsupported.");
        return destination;
    }

    public static void ValidateContracts(Repository repository, Config config)
    {
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var contract in config.BuildTaskContracts ?? [])
        {
            if (contract == null || string.IsNullOrWhiteSpace(contract.Target) || string.IsNullOrWhiteSpace(contract.Task) ||
                contract.Target.Contains('*') || contract.Task.Contains('*') || !identities.Add(contract.Origin + "\n" + contract.Target + "\n" + contract.Task))
                throw new TadaException("Build-task contracts require unique exact origin/target/task identities; wildcards are unsupported.");
            repository.Resolve(contract.Origin);
            foreach (string root in contract.InputRoots)
                repository.Resolve(root, Directory.Exists(Path.Combine(repository.Root, root)));
        }
    }

    public static void CheckEvaluation(Repository repository, Config config, IEnumerable<string> paths)
    {
        string[] pure = ["System.String", "System.Math", "System.MathF", "System.Version", "System.Convert", "System.Guid", "System.DateTime", "System.DateTimeOffset",
            "System.TimeSpan", "System.Int32", "System.Int64", "System.UInt32", "System.UInt64", "System.Boolean", "System.Char", "System.IO.Path", "System.Environment"];
        string[] pureMsbuild = ["EnsureTrailingSlash", "NormalizePath", "NormalizeDirectory", "MakeRelative", "Escape", "Unescape", "ValueOrDefault", "IsOSPlatform", "IsOsUnixLike",
            "GetTargetFrameworkIdentifier", "GetTargetFrameworkVersion", "GetTargetPlatformIdentifier", "GetTargetPlatformVersion", "VersionEquals", "VersionNotEquals", "VersionGreaterThan", "VersionGreaterThanOrEquals", "VersionLessThan", "VersionLessThanOrEquals"];
        foreach (string relative in paths.Distinct(StringComparer.Ordinal))
        {
            if (!new[] { ".csproj", ".props", ".targets" }.Contains(Path.GetExtension(relative), StringComparer.OrdinalIgnoreCase)) continue;
            var xml = XDocument.Load(repository.Resolve(relative));
            bool opaque = xml.Descendants().Attributes().Any(attribute => attribute.Name.LocalName == "Condition" && Regex.IsMatch(attribute.Value, @"\bExists\s*\(", RegexOptions.IgnoreCase));
            foreach (var match in Regex.Matches(xml.ToString(), @"\$\(\s*\[\s*([^\]]+)\]\s*::\s*(\w+)", RegexOptions.CultureInvariant).Cast<Match>())
                if (!pure.Contains(match.Groups[1].Value.Trim(), StringComparer.OrdinalIgnoreCase) &&
                    !(match.Groups[1].Value.Trim().Equals("MSBuild", StringComparison.OrdinalIgnoreCase) && pureMsbuild.Contains(match.Groups[2].Value, StringComparer.OrdinalIgnoreCase))) opaque = true;
            if (opaque && !HasContract(config, relative, "@evaluation", "property-functions"))
                throw new TadaException($"Custom file/opaque evaluation in {relative} requires a reviewed buildTaskContracts entry with target @evaluation, task property-functions and all read roots.");
        }
    }

    public static BuildInput[] Audit(Repository repository, Inventory inventory, Config config, string sdk, string run, TaskReport report)
    {
        var oracle = inventory.Files.ToDictionary(file => Path.GetFullPath(repository.Resolve(file.Path)), file => file.Digest, StringComparer.OrdinalIgnoreCase);
        var implementation = repository.CapturePaths(config with { OracleRoots = config.ImplementationRoots ?? [] }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var packageDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string role in new[] { "harness", "sut" })
            foreach (string project in inventory.Files.Select(file => file.Path).Concat(implementation).Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).Distinct())
            {
                string assets = Path.Combine(run, "build", role, "obj", Path.GetFileNameWithoutExtension(project), "project.assets.json");
                if (File.Exists(assets)) foreach (string directory in BuildInputs.PackageDirectories(repository, project, assets)) packageDirectories.Add(directory);
            }
        var result = new List<BuildInput>();
        foreach (var item in Read(report, run))
        {
            if (item.Kind == "task-start")
            {
                var origin = Capture(item.Origin, "task-origin", null, report.Role);
                var assembly = Capture(item.Assembly, "task-assembly", item.AssemblyDigest, report.Role);
                bool toolchain = Trusted(origin) && Trusted(assembly);
                bool standard = assembly.Category == "sdk" && new[] { "Microsoft.Build.Tasks.Core.dll", "Microsoft.Build.dll", "Microsoft.Build.Tasks.CodeAnalysis.dll" }
                    .Contains(Path.GetFileName(item.Assembly), StringComparer.OrdinalIgnoreCase) && TaskRules.Standard(item.Name);
                string relative = repository.Relative(item.Origin);
                if (!toolchain && !standard && !HasContract(config, relative, item.Target, item.Name))
                    throw new TadaException($"Custom build task {relative} / {item.Target} / {item.Name} requires a reviewed buildTaskContracts entry and complete read-root declarations.");
            }
            else if (item.Kind == "task-input" && TaskRules.Reads(item.Name, item.Parameter))
                foreach (var value in item.Values)
                {
                    if (value.Path != null && value.Digest != null) Capture(value.Path, $"task-input:{item.Name}:{item.Parameter}", value.Digest, report.Role);
                    else if (value.Path != null && !BuildInputs.Within(value.Path, Path.Combine(run, "build")))
                        throw new TadaException($"File input {item.Name}.{item.Parameter} has no pre-task identity: {value.Value}");
                }
        }
        return result.ToArray();

        BuildInput Capture(string path, string use, string? observed, string role)
        {
            if (!Path.IsPathFullyQualified(path)) throw new TadaException($"Task {use} has no absolute file identity: {path}");
            path = Path.GetFullPath(path);
            string category;
            if (BuildInputs.Within(path, Path.Combine(run, "build")) || BuildInputs.Within(path, Path.Combine(run, role))) category = "generated";
            else if (BuildInputs.Within(path, sdk) || BuildInputs.Within(path, Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(sdk))!, "packs"))) category = "sdk";
            else if (packageDirectories.Any(root => BuildInputs.Within(path, root))) category = "locked-package";
            else if (oracle.ContainsKey(path)) category = "oracle";
            else if (role == "sut" && implementation.Contains(repository.Relative(path))) category = "implementation";
            else throw new TadaException($"Unreviewed {role} task input ({use}): {path}. Select the input roots and obtain approval for a new baseline.");
            string digest = observed ?? CanonicalJson.FileDigest(path);
            if (category == "oracle" && digest != oracle[path]) throw new TadaException($"Reviewed task input changed before consumption: {repository.Relative(path)}");
            if (category != "generated")
            {
                NoLinks(path);
                if (CanonicalJson.FileDigest(path) != digest) throw new TadaException($"Task input changed after consumption: {path}");
            }
            var input = new BuildInput(path, category, digest, [use]);
            result.Add(input);
            return input;
        }
        static bool Trusted(BuildInput input) => input.Category is "sdk" or "locked-package";
    }

    public static TaskEvent[] Read(TaskReport report, string run)
    {
        string path = new Repository(run).Resolve(report.Path);
        if (CanonicalJson.FileDigest(path) != report.Digest) throw new TadaException("MSBuild task journal changed.");
        var rows = new List<TaskEvent>();
        var active = new Dictionary<string, TaskEvent>(StringComparer.Ordinal);
        var parameters = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        long sequence = 0;
        DateTimeOffset? previous = null;
        foreach (string line in File.ReadLines(path))
        {
            using var document = JsonDocument.Parse(line);
            CanonicalJson.Encode(document.RootElement);
            var item = JsonSerializer.Deserialize<TaskEvent>(line, Data.Json) ?? throw new TadaException("Empty task journal row.");
            if (item.SchemaVersion != 1 || item.Sequence != ++sequence || item.Time < report.Started || item.Time > report.Finished || (previous != null && item.Time < previous))
                throw new TadaException("MSBuild task journal sequence/time mismatch.");
            if ((sequence == 1 && item.Kind != "logger-start") || (sequence > 1 && item.Kind == "logger-start")) throw new TadaException("MSBuild task journal startup is missing or duplicated.");
            if (rows.LastOrDefault()?.Kind == "logger-finish") throw new TadaException("Task journal extends past completion.");
            if (item.Kind == "task-start")
            {
                if (!active.TryAdd(item.Key, item)) throw new TadaException("Duplicate task identity.");
                parameters.Add(item.Key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
            else if (item.Kind is "task-input" or "task-finish")
            {
                if (!active.TryGetValue(item.Key, out var first) || first.Project != item.Project || first.Origin != item.Origin ||
                    first.Target != item.Target || first.Name != item.Name || first.Assembly != item.Assembly || first.AssemblyDigest != item.AssemblyDigest)
                    throw new TadaException("MSBuild task event has no matching task identity.");
                if (item.Kind == "task-input") parameters[item.Key].Add(item.Parameter);
                if (item.Kind == "task-finish")
                {
                    if (!TaskRules.RequiredInputs(item.Name).All(parameter => parameters[item.Key].Contains(parameter))) throw new TadaException($"Required structured task inputs are missing for {item.Name} in {item.Target}.");
                    if (!item.Succeeded || !active.Remove(item.Key)) throw new TadaException("Build task did not complete successfully.");
                    parameters.Remove(item.Key);
                }
            }
            else if (item.Kind is not ("logger-start" or "logger-finish" or "error" or "warning")) throw new TadaException("Unsupported task journal event.");
            rows.Add(item);
            previous = item.Time;
        }
        if (rows.LastOrDefault()?.Kind != "logger-finish" || !rows[^1].Succeeded || active.Count != 0) throw new TadaException("MSBuild task journal completion is missing or incomplete.");
        return rows.ToArray();
    }

    public static void Verify(string run, Inventory inventory, Config config, RunRecord record)
    {
        string path = new Repository(run).Resolve(FileName);
        if (record.TaskInputsDigest == null || CanonicalJson.FileDigest(path) != record.TaskInputsDigest) throw new TadaException("Task-input evidence is missing or changed.");
        var evidence = Data.Read<TaskInputEvidence>(path);
        if (evidence.SchemaVersion != 1 || evidence.Policy != Policy || evidence.Reports.Length == 0 ||
            !Path.IsPathFullyQualified(evidence.RepositoryRoot) || !Path.IsPathFullyQualified(evidence.SdkDirectory) || inventory.Policy.BuildTasks != Policy)
            throw new TadaException("Task-input evidence is incomplete.");
        if (!record.Artifacts.ContainsKey(LoggerPath)) throw new TadaException("Task logger has no captured artifact identity.");
        var commands = record.Commands.Where(command => command.Arguments.Any(argument => argument.StartsWith("-logger:", StringComparison.Ordinal))).ToArray();
        if (commands.Length != evidence.Reports.Length || evidence.Reports.Select(report => report.Path).Distinct(StringComparer.Ordinal).Count() != evidence.Reports.Length)
            throw new TadaException("Task journals do not cover every instrumented build command.");
        string[] rawReports = Directory.EnumerateFiles(Path.Combine(run, "build"), "*.tasks.ndjson", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(run, file).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        if (!rawReports.SequenceEqual(evidence.Reports.Select(report => report.Path).Order(StringComparer.Ordinal))) throw new TadaException("Task journal report set changed.");
        foreach (var report in evidence.Reports)
        {
            string argument = "-logger:" + Path.GetFullPath(Path.Combine(run, LoggerPath)) + ";" + Path.GetFullPath(Path.Combine(run, report.Path));
            if (report.Role is not ("harness" or "sut") || report.Operation is not ("restore" or "build") ||
                !commands.Any(command => command.Arguments.Contains(argument) && command.Started == report.Started && command.Finished == report.Finished))
                throw new TadaException("Task journal is not linked to its actual build command.");
            foreach (var item in Read(report, run))
            {
                if (item.Kind == "task-start")
                {
                    var origin = Input(item.Origin, null, "task-origin");
                    var assembly = Input(item.Assembly, item.AssemblyDigest, "task-assembly");
                    bool trusted = origin.Category is "sdk" or "locked-package" && assembly.Category is "sdk" or "locked-package";
                    bool standard = assembly.Category == "sdk" && new[] { "Microsoft.Build.Tasks.Core.dll", "Microsoft.Build.dll", "Microsoft.Build.Tasks.CodeAnalysis.dll" }
                        .Contains(Path.GetFileName(item.Assembly), StringComparer.OrdinalIgnoreCase) && TaskRules.Standard(item.Name);
                    if (!trusted && !standard && !HasContract(config, Path.GetRelativePath(evidence.RepositoryRoot, item.Origin).Replace('\\', '/'), item.Target, item.Name))
                        throw new TadaException("Task journal contains undeclared custom code.");
                }
                else if (item.Kind == "task-input" && TaskRules.Reads(item.Name, item.Parameter))
                    foreach (var value in item.Values.Where(value => value.Path != null && value.Digest != null))
                        Input(value.Path!, value.Digest, $"task-input:{item.Name}:{item.Parameter}");
            }
        }

        BuildInput Input(string path, string? digest, string use)
        {
            var input = evidence.Inputs.FirstOrDefault(input => string.Equals(input.Path, path, StringComparison.OrdinalIgnoreCase) && input.Uses.Contains(use) && (digest == null || input.Digest == digest))
                ?? throw new TadaException($"Raw task input has no captured file identity: {path}");
            if (input.Category == "oracle")
            {
                string relative = Path.GetRelativePath(evidence.RepositoryRoot, path).Replace('\\', '/');
                if (!inventory.Files.Any(file => file.Path == relative && file.Digest == input.Digest)) throw new TadaException("Task source/input differs from the approved oracle.");
            }
            if (input.Category == "sdk" && !(BuildInputs.Within(path, evidence.SdkDirectory) || BuildInputs.Within(path, Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(evidence.SdkDirectory))!, "packs"))))
                throw new TadaException("Task input claims an unrelated SDK root.");
            return input;
        }
    }

    private static bool HasContract(Config config, string origin, string target, string task) => (config.BuildTaskContracts ?? []).Any(contract =>
        string.Equals(contract.Origin, origin, StringComparison.OrdinalIgnoreCase) && string.Equals(contract.Target, target, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(contract.Task, task, StringComparison.OrdinalIgnoreCase));
    private static void NoLinks(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current)) Repository.NoLink(current);
    }
}

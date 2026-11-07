using System.Text.RegularExpressions;

namespace Tada;

public sealed partial class Repository
{
    public string Root { get; }
    public string State => Path.Combine(Root, ".tada");
    public Repository(string root)
    {
        Root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        if (!Directory.Exists(Root)) throw new TadaException("Repository directory does not exist.");
        NoLink(Root);
        if (Directory.Exists(State)) NoLink(State);
    }

    public string Resolve(string relative, bool directory = false)
    {
        if (string.IsNullOrEmpty(relative) || relative.Contains('\\') || Path.IsPathRooted(relative))
            throw new TadaException($"Expected canonical repository-relative path: {relative}");
        var segments = relative.Split('/');
        foreach (var segment in segments)
        {
            if (segment is "" or "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') ||
                segment.IndexOfAny([':', '<', '>', '"', '|', '?', '*']) >= 0 || segment.Any(char.IsControl) ||
                DeviceName().IsMatch(segment)) throw new TadaException($"Unsafe path: {relative}");
        }
        string current = Root;
        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current)) throw new TadaException($"Missing input: {relative}");
            NoLink(current);
        }
        if (directory ? !Directory.Exists(current) : !File.Exists(current))
            throw new TadaException($"Missing {(directory ? "directory" : "file")}: {relative}");
        return current;
    }

    public static void NoLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new TadaException($"Symlinks and reparse points are unsupported: {path}");
    }

    public string StateDirectory(params string[] components)
    {
        NoLink(Root);
        string current = Root;
        foreach (string component in new[] { ".tada" }.Concat(components))
        {
            if (component is "" or "." or ".." || component.IndexOfAny(['/', '\\', ':']) >= 0)
                throw new TadaException("Invalid state-directory component.");
            current = Path.Combine(current, component);
            if (Directory.Exists(current)) NoLink(current);
            else Directory.CreateDirectory(current);
        }
        return current;
    }

    public string Relative(string full) => Path.GetRelativePath(Root, full).Replace('\\', '/');

    public string[] CapturePaths(Config config)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var selectedRoots = config.OracleRoots.Concat((config.BuildTaskContracts ?? []).SelectMany(contract => contract.InputRoots.Prepend(contract.Origin))).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var selected in selectedRoots)
        {
            var full = Path.Combine(Root, selected);
            if (File.Exists(full)) paths.Add(Relative(Resolve(selected)));
            else Visit(Resolve(selected, true));
        }
        paths.Add("tada.json");
        foreach (string input in new[] { "global.json", "NuGet.Config", "dotnet-tools.json", ".config/dotnet-tools.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
            if (File.Exists(Path.Combine(Root, input))) paths.Add(input);
        foreach (var selected in selectedRoots.Append(config.Execution.Project))
        {
            string? ancestor = Path.GetDirectoryName(Path.Combine(Root, selected));
            while (ancestor != null && ancestor.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
                {
                    string imported = Path.Combine(ancestor, name);
                    if (File.Exists(imported)) paths.Add(Relative(Resolve(Relative(imported))));
                }
                ancestor = Path.GetDirectoryName(ancestor);
            }
        }
        var collisions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
            if (!collisions.Add(path)) throw new TadaException($"Case-colliding oracle path: {path}");
        return paths.Order(StringComparer.Ordinal).ToArray();

        void Visit(string directory)
        {
            foreach (string item in Directory.EnumerateFileSystemEntries(directory))
            {
                string name = Path.GetFileName(item);
                if (name is "bin" or "obj" or ".git" or ".tada") continue;
                NoLink(item);
                if (Directory.Exists(item)) Visit(item);
                else paths.Add(Relative(Resolve(Relative(item))));
            }
        }
    }

    public Inventory Capture()
    {
        var config = Data.Read<Config>(Resolve("tada.json"));
        if (config.SchemaVersion != 1 || config.OracleRoots.Length == 0) throw new TadaException("Unsupported or empty configuration.");
        TaskJournal.ValidateContracts(this, config);
        if (!config.Execution.Project.EndsWith(".csproj", StringComparison.Ordinal) ||
            !config.Execution.Assembly.EndsWith(".dll", StringComparison.Ordinal) ||
            config.Execution.Assembly != Path.GetFileName(config.Execution.Assembly) ||
            config.Execution.MessagesFile != Path.GetFileName(config.Execution.MessagesFile) ||
            config.Execution.MessagesFile.Contains('\\') || config.Execution.TimeoutSeconds is < 1 or > 3600)
            throw new TadaException("Invalid execution configuration.");
        Resolve(config.Execution.Project);
        var inputs = CapturePaths(config).Select(path => new FileInput(path,
            [path.EndsWith(".feature", StringComparison.Ordinal) ? "specification" : "support"], CanonicalJson.FileDigest(Resolve(path)))).ToArray();
        if (!inputs.Any(f => f.Path == config.Execution.Project)) throw new TadaException("Harness project is outside the captured oracle.");
        var cases = Features.Read(this, inputs.Where(x => x.Roles.Contains("specification")), config);
        if (cases.Length == 0) throw new TadaException("No acceptance cases. For a chore, use the maintenance workflow rather than inventing business scenarios.");
        var requirements = cases.SelectMany(x => x.RequirementIds).Distinct().Order(StringComparer.Ordinal).ToArray();
        string lockPath = Path.Combine(Path.GetDirectoryName(config.Execution.Project)!, "packages.lock.json").Replace('\\', '/');
        if (!inputs.Any(f => f.Path == lockPath))
            throw new TadaException("Restore the harness first to create packages.lock.json, then prepare the baseline.");
        if (!inputs.Any(f => f.Path == "global.json") || !inputs.Any(f => f.Path == "NuGet.Config"))
            throw new TadaException("A repository global.json and NuGet.Config are required to identify execution inputs.");
        var external = new[]
        {
            new ExternalInput("dotnet-sdk-policy", inputs.Single(f => f.Path == "global.json").Digest),
            new ExternalInput("nuget-locked-graph", inputs.Single(f => f.Path == lockPath).Digest)
        }.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        return new Inventory(6, new Scope(requirements, cases.Select(x => x.ScenarioId).ToArray()), inputs, cases,
            config.Execution, external, new Policy(BuildInputs: BuildInputs.Policy, RuntimeInputs: RuntimeInputs.Policy, BuildTasks: TaskJournal.Policy));
    }

    [GeneratedRegex(@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DeviceName();
}

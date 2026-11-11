namespace Tada;

/// <summary>A difference between two captured revisions of protected verification inputs.</summary>
public sealed record ProtectedChange(string Kind, string Subject, string Change, string Detail);

/// <summary>
/// Classifies captured oracle inputs by the kind of obligation they express. Classification makes
/// protected changes explicit for human review; every captured input remains approval-bound.
/// </summary>
public static class Obligations
{
    public const string Specification = "specification";
    public const string ExecutableInterpretation = "executable-interpretation";
    public const string Contract = "contract";
    public const string Fitness = "fitness";
    public const string VerificationConfiguration = "verification-configuration";
    public const string Enforcement = "enforcement";
    public const string EnforcementPolicy = "enforcement-policy";

    private static readonly string[] DeclaredKinds = [Contract, Fitness];

    public static void Validate(Config config, string[] capturedPaths)
    {
        foreach (var declaration in config.Obligations ?? [])
        {
            if (declaration == null || !DeclaredKinds.Contains(declaration.Kind) || declaration.Roots.Length == 0)
                throw new TadaException("Declared obligations need a kind of 'contract' or 'fitness' and at least one root.");
            foreach (string root in declaration.Roots)
            {
                if (string.IsNullOrWhiteSpace(root) || root.Contains('\\') || root.EndsWith('/') ||
                    !capturedPaths.Any(path => path == root || path.StartsWith(root + "/", StringComparison.Ordinal)))
                    throw new TadaException($"Declared {declaration.Kind} obligation root is not inside the captured oracle: {root}");
            }
        }
        var roots = (config.Obligations ?? []).SelectMany(declaration => declaration.Roots).ToArray();
        if (roots.Distinct(StringComparer.Ordinal).Count() != roots.Length)
            throw new TadaException("An obligation root can be declared only once.");
    }

    public static string Kind(string path, Config config)
    {
        foreach (var declaration in config.Obligations ?? [])
            if (declaration.Roots.Any(root => path == root || path.StartsWith(root + "/", StringComparison.Ordinal)))
                return declaration.Kind;
        if (path.EndsWith(".feature", StringComparison.Ordinal)) return Specification;
        string name = Path.GetFileName(path);
        if (path is "tada.json" or "global.json" or "NuGet.Config" or "dotnet-tools.json" or ".config/dotnet-tools.json" ||
            name is "packages.lock.json" or "reqnroll.json" || name.StartsWith("Directory.", StringComparison.Ordinal) ||
            name.EndsWith(".csproj", StringComparison.Ordinal) || name.EndsWith(".props", StringComparison.Ordinal) ||
            name.EndsWith(".targets", StringComparison.Ordinal))
            return VerificationConfiguration;
        return ExecutableInterpretation;
    }

    public static string Describe(string kind) => kind switch
    {
        Specification => "Agreed Gherkin business expectation.",
        Contract => "Agreed service-contract expectation.",
        Fitness => "Agreed architectural or non-functional fitness obligation.",
        VerificationConfiguration => "Verification configuration, dependency or runner input.",
        Enforcement => "Merge-enforcement status of an approved obligation.",
        EnforcementPolicy => "Enforcement policy for pending candidate obligations.",
        _ => "Executable interpretation: bindings, assertions, fixtures, observers or drivers."
    };

    /// <summary>Compares two captured baselines. Both revisions are read through their integrity checks.</summary>
    public static ProtectedChange[] Compare(BaselineStore store, string fromId, string toId)
    {
        var from = store.Read(fromId);
        var to = store.Read(toId);
        var fromConfig = Data.Read<Config>(new Repository(store.Snapshot(fromId)).Resolve("tada.json"));
        var toConfig = Data.Read<Config>(new Repository(store.Snapshot(toId)).Resolve("tada.json"));
        var changes = new List<ProtectedChange>();
        foreach (var file in to.Files)
        {
            string kind = Kind(file.Path, toConfig);
            var prior = from.Files.FirstOrDefault(previous => previous.Path == file.Path);
            if (prior == null) changes.Add(new ProtectedChange(kind, file.Path, "added", Describe(kind)));
            else if (prior.Digest != file.Digest) changes.Add(new ProtectedChange(kind, file.Path, "changed", Describe(kind)));
            else if (Kind(prior.Path, fromConfig) is var previousKind && previousKind != kind)
                changes.Add(new ProtectedChange(previousKind, file.Path, "reclassified", $"Reclassified from {previousKind} to {kind}."));
        }
        foreach (var removed in from.Files.Where(previous => to.Files.All(file => file.Path != previous.Path)))
        {
            string kind = Kind(removed.Path, fromConfig);
            changes.Add(new ProtectedChange(kind, removed.Path, "removed", Describe(kind)));
        }
        return changes.OrderBy(change => change.Kind, StringComparer.Ordinal).ThenBy(change => change.Subject, StringComparer.Ordinal).ToArray();
    }
}

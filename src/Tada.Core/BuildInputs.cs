using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace Tada;

public sealed record BuildInput(string Path, string Category, string Digest, string[] Uses);
public sealed record ProjectDependency(string Path, string Role, bool BuildOnlySut);
public sealed record ProjectBuildInputs(string Project, string Role, BuildInput[] Before, BuildInput[] After,
    Dictionary<string, string> Reports,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProjectDependency[]? Dependencies = null);
public sealed record BuildInputEvidence(int SchemaVersion, string Policy, string SdkDirectory, ProjectBuildInputs[] Projects);

/// <summary>
/// Audits the supported SDK build's evaluated inputs. This is local observation, not a sandbox:
/// reviewed targets and the installed SDK/package cache remain trusted executable tools.
/// </summary>
public static class BuildInputs
{
    public const string LegacyPolicy = "evaluated-dotnet-build-v3";
    public const string Policy = "evaluated-dotnet-build-v4";
    public static bool Supported(string? policy) => policy is Policy or LegacyPolicy;
    public const string FileName = "build-inputs.json";
    public const string Items = "Compile,EmbeddedResource,AdditionalFiles,Analyzer,EditorConfigFiles,GlobalAnalyzerConfigFiles,ReferencePath,ReferenceCopyLocalPaths,ProjectReference,Content,None,ReqnrollFeatureFile";
    public const string Properties = "MSBuildProjectFullPath,BaseIntermediateOutputPath,IntermediateOutputPath,MSBuildProjectExtensionsPath,ProjectAssetsFile,NuGetPackageRoot,CompilerResponseFile,AssemblyOriginatorKeyFile,Win32Resource,ApplicationIcon,ApplicationManifest,CodeAnalysisRuleSet,SourceLink,AppConfig,TadaVerifiedBuild";

    public static ProjectDependency[] ReadDependencies(string evaluation, string role, string? sutProject, string[]? serviceProjects = null,
        IReadOnlyDictionary<string, string>? serviceIds = null)
    {
        var applications = serviceProjects ?? (sutProject == null ? [] : [sutProject]);
        using var document = JsonDocument.Parse(File.ReadAllBytes(evaluation));
        return document.RootElement.GetProperty("Items").GetProperty("ProjectReference").EnumerateArray().Select(item =>
        {
            string path = Path.GetFullPath(item.GetProperty("FullPath").GetString()!);
            bool sut = item.TryGetProperty("OutputItemType", out var output) && output.GetString() == "TadaSutOutput";
            if (sut && (role != "harness" || !applications.Contains(path, StringComparer.OrdinalIgnoreCase) ||
                !item.TryGetProperty("ReferenceOutputAssembly", out var reference) || !string.Equals(reference.GetString(), "false", StringComparison.OrdinalIgnoreCase) ||
                !item.TryGetProperty("Private", out var copy) || !string.Equals(copy.GetString(), "false", StringComparison.OrdinalIgnoreCase)))
                throw new TadaException("TadaSutOutput must be a build-only reference to the configured SUT, with ReferenceOutputAssembly=false and Private=false.");
            if (sut && serviceIds != null && (!item.TryGetProperty("TadaServiceId", out var id) || !serviceIds.TryGetValue(path, out string? expectedId) || id.GetString() != expectedId))
                throw new TadaException("Native TadaServiceId must match the captured service ID for this project.");
            return new ProjectDependency(path, sut ? "sut" : role, sut);
        }).OrderBy(item => item.Path, StringComparer.Ordinal).ToArray();
    }

    public static string FindSdk(string installedSdks, string version)
    {
        var roots = installedSdks.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())
            .Where(line => line.StartsWith(version + " [", StringComparison.Ordinal) && line.EndsWith(']'))
            .Select(line => Path.GetFullPath(Path.Combine(line[(version.Length + 2)..^1], version))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (roots.Length != 1 || !File.Exists(Path.Combine(roots[0], "MSBuild.dll")))
            throw new TadaException("Cannot identify the pinned SDK installation from dotnet --list-sdks.");
        return roots[0];
    }

    public static bool Within(string path, string directory) =>
        Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    // MSBuildAllProjects does not enumerate all SDK/package imports. Preprocessing emits
    // import boundary comments containing the full imported filename, including implicit imports.
    public static string[] Imports(string report)
    {
        using var reader = XmlReader.Create(report, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var comment in document.DescendantNodes().OfType<XComment>())
        {
            var lines = comment.Value.Split('\n').Select(line => line.Trim()).ToArray();
            for (int i = 1; i + 1 < lines.Length; i++)
                if (Path.IsPathFullyQualified(lines[i]) && Separator(lines[i + 1])) result.Add(Path.GetFullPath(lines[i]));
        }
        if (result.Count == 0) throw new TadaException("MSBuild preprocessing did not identify any project/import inputs.");
        return result.Order(StringComparer.Ordinal).ToArray();

        static bool Separator(string line) => line.Length >= 10 && line.All(c => c == '=');
    }

    public static BuildInput[] Capture(Repository repository, Inventory inventory, Config config, string sdk,
        string generated, string output, string role, string project, string evaluation, string preprocessed)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(evaluation));
        var properties = document.RootElement.GetProperty("Properties");
        if (inventory.Policy.BuildInputs == Policy && (!properties.TryGetProperty("TadaVerifiedBuild", out var verified) || verified.GetString() != "true"))
            throw new TadaException("Recorded builds must set TadaVerifiedBuild=true; native staging is not verified execution.");
        string projectFile = repository.Resolve(project);
        if (!string.Equals(properties.GetProperty("MSBuildProjectFullPath").GetString(), projectFile, StringComparison.OrdinalIgnoreCase))
            throw new TadaException("Evaluated project identity differs from the requested build.");
        foreach (string property in new[] { "BaseIntermediateOutputPath", "IntermediateOutputPath", "MSBuildProjectExtensionsPath", "ProjectAssetsFile" })
        {
            string value = properties.GetProperty(property).GetString() ?? "";
            if (!Path.IsPathFullyQualified(value)) value = Path.GetFullPath(value, Path.GetDirectoryName(projectFile)!);
            if (!Within(value, generated)) throw new TadaException($"{property} must remain inside this run's fresh build directory: {value}");
        }
        var oracle = inventory.Files.ToDictionary(file => Path.GetFullPath(repository.Resolve(file.Path)), file => file.Digest, StringComparer.OrdinalIgnoreCase);
        var implementation = role == "sut" || inventory.Engineering != null ? repository.CapturePaths(config with { OracleRoots = config.ImplementationRoots ?? [] })
            .ToHashSet(StringComparer.OrdinalIgnoreCase) : [];
        string assets = properties.GetProperty("ProjectAssetsFile").GetString()!;
        var packages = PackageDirectories(repository, project, assets);
        var sutReferences = inventory.Policy.BuildInputs == Policy
            ? ReadDependencies(evaluation, role, null, config.Execution.Applications().Select(service => repository.Resolve(service.Project)).ToArray(),
                config.Execution.Services?.ToDictionary(service => repository.Resolve(service.Project), service => service.Id, StringComparer.OrdinalIgnoreCase))
                .Where(item => item.BuildOnlySut).Select(item => item.Path).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var paths = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        Add(projectFile, "project");
        Add(Path.Combine(Path.GetDirectoryName(projectFile)!, "packages.lock.json"), "lock");
        Add(assets, "restore-assets");
        foreach (string property in new[] { "CompilerResponseFile", "AssemblyOriginatorKeyFile", "Win32Resource", "ApplicationIcon", "ApplicationManifest", "CodeAnalysisRuleSet", "SourceLink", "AppConfig" })
            if (properties.TryGetProperty(property, out var value) && value.GetString() is { Length: > 0 } selected)
            {
                if (property == "CompilerResponseFile") throw new TadaException("Custom compiler response files are unsupported; declare compiler settings and inputs in reviewed project files.");
                foreach (string path in selected.Split(';', StringSplitOptions.RemoveEmptyEntries)) Add(path, property);
            }
        foreach (string import in Imports(preprocessed)) Add(import, "import");
        foreach (var group in document.RootElement.GetProperty("Items").EnumerateObject())
            foreach (var item in group.Value.EnumerateArray())
                Add(item.GetProperty("FullPath").GetString()!, group.Name);
        var result = new List<BuildInput>();
        string dotnetRoot = Path.GetDirectoryName(Path.GetDirectoryName(sdk))!;
        foreach (var pair in paths.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            string path = pair.Key;
            // SDK analyzer-config discovery includes absent ancestor candidates such as E:\.globalconfig.
            // Missing required compiler inputs still fail the build; disappearing observed inputs fail stability.
            if (!File.Exists(path)) continue;
            string category;
            if (Within(path, generated) || Within(path, output)) category = "generated";
            else if (Within(path, sdk) || Within(path, Path.Combine(dotnetRoot, "packs")) || Within(path, Path.Combine(dotnetRoot, "sdk-manifests"))) category = "sdk";
            else if (packages.Any(package => Within(path, package))) category = "locked-package";
            else if (sutReferences.Contains(path) && pair.Value.SetEquals(["ProjectReference"])) category = "sut-reference";
            else if (oracle.TryGetValue(path, out string? approved))
            {
                category = "oracle";
                if (File.Exists(path) && CanonicalJson.FileDigest(path) != approved) throw new TadaException($"Reviewed build input changed: {repository.Relative(path)}");
            }
            else if (Within(path, repository.Root) && implementation.Contains(repository.Relative(path))) category = "implementation";
            else throw new TadaException($"Unreviewed {role} build input: {path}. Include repository inputs in the appropriate selected roots; external/shared inputs are unsupported.");
            NoLinks(path);
            result.Add(new BuildInput(path, category, CanonicalJson.FileDigest(path), pair.Value.Order(StringComparer.Ordinal).ToArray()));
        }
        return result.ToArray();

        void Add(string path, string use)
        {
            path = Path.GetFullPath(path, Path.GetDirectoryName(projectFile)!);
            if (!paths.TryGetValue(path, out var uses)) paths.Add(path, uses = new(StringComparer.Ordinal));
            uses.Add(use);
        }
    }

    public static void CheckStable(BuildInput[] before, BuildInput[] after)
    {
        var current = after.ToDictionary(input => input.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var input in before.Where(input => input.Category != "generated"))
            if (!current.TryGetValue(input.Path, out var next) || next.Digest != input.Digest)
                throw new TadaException($"Build input changed during build: {input.Path}");
    }

    public static void CheckCurrent(IEnumerable<BuildInput> inputs)
    {
        foreach (var group in inputs.GroupBy(input => input.Path, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Select(input => input.Digest).Distinct(StringComparer.Ordinal).Count() != 1)
                throw new TadaException($"Build input had inconsistent identities across project builds: {group.Key}");
            var input = group.First();
            NoLinks(input.Path);
            if (CanonicalJson.FileDigest(input.Path) != input.Digest) throw new TadaException($"Captured build input changed during execution: {input.Path}");
        }
    }

    public static void Verify(string run, Inventory inventory, string? digest)
    {
        if (inventory.SchemaVersion is not (6 or 7) || !Supported(inventory.Policy.BuildInputs))
            throw new TadaException("This baseline predates the evaluated-build-input policy. Prepare, review and approve a new revision; historical inputs are not inferred.");
        string path = new Repository(run).Resolve(FileName);
        if (string.IsNullOrEmpty(digest) || CanonicalJson.FileDigest(path) != digest)
            throw new TadaException("Build-input evidence is missing or changed.");
        var evidence = Data.Read<BuildInputEvidence>(path);
        if (evidence.Policy != inventory.Policy.BuildInputs || evidence.SchemaVersion != (evidence.Policy == Policy ? 2 : 1) || evidence.Projects.Length == 0)
            throw new TadaException("Unsupported or empty build-input evidence.");
        if (evidence.Projects.Any(project => project == null) ||
            evidence.Projects.GroupBy(project => (project.Role, project.Project)).Any(group => group.Count() != 1))
            throw new TadaException("Null or duplicate project build-input evidence.");
        foreach (var (project, role) in new[] { (inventory.Execution.Project, "harness") }
            .Concat(inventory.Execution.Applications().Select(service => (service.Project, "sut"))))
            if (project != null && !evidence.Projects.Any(item => item.Project == project && item.Role == role))
                throw new TadaException($"Build-input evidence does not cover the configured {role} project.");
        foreach (var project in evidence.Projects)
        {
            if (project.Before.Length == 0 || project.After.Length == 0 || project.Reports.Count != 4)
                throw new TadaException("Incomplete project build-input evidence.");
            foreach (var input in project.Before.Concat(project.After))
                if (input == null || !Path.IsPathFullyQualified(input.Path) || input.Uses.Length == 0 ||
                    !(input.Category is "oracle" or "implementation" or "generated" or "sdk" or "locked-package" ||
                        evidence.SchemaVersion == 2 && input.Category == "sut-reference") ||
                    input.Digest.Length != 71 || !input.Digest.StartsWith("sha256:", StringComparison.Ordinal) ||
                    input.Digest[7..].Any(c => !char.IsAsciiHexDigitLower(c)))
                    throw new TadaException("Invalid captured build input.");
            CheckStable(project.Before, project.After);
            foreach (var reference in project.After.Where(input => input.Uses.Contains("ProjectReference")))
                if (!evidence.Projects.Any(candidate => candidate.Role == (reference.Category == "sut-reference" ? "sut" : project.Role) && candidate.After.Any(input =>
                    input.Uses.Contains("project") && string.Equals(input.Path, reference.Path, StringComparison.OrdinalIgnoreCase))))
                    throw new TadaException($"Referenced project has no captured build inputs: {reference.Path}");
            foreach (var report in project.Reports)
                if (CanonicalJson.FileDigest(new Repository(run).Resolve(report.Key)) != report.Value)
                    throw new TadaException($"Build-input report changed: {report.Key}");
            if (evidence.SchemaVersion == 2)
            {
                string? sutProject = evidence.Projects.FirstOrDefault(item => item.Role == "sut" && item.Project == inventory.Execution.SutProject)?
                    .After.Single(input => input.Uses.Contains("project")).Path;
                var serviceProjects = evidence.Projects.Where(item => item.Role == "sut" && inventory.Execution.Applications().Any(service => service.Project == item.Project))
                    .Select(item => item.After.Single(input => input.Uses.Contains("project")).Path).ToArray();
                var serviceIds = inventory.Execution.Services?.ToDictionary(service => evidence.Projects.Single(item => item.Role == "sut" && item.Project == service.Project)
                    .After.Single(input => input.Uses.Contains("project")).Path, service => service.Id, StringComparer.OrdinalIgnoreCase);
                if (project.Dependencies == null) throw new TadaException("Project dependency metadata is missing.");
                foreach (string report in project.Reports.Keys.Where(path => path.EndsWith(".json", StringComparison.Ordinal)))
                {
                    using var evaluation = JsonDocument.Parse(File.ReadAllBytes(new Repository(run).Resolve(report)));
                    if (evaluation.RootElement.GetProperty("Properties").GetProperty("TadaVerifiedBuild").GetString() != "true")
                        throw new TadaException("Build report does not identify a recorded TADA build.");
                    if (!ReadDependencies(new Repository(run).Resolve(report), project.Role, sutProject, serviceProjects, serviceIds).SequenceEqual(project.Dependencies))
                        throw new TadaException("Recorded project dependencies differ from the evaluated build reports.");
                }
                foreach (var dependency in project.Dependencies)
                    if (!project.After.Any(input => string.Equals(input.Path, dependency.Path, StringComparison.OrdinalIgnoreCase) &&
                        input.Uses.Contains("ProjectReference") && (input.Category == "sut-reference") == dependency.BuildOnlySut))
                        throw new TadaException("Project dependency has no matching captured input.");
                if (project.After.Where(input => input.Category == "sut-reference").Any(input =>
                    !input.Uses.SequenceEqual(["ProjectReference"]) || !project.Dependencies.Any(dependency => dependency.BuildOnlySut && dependency.Path == input.Path)))
                    throw new TadaException("SUT reference is used as a harness input.");
            }
        }
    }

    internal static string[] PackageDirectories(Repository repository, string project, string assets)
    {
        string lockPath = repository.Relative(Path.Combine(Path.GetDirectoryName(repository.Resolve(project))!, "packages.lock.json"));
        using var locked = JsonDocument.Parse(File.ReadAllBytes(repository.Resolve(lockPath)));
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var framework in locked.RootElement.GetProperty("dependencies").EnumerateObject())
            foreach (var package in framework.Value.EnumerateObject())
                if (package.Value.GetProperty("type").GetString() != "Project" && package.Value.TryGetProperty("resolved", out var version))
                    identities.Add(package.Name + "/" + version.GetString());
        using var restored = JsonDocument.Parse(File.ReadAllBytes(assets));
        foreach (var library in restored.RootElement.GetProperty("libraries").EnumerateObject())
            if (library.Value.GetProperty("type").GetString() == "package" && !identities.Contains(library.Name))
                throw new TadaException($"Restored package is not in the selected project's lock: {library.Name}");
        // NuGet SDK packages are resolved before project restore and are not in packages.lock.json.
        // Their exact version must be explicit in a reviewed project or reviewed global.json.
        var sdkNames = XDocument.Load(repository.Resolve(project)).Root!.Attributes("Sdk").Select(attribute => attribute.Value)
            .Concat(XDocument.Load(repository.Resolve(project)).Descendants("Sdk").Select(element =>
                (string?)element.Attribute("Name") + "/" + (string?)element.Attribute("Version")));
        foreach (string name in sdkNames.SelectMany(value => value.Split(';')))
            if (name.Split('/') is [string id, string version] && !string.IsNullOrWhiteSpace(id) && IsExactVersion(version)) identities.Add(id + "/" + version);
        using var global = JsonDocument.Parse(File.ReadAllBytes(repository.Resolve("global.json")));
        if (global.RootElement.TryGetProperty("msbuild-sdks", out var sdks))
            foreach (var sdk in sdks.EnumerateObject())
                if (IsExactVersion(sdk.Value.GetString() ?? "")) identities.Add(sdk.Name + "/" + sdk.Value.GetString());
        return restored.RootElement.GetProperty("packageFolders").EnumerateObject().SelectMany(folder => identities.Select(identity =>
            Path.GetFullPath(Path.Combine(folder.Name, identity.ToLowerInvariant().Replace('/', Path.DirectorySeparatorChar))))).ToArray();
    }

    private static bool IsExactVersion(string version) => version.Length > 0 && char.IsAsciiDigit(version[0]) &&
        version.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '+');

    private static void NoLinks(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current)) Repository.NoLink(current);
    }
}

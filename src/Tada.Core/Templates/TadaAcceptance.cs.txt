using System.Runtime.CompilerServices;

// Shared fixture setup for native MSTest/MTP execution and recorded TADA runs.
// Business bindings own their API processes, readiness checks and database cleanup.
public sealed record TadaAcceptance(string WorkingDirectory, string SutAssembly, string DotnetHost)
{
    public IReadOnlyDictionary<string, string> Services { get; init; } = new Dictionary<string, string> { ["sut"] = SutAssembly };
    private static readonly string NativeDirectory = Path.Combine(Path.GetTempPath(), "tada-tests", Guid.NewGuid().ToString("N"));

    [ModuleInitializer]
    public static void ConfigureNativeReports()
    {
        if (HasManagedContext()) return;
        Directory.CreateDirectory(NativeDirectory);
        // Configure before Reqnroll creates its formatter. Each test process owns its report.
        Environment.SetEnvironmentVariable("REQNROLL_FORMATTERS_MESSAGE", "outputFilePath=" + Path.Combine(NativeDirectory, "messages.ndjson"));
    }

    public static TadaAcceptance Create(string? testOutputDirectory = null)
    {
        string root;
        string assembly;
        string host;
        var services = new Dictionary<string, string>(StringComparer.Ordinal);
        if (HasManagedContext())
        {
            foreach (string name in new[] { "TADA_RUN_ID", "TADA_RUN_DIRECTORY", "TADA_DOTNET_HOST", "TADA_RUNTIME_DIRECTORY" })
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                    throw new InvalidOperationException($"Incomplete TADA run context. {name} is required. Start the approved baseline with tada run, or clear inherited TADA run settings for native testing.");
            root = Environment.GetEnvironmentVariable("TADA_RUN_DIRECTORY")!;
            string? serviceManifest = Environment.GetEnvironmentVariable("TADA_SERVICES_FILE");
            if (serviceManifest != null)
            {
                services = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(serviceManifest))
                    ?? throw new InvalidOperationException("Captured service metadata is empty.");
                if (services.Count == 0 || services.Values.Any(path => !Path.IsPathFullyQualified(path) || !File.Exists(path)))
                    throw new InvalidOperationException("Captured service paths must identify existing absolute assemblies.");
            }
            assembly = serviceManifest == null ? Environment.GetEnvironmentVariable("TADA_SUT_ASSEMBLY")
                ?? throw new InvalidOperationException("Incomplete TADA run context. TADA_SUT_ASSEMBLY is required.") : services.OrderBy(pair => pair.Key, StringComparer.Ordinal).First().Value;
            host = Environment.GetEnvironmentVariable("TADA_DOTNET_HOST")!;
            if (!Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(assembly) || !Path.IsPathFullyQualified(host) || !Directory.Exists(root) || !File.Exists(host))
                throw new InvalidOperationException("TADA run paths must identify the existing absolute run directory, API assembly and .NET host.");
        }
        else
        {
            root = NativeDirectory;
            string output = Path.GetFullPath(testOutputDirectory ?? AppContext.BaseDirectory);
            string manifest = Path.Combine(output, "tada-sut.txt");
            string serviceManifest = Path.Combine(output, "tada-services.txt");
            if (File.Exists(serviceManifest))
            {
                foreach (string line in File.ReadAllLines(serviceManifest))
                {
                    var parts = line.Split('|');
                    if (parts.Length != 2 || !System.Text.RegularExpressions.Regex.IsMatch(parts[0], "^[a-z][a-z0-9_-]*$") ||
                        Path.IsPathFullyQualified(parts[1]) || !services.TryAdd(parts[0], Path.GetFullPath(Path.Combine(output, parts[1]))))
                        throw new InvalidOperationException("Native service metadata has invalid or duplicate service identities.");
                }
                if (services.Count == 0 || services.Values.Any(path => !path.StartsWith(Path.Combine(output, "sut") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Native service metadata must select assemblies inside the acceptance output's sut directory.");
                assembly = services.OrderBy(pair => pair.Key, StringComparer.Ordinal).First().Value;
            }
            else
            {
            if (!File.Exists(manifest))
                throw new InvalidOperationException("Native API output is missing. Add a build-only API ProjectReference with ReferenceOutputAssembly=false, Private=false and OutputItemType=TadaSutOutput, import Tada.Acceptance.targets, and build the acceptance project.");
            string relative = File.ReadAllText(manifest).Trim();
            assembly = Path.GetFullPath(Path.Combine(output, relative));
            string sut = Path.Combine(output, "sut") + Path.DirectorySeparatorChar;
            if (Path.IsPathFullyQualified(relative) || !assembly.StartsWith(sut, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Native API metadata must select an assembly inside the acceptance output's sut directory.");
            }
            host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        }
        if (!File.Exists(assembly)) throw new InvalidOperationException($"API assembly is missing at {assembly}. Rebuild the acceptance project in the selected configuration.");
        if (services.Count == 0) services.Add("sut", assembly);
        if (services.Values.Any(path => !File.Exists(path))) throw new InvalidOperationException("A staged service assembly is missing. Rebuild the acceptance project.");
        string directory = Path.Combine(root, "scenario-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Console.WriteLine("Acceptance diagnostics directory: " + directory);
        return new TadaAcceptance(directory, assembly, host) { Services = services };
    }

    private static bool HasManagedContext() => new[] { "TADA_RUN_ID", "TADA_RUN_DIRECTORY", "TADA_SUT_ASSEMBLY", "TADA_RUNTIME_DIRECTORY", "TADA_SERVICES_FILE" }
        .Any(name => Environment.GetEnvironmentVariable(name) != null);
}

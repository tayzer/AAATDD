using System.Runtime.CompilerServices;

// Shared fixture setup for native MSTest/MTP execution and recorded TADA runs.
// Business bindings own their API processes, readiness checks and database cleanup.
public sealed record TadaAcceptance(string WorkingDirectory, string SutAssembly, string DotnetHost)
{
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
        if (HasManagedContext())
        {
            foreach (string name in new[] { "TADA_RUN_ID", "TADA_RUN_DIRECTORY", "TADA_SUT_ASSEMBLY", "TADA_DOTNET_HOST", "TADA_RUNTIME_DIRECTORY" })
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                    throw new InvalidOperationException($"Incomplete TADA run context. {name} is required. Start the approved baseline with tada run, or clear inherited TADA run settings for native testing.");
            root = Environment.GetEnvironmentVariable("TADA_RUN_DIRECTORY")!;
            assembly = Environment.GetEnvironmentVariable("TADA_SUT_ASSEMBLY")!;
            host = Environment.GetEnvironmentVariable("TADA_DOTNET_HOST")!;
            if (!Path.IsPathFullyQualified(root) || !Path.IsPathFullyQualified(assembly) || !Path.IsPathFullyQualified(host) || !Directory.Exists(root) || !File.Exists(host))
                throw new InvalidOperationException("TADA run paths must identify the existing absolute run directory, API assembly and .NET host.");
        }
        else
        {
            root = NativeDirectory;
            string output = Path.GetFullPath(testOutputDirectory ?? AppContext.BaseDirectory);
            string manifest = Path.Combine(output, "tada-sut.txt");
            if (!File.Exists(manifest))
                throw new InvalidOperationException("Native API output is missing. Add a build-only API ProjectReference with ReferenceOutputAssembly=false, Private=false and OutputItemType=TadaSutOutput, import Tada.Acceptance.targets, and build the acceptance project.");
            string relative = File.ReadAllText(manifest).Trim();
            assembly = Path.GetFullPath(Path.Combine(output, relative));
            string sut = Path.Combine(output, "sut") + Path.DirectorySeparatorChar;
            if (Path.IsPathFullyQualified(relative) || !assembly.StartsWith(sut, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Native API metadata must select an assembly inside the acceptance output's sut directory.");
            host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        }
        if (!File.Exists(assembly)) throw new InvalidOperationException($"API assembly is missing at {assembly}. Rebuild the acceptance project in the selected configuration.");
        string directory = Path.Combine(root, "scenario-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Console.WriteLine("Acceptance diagnostics directory: " + directory);
        return new TadaAcceptance(directory, assembly, host);
    }

    private static bool HasManagedContext() => new[] { "TADA_RUN_ID", "TADA_RUN_DIRECTORY", "TADA_SUT_ASSEMBLY", "TADA_RUNTIME_DIRECTORY" }
        .Any(name => Environment.GetEnvironmentVariable(name) != null);
}

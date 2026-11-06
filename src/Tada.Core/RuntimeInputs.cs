using System.Text.Json;
using Tada.RuntimeProbe;

namespace Tada;

public sealed record RuntimeInputEvidence(int SchemaVersion, string Policy, string SystemDirectory,
    Dictionary<string, string> PlatformInputs, Dictionary<string, string> Reports);
public sealed record LoadedInput(int ProcessId, string Kind, string Path, string Digest, string Category);

public static class RuntimeInputs
{
    public const string Policy = Observer.Policy;
    public const string FileName = "runtime-inputs.json";
    public const string EnvironmentFile = "execution-environment.json";
    public const string ProbePath = "observer/Tada.RuntimeProbe.dll";
    public const string NativeProbePath = "observer/" + NativeLoader.FileName;

    public static string InstallProbe(string run)
    {
        string path = Path.Combine(run, ProbePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Copy(typeof(Observer).Assembly.Location, path, overwrite: false);
        File.Copy(Path.Combine(Path.GetDirectoryName(typeof(Observer).Assembly.Location)!, NativeLoader.FileName), Path.Combine(run, NativeProbePath), overwrite: false);
        Directory.CreateDirectory(Path.Combine(run, "runtime"));
        return path;
    }

    public static Dictionary<string, string> CapturePlatform(string sdk)
    {
        string host = Path.GetDirectoryName(Path.GetDirectoryName(sdk))!;
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(Path.Combine(host, "dotnet.exe"));
        Visit(Path.Combine(host, "host", "fxr"));
        foreach (string framework in new[] { "Microsoft.NETCore.App", "Microsoft.AspNetCore.App" })
        {
            string root = Path.Combine(host, "shared", framework);
            if (!Directory.Exists(root)) continue;
            foreach (string version in Directory.EnumerateDirectories(root))
                if (Version.TryParse(Path.GetFileName(version), out var parsed) && parsed.Major == 10) Visit(version);
        }
        foreach (string name in new[] { "kernel32.dll", "KernelBase.dll", "ntdll.dll" })
            Add(Path.Combine(Environment.SystemDirectory, name));
        return inputs;

        void Visit(string directory)
        {
            Repository.NoLink(directory);
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                Repository.NoLink(path);
                if (Directory.Exists(path)) Visit(path);
                else Add(path);
            }
        }
        void Add(string path)
        {
            NoLinks(path);
            inputs.Add(Path.GetFullPath(path), CanonicalJson.FileDigest(path));
        }
    }

    public static RuntimeInputEvidence Capture(string run, Dictionary<string, string> platform)
    {
        var reports = Reports(run).ToDictionary(path => Path.GetRelativePath(run, path).Replace('\\', '/'), CanonicalJson.FileDigest);
        return new RuntimeInputEvidence(2, Policy, Environment.SystemDirectory, platform, reports);
    }

    public static LoadedInput[] Verify(string run, Inventory inventory, RunRecord record, bool checkCurrent = false)
    {
        if (inventory.SchemaVersion != 6 || inventory.Policy.RuntimeInputs != Policy)
            throw new TadaException("Baseline has no supported runtime-observation policy. Prepare, review and approve a new revision.");
        var repository = new Repository(run);
        string evidenceFile = repository.Resolve(FileName);
        if (record.RuntimeInputsDigest == null || CanonicalJson.FileDigest(evidenceFile) != record.RuntimeInputsDigest)
            throw new TadaException("Runtime-input evidence is missing or changed.");
        string environment = repository.Resolve(EnvironmentFile);
        if (record.ExecutionEnvironmentDigest == null || CanonicalJson.FileDigest(environment) != record.ExecutionEnvironmentDigest)
            throw new TadaException("Execution-environment evidence is missing or changed.");
        var evidence = Data.Read<RuntimeInputEvidence>(evidenceFile);
        evidence = evidence with { PlatformInputs = new Dictionary<string, string>(evidence.PlatformInputs, StringComparer.OrdinalIgnoreCase) };
        if (evidence.SchemaVersion != 2 || evidence.Policy != Policy || evidence.Reports.Count == 0 || evidence.PlatformInputs.Count == 0 ||
            !Path.IsPathFullyQualified(evidence.SystemDirectory)) throw new TadaException("Unsupported or incomplete runtime-input evidence.");
        if (!record.Artifacts.TryGetValue(ProbePath, out string? probeDigest)) throw new TadaException("The runtime observer has no captured artifact identity.");
        if (!record.Artifacts.TryGetValue(NativeProbePath, out string? nativeProbeDigest)) throw new TadaException("The native loader observer has no captured artifact identity.");
        var artifactIdentities = new Dictionary<string, string>(record.Artifacts, StringComparer.OrdinalIgnoreCase);
        var actualReports = Reports(run).Select(path => Path.GetRelativePath(run, path).Replace('\\', '/')).Order(StringComparer.Ordinal);
        if (!actualReports.SequenceEqual(evidence.Reports.Keys.Order(StringComparer.Ordinal)))
            throw new TadaException("Runtime observation report set changed.");
        string harness = Path.GetFullPath(Path.Combine(run, "harness", inventory.Execution.Assembly));
        string? sut = inventory.Execution.SutAssembly == null ? null : Path.GetFullPath(Path.Combine(run, "sut", inventory.Execution.SutAssembly));
        var result = new List<LoadedInput>();
        var pids = new HashSet<int>();
        bool sawHarness = false, sawSut = sut == null;
        foreach (var report in evidence.Reports)
        {
            string path = repository.Resolve(report.Key);
            if (CanonicalJson.FileDigest(path) != report.Value) throw new TadaException($"Runtime observation changed: {report.Key}");
            Observation? first = null, last = null;
            long sequence = 0;
            bool sawEntry = false, sawProbe = false;
            bool loaderStarted = false, loaderStopped = false;
            int nativeLoads = 0;
            foreach (string line in File.ReadLines(path))
            {
                using var document = JsonDocument.Parse(line);
                CanonicalJson.Encode(document.RootElement);
                var item = JsonSerializer.Deserialize<Observation>(line, Data.Json) ?? throw new TadaException("Empty runtime observation.");
                if (item.SchemaVersion != 1 || item.RunId != record.Id || item.ProcessId <= 0 || item.ProcessStartedTicks <= 0 ||
                    item.Sequence != ++sequence || item.Time < record.Started || item.Time > record.Finished)
                    throw new TadaException("Runtime observation identity, sequence or timestamp mismatch.");
                if (first == null)
                {
                    if (item.Kind != "start" || item.Identity != Policy || !pids.Add(item.ProcessId))
                        throw new TadaException("Runtime process has no unique supported startup record.");
                    first = item;
                    if (Same(item.Path, harness)) sawHarness = true;
                    else if (sut != null && Same(item.Path, sut)) sawSut = true;
                    else throw new TadaException($"Unconfigured managed child process: {item.Path}");
                }
                else if (item.ProcessId != first.ProcessId || item.ProcessStartedTicks != first.ProcessStartedTicks ||
                    item.Kind == "start" || item.Time < last!.Time || last.Kind is "exit" or "checkpoint")
                    throw new TadaException("Runtime process stream is inconsistent or extends past its terminal snapshot.");
                if (item.Kind == "native-loader-start")
                {
                    if (loaderStarted || !Same(item.Path, Path.Combine(run, NativeProbePath)) || item.Digest != nativeProbeDigest ||
                        item.Identity != NativeLoader.Protocol.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        throw new TadaException("Native loader observer startup identity is missing or inconsistent.");
                    loaderStarted = true;
                }
                else if (item.Kind == "native-loader-stop")
                {
                    if (!loaderStarted || loaderStopped || item.Identity != nativeLoads.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        throw new TadaException("Native loader event count is incomplete or inconsistent.");
                    loaderStopped = true;
                }
                else if (item.Kind is "managed" or "native" or "native-loaded")
                {
                    if (item.Kind == "native-loaded" && (!loaderStarted || loaderStopped ||
                        item.Identity != (++nativeLoads).ToString(System.Globalization.CultureInfo.InvariantCulture)))
                        throw new TadaException("Native loader event sequence is incomplete or outside its observation interval.");
                    if (!Path.IsPathFullyQualified(item.Path) || !Digest(item.Digest)) throw new TadaException("Runtime module lacks a full path and byte identity.");
                    string category = Classify(item, first, run, evidence, artifactIdentities);
                    if (Same(item.Path, first.Path) && item.Kind == "managed") sawEntry = true;
                    if (Same(item.Path, Path.Combine(run, ProbePath)) && item.Digest == probeDigest && item.Kind == "managed") sawProbe = true;
                    if (checkCurrent)
                    {
                        NoLinks(item.Path);
                        if (CanonicalJson.FileDigest(item.Path) != item.Digest) throw new TadaException($"Loaded runtime input changed: {item.Path}");
                    }
                    result.Add(new LoadedInput(item.ProcessId, item.Kind, item.Path, item.Digest, category));
                }
                else if (item.Kind == "dynamic-managed" && item.Identity.StartsWith("Anonymously Hosted DynamicMethods Assembly,", StringComparison.Ordinal))
                    result.Add(new LoadedInput(item.ProcessId, item.Kind, item.Path, "", "clr-generated"));
                else if (item.Kind is not ("start" or "exit" or "checkpoint"))
                    throw new TadaException($"Unverifiable runtime input: {item.Kind}: {item.Identity}");
                last = item;
            }
            if (last == null || first == null || !sawEntry || !sawProbe || !loaderStarted || !loaderStopped || last.Kind is not ("exit" or "checkpoint") || last.Identity != "complete" ||
                (Same(first.Path, harness) && last.Kind != "exit"))
                throw new TadaException("Runtime observation lacks entry/observer identities or a complete final snapshot. Checkpoint owned APIs before termination.");
        }
        if (!sawHarness || !sawSut) throw new TadaException("Runtime observation does not cover the configured harness and SUT.");
        if (checkCurrent)
            foreach (var platform in evidence.PlatformInputs)
            {
                NoLinks(platform.Key);
                if (CanonicalJson.FileDigest(platform.Key) != platform.Value) throw new TadaException($"Runtime platform changed during execution: {platform.Key}");
            }
        return result.ToArray();

        string Classify(Observation item, Observation process, string directory, RuntimeInputEvidence captured, Dictionary<string, string> artifacts)
        {
            if (BuildInputs.Within(item.Path, directory))
            {
                string relative = Path.GetRelativePath(directory, item.Path).Replace('\\', '/');
                if (!artifacts.TryGetValue(relative, out string? expected) || expected != item.Digest)
                    throw new TadaException($"Loaded module was not captured in build outputs: {relative}");
                if (Same(process.Path, harness) && relative.StartsWith("sut/", StringComparison.OrdinalIgnoreCase))
                    throw new TadaException("Acceptance harness loaded an implementation assembly into its own process.");
                if (!Same(process.Path, harness) && relative.StartsWith("harness/", StringComparison.OrdinalIgnoreCase))
                    throw new TadaException("SUT loaded an acceptance harness assembly into its own process.");
                return relative.StartsWith("observer/", StringComparison.Ordinal) ? "observer" : "build-output";
            }
            if (captured.PlatformInputs.TryGetValue(item.Path, out string? platformDigest))
            {
                if (platformDigest != item.Digest) throw new TadaException($"Loaded platform bytes differ from the pre-run snapshot: {item.Path}");
                return "platform";
            }
            if (item.Kind is "native" or "native-loaded" && (BuildInputs.Within(item.Path, captured.SystemDirectory) ||
                BuildInputs.Within(item.Path, Path.Combine(Path.GetDirectoryName(captured.SystemDirectory)!, "WinSxS")))) return "windows-system";
            throw new TadaException($"Unidentified loaded runtime input: {item.Path}. Use captured outputs or the supported platform policy.");
        }
    }

    private static string[] Reports(string run)
    {
        string directory = new Repository(run).Resolve("runtime", directory: true);
        return Directory.EnumerateFiles(directory, "*.ndjson").Order(StringComparer.Ordinal).ToArray();
    }

    private static bool Same(string left, string right) => string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    private static bool Digest(string value) => value.Length == 71 && value.StartsWith("sha256:", StringComparison.Ordinal) && value[7..].All(char.IsAsciiHexDigitLower);
    private static void NoLinks(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current)) Repository.NoLink(current);
    }
}

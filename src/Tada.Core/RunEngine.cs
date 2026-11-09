using System.Text.Json;

namespace Tada;

public sealed class RunEngine(BaselineStore store, string dotnet = "dotnet")
{
    public async Task<Verification> Run(string baseline, CancellationToken cancellation = default)
    {
        string id = Guid.NewGuid().ToString("N");
        string run = RunDirectory(id);
        Directory.CreateDirectory(run);
        var commands = new List<CommandEvidence>();
        var gaps = new List<Reason>();
        var artifacts = new Dictionary<string, string>();
        string implementation = "unestablished", environment = "unestablished", approvalId = "";
        string messagesDigest = "";
        string? buildInputsDigest = null;
        string? runtimeInputsDigest = null;
        string? executionEnvironmentDigest = null;
        string? taskInputsDigest = null;
        string taskLogger = "";
        var taskReports = new List<TaskReport>();
        string sdk = "";
        string host = dotnet;
        var executionEnvironment = ExecutionEnvironment.Capture();
        string temporary = Path.Combine(run, "build", "temp");
        Directory.CreateDirectory(temporary);
        executionEnvironment["TEMP"] = temporary;
        executionEnvironment["TMP"] = temporary;
        var builds = new List<ProjectBuildInputs>();
        DateTimeOffset began = DateTimeOffset.UtcNow;
        Data.WriteNew(Path.Combine(run, "intent.json"), new RunIntent(id, baseline, began));
        Inventory? inventory = null;
        try
        {
            inventory = store.Read(baseline);
            if (inventory.SchemaVersion != 6 || !BuildInputs.Supported(inventory.Policy.BuildInputs) || inventory.Policy.RuntimeInputs != RuntimeInputs.Policy || inventory.Policy.BuildTasks != TaskJournal.Policy)
                throw new TadaException("Baseline predates the evaluated-build-input policy. Prepare, review and approve a new revision.");
            if (!OperatingSystem.IsWindows() || System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture != System.Runtime.InteropServices.Architecture.X64)
                throw new TadaException("This development profile supports Windows x64 only.");
            var approval = store.Approved(baseline);
            approvalId = approval.Id;
            store.CheckCurrent(baseline);
            var config = Data.Read<Config>(store.Repository.Resolve("tada.json"));
            TaskJournal.CheckEvaluation(store.Repository, config, store.Repository.CapturePaths(config).Concat(
                store.Repository.CapturePaths(config with { OracleRoots = config.ImplementationRoots ?? [] })));
            var implementationBefore = Implementation(config);
            string environmentFile = Path.Combine(run, RuntimeInputs.EnvironmentFile);
            Data.WriteNew(environmentFile, executionEnvironment);
            executionEnvironmentDigest = CanonicalJson.FileDigest(environmentFile);
            var version = await Command(["--version"]);
            using var sdkDocument = JsonDocument.Parse(File.ReadAllBytes(store.Repository.Resolve("global.json")));
            string expectedSdk = sdkDocument.RootElement.GetProperty("sdk").GetProperty("version").GetString()!;
            if (version.ExitCode != 0 || version.Stdout.Trim() != expectedSdk)
                throw new TadaException("Actual dotnet SDK does not match the reviewed SDK pin.");
            var installed = await Command(["--list-sdks"]);
            if (installed.ExitCode != 0 || installed.TimedOut) throw new TadaException("Cannot identify installed SDKs.");
            sdk = BuildInputs.FindSdk(installed.Stdout, expectedSdk);
            host = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(sdk))!, "dotnet.exe");
            taskLogger = TaskJournal.Install(run);
            var info = await Command(["--info"]);
            environment = CanonicalJson.Identity(new { sdk = version.Stdout.Trim(), info = info.Stdout, os = System.Runtime.InteropServices.RuntimeInformation.OSDescription });
            var execution = inventory.Execution;
            if (execution.SutProject != null)
            {
                store.Repository.Resolve(execution.SutProject);
                await Build(execution.SutProject, "sut");
            }
            await Build(execution.Project, "harness");
            var taskInputs = taskReports.SelectMany(report => TaskJournal.Audit(store.Repository, inventory, config, sdk, run, report, builds)).ToArray();
            string taskFile = Path.Combine(run, TaskJournal.FileName);
            Data.WriteNew(taskFile, new TaskInputEvidence(1, TaskJournal.Policy, store.Repository.Root, sdk, taskReports.ToArray(), taskInputs));
            taskInputsDigest = CanonicalJson.FileDigest(taskFile);
            BuildInputs.CheckCurrent(taskInputs.Where(input => input.Category != "generated"));
            string buildInputsFile = Path.Combine(run, BuildInputs.FileName);
            Data.WriteNew(buildInputsFile, new BuildInputEvidence(inventory.Policy.BuildInputs == BuildInputs.Policy ? 2 : 1, inventory.Policy.BuildInputs!, sdk, builds.ToArray()));
            buildInputsDigest = CanonicalJson.FileDigest(buildInputsFile);
            BuildInputs.Verify(run, inventory, buildInputsDigest);
            BuildInputs.CheckCurrent(builds.SelectMany(build => build.After));
            var platform = RuntimeInputs.CapturePlatform(sdk);
            environment = CanonicalJson.Identity(new
            {
                host = environment,
                executionEnvironment = executionEnvironment.ToDictionary(pair => pair.Key, pair => pair.Value.Replace(run, "<run>", StringComparison.OrdinalIgnoreCase)),
                platform,
                tools = ExecutionEnvironment.Tools(builds.SelectMany(build => build.After).Concat(taskInputs))
            });
            store.CheckCurrent(baseline);
            string probe = RuntimeInputs.InstallProbe(run);
            artifacts = CollectArtifacts(run, execution.MessagesFile, beforeRun: true);
            // Compare identified implementation source across reruns. Per-run executable hashes
            // remain protected in Artifacts; fresh output paths must not erase an earlier failure.
            implementation = implementationBefore;
            string harness = Path.Combine(run, "harness");
            string assembly = Path.Combine(harness, execution.Assembly);
            if (!File.Exists(assembly)) throw new TadaException("Configured harness assembly was not built.");
            var variables = new Dictionary<string, string>(executionEnvironment, StringComparer.OrdinalIgnoreCase)
            {
                ["TADA_REPOSITORY"] = store.Repository.Root,
                ["TADA_RUN_DIRECTORY"] = run,
                ["TADA_RUN_ID"] = id,
                ["TADA_DOTNET_HOST"] = host,
                ["TADA_RUNTIME_DIRECTORY"] = Path.Combine(run, "runtime"),
                ["DOTNET_STARTUP_HOOKS"] = probe
            };
            if (execution.SutAssembly != null)
            {
                if (execution.SutAssembly != Path.GetFileName(execution.SutAssembly)) throw new TadaException("SUT assembly must be a filename.");
                string sut = Path.Combine(run, "sut", execution.SutAssembly);
                if (!File.Exists(sut)) throw new TadaException("SUT assembly was not built.");
                variables["TADA_SUT_ASSEMBLY"] = sut;
            }
            var result = await Processes.Execute(host, [assembly, "--progress", "off", "--no-ansi"], harness,
                execution.TimeoutSeconds, variables, cancellation, replaceEnvironment: true);
            commands.Add(result);
            if (result.Cancelled) gaps.Add(new Reason("cancelled", "Execution cancelled; owned process tree was terminated."));
            if (result.TimedOut) gaps.Add(new Reason("timeout", "Harness deadline expired; owned process tree was terminated."));
            if (implementationBefore != Implementation(config)) gaps.Add(new Reason("implementation-drift", "Implementation source changed during execution."));
            store.CheckCurrent(baseline);
            BuildInputs.CheckCurrent(builds.SelectMany(build => build.After));
            BuildInputs.CheckCurrent(taskInputs.Where(input => input.Category != "generated"));
            var after = CollectArtifacts(run, execution.MessagesFile);
            if (!artifacts.OrderBy(p => p.Key).SequenceEqual(after.OrderBy(p => p.Key)))
                gaps.Add(new Reason("binary-drift", "Generated binaries/configuration changed after capture."));
            string messages = Path.Combine(harness, execution.MessagesFile);
            if (File.Exists(messages)) messagesDigest = CanonicalJson.FileDigest(messages);
            else gaps.Add(new Reason("missing-messages", "Harness did not produce native Cucumber Messages."));
            string runtimeFile = Path.Combine(run, RuntimeInputs.FileName);
            Data.WriteNew(runtimeFile, RuntimeInputs.Capture(run, platform));
            runtimeInputsDigest = CanonicalJson.FileDigest(runtimeFile);
            var provisional = new RunRecord(id, baseline, approvalId, began, DateTimeOffset.UtcNow, implementation,
                environment, messagesDigest, artifacts, commands.ToArray(), gaps.ToArray(), buildInputsDigest, runtimeInputsDigest, executionEnvironmentDigest, taskInputsDigest);
            RuntimeInputs.Verify(run, inventory, provisional, checkCurrent: true);
        }
        catch (Exception error) when (error is TadaException or IOException or JsonException or InvalidOperationException or
            ArgumentException or KeyNotFoundException or System.Xml.XmlException or UnauthorizedAccessException or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            gaps.Add(new Reason("execution-invalid", error.Message));
        }
        var record = new RunRecord(id, baseline, approvalId, began, DateTimeOffset.UtcNow, implementation,
            environment, messagesDigest, artifacts, commands.ToArray(), gaps.ToArray(), buildInputsDigest, runtimeInputsDigest, executionEnvironmentDigest, taskInputsDigest);
        Data.WriteNew(Path.Combine(run, "run.json"), record);
        Verification verification;
        if (gaps.Count > 0 || inventory == null)
            verification = new Verification("INDETERMINATE", "L1-advisory", baseline, id, [], gaps.ToArray());
        else verification = Verify(id);
        Data.WriteNew(Path.Combine(run, "result.json"), verification);
        return verification;

        async Task<CommandEvidence> Command(string[] args)
        {
            var command = await Processes.Execute(host, args, store.Repository.Root, 120, executionEnvironment, cancellation, replaceEnvironment: true);
            commands.Add(command);
            if (command.Cancelled) throw new OperationCanceledException("Execution cancelled; owned process tree was terminated.", cancellation);
            return command;
        }
        async Task<CommandEvidence> TaskCommand(string[] args, string role, string operation)
        {
            string path = Path.Combine(run, "build", Guid.NewGuid().ToString("N") + ".tasks.ndjson");
            var command = await Command([.. args, "-verbosity:diagnostic", "-noconsolelogger", "-logger:" + taskLogger + ";" + path]);
            if (File.Exists(path)) taskReports.Add(new TaskReport(Path.GetRelativePath(run, path).Replace('\\', '/'), CanonicalJson.FileDigest(path), role, operation, command.Started, command.Finished));
            else throw new TadaException("MSBuild did not produce the required task journal.");
            if (command.ExitCode != 0 || command.TimedOut)
            {
                var errors = File.ReadLines(path).Select(line => JsonSerializer.Deserialize<Tada.BuildLogger.TaskEvent>(line, Data.Json)!)
                    .Where(item => item.Kind == "error").Take(3).Select(item => item.Name + ": " + string.Join(" ", item.Values.Select(value => value.Value)));
                throw new TadaException($"MSBuild {operation} failed; see retained command/task evidence. " + string.Join(" | ", errors));
            }
            return command;
        }
        async Task Build(string project, string subdirectory)
        {
            string generated = Path.Combine(run, "build", subdirectory);
            string output = Path.Combine(run, subdirectory);
            Directory.CreateDirectory(generated);
            string[] properties = ["-p:UseArtifactsOutput=true", "-p:ArtifactsPath=" + generated, "-p:Configuration=Release",
                "-p:TadaVerifiedBuild=true",
                "-p:OutputPath=" + output + Path.DirectorySeparatorChar, "-p:UseSharedCompilation=false",
                "-p:PathMap=" + run + "=/_tada_run"];
            var restore = await TaskCommand(["restore", project, "--locked-mode", "--configfile", "NuGet.Config", "-p:NuGetAudit=false", "--disable-build-servers", "-m:1", "-noAutoResponse", .. properties], subdirectory, "restore");
            if (restore.ExitCode != 0 || restore.TimedOut) throw new TadaException($"Locked restore failed for {project}; see retained command output.");
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var finished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await BuildProject(project);

            async Task BuildProject(string selected)
            {
                string full = store.Repository.Resolve(selected);
                if (finished.Contains(full)) return;
                if (!visiting.Add(full)) throw new TadaException("Cyclic ProjectReference graph is unsupported.");
                if (!projectNames.Add(Path.GetFileNameWithoutExtension(full))) throw new TadaException("Build projects require distinct filenames for isolated outputs.");
                string prefix = Path.Combine(generated, Guid.NewGuid().ToString("N"));
                string beforeJson = prefix + "-before.json", beforeXml = prefix + "-before.xml";
                string afterJson = prefix + "-after.json", afterXml = prefix + "-after.xml";
                await Msbuild(["-getItem:" + BuildInputs.Items, "-getProperty:" + BuildInputs.Properties, "-getResultOutputFile:" + beforeJson]);
                await Msbuild(["-preprocess:" + beforeXml]);
                var config = Data.Read<Config>(store.Repository.Resolve("tada.json"));
                var before = BuildInputs.Capture(store.Repository, inventory!, config, sdk, generated, output, subdirectory, selected, beforeJson, beforeXml);
                ProjectDependency[]? dependencies = inventory!.Policy.BuildInputs == BuildInputs.Policy
                    ? BuildInputs.ReadDependencies(beforeJson, subdirectory, config.Execution.SutProject == null ? null : store.Repository.Resolve(config.Execution.SutProject)) : null;
                using (var evaluation = JsonDocument.Parse(File.ReadAllBytes(beforeJson)))
                    foreach (var reference in evaluation.RootElement.GetProperty("Items").GetProperty("ProjectReference").EnumerateArray())
                    {
                        string path = reference.GetProperty("FullPath").GetString()!;
                        if (dependencies?.Any(item => item.BuildOnlySut && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)) == true)
                        {
                            if (!builds.Any(build => build.Role == "sut" && build.Project == store.Repository.Relative(path)))
                                throw new TadaException("Build-only API dependency has no prior captured SUT build.");
                            continue;
                        }
                        await BuildProject(store.Repository.Relative(path));
                    }
                // Each project is rebuilt once, in dependency order, with its own fresh SDK obj directory.
                await Msbuild(["-t:Rebuild", "-p:BuildProjectReferences=false", "-getItem:" + BuildInputs.Items,
                    "-getProperty:" + BuildInputs.Properties, "-getResultOutputFile:" + afterJson]);
                await Msbuild(["-preprocess:" + afterXml]);
                var after = BuildInputs.Capture(store.Repository, inventory!, config, sdk, generated, output, subdirectory, selected, afterJson, afterXml);
                BuildInputs.CheckStable(before, after);
                if (dependencies != null && !BuildInputs.ReadDependencies(afterJson, subdirectory,
                    config.Execution.SutProject == null ? null : store.Repository.Resolve(config.Execution.SutProject)).SequenceEqual(dependencies))
                    throw new TadaException("Project reference metadata changed during build.");
                var reports = new[] { beforeJson, beforeXml, afterJson, afterXml }.ToDictionary(
                    path => Path.GetRelativePath(run, path).Replace('\\', '/'), CanonicalJson.FileDigest);
                builds.Add(new ProjectBuildInputs(selected, subdirectory, before, after, reports, dependencies));
                visiting.Remove(full);
                finished.Add(full);

                async Task Msbuild(string[] args)
                {
                    string[] arguments = ["msbuild", selected, "-m:1", "-nodeReuse:false", "-noAutoResponse", .. properties, .. args];
                    var result = args.Any(argument => argument.StartsWith("-t:", StringComparison.Ordinal))
                        ? await TaskCommand(arguments, subdirectory, "build") : await Command(arguments);
                    if (result.ExitCode != 0 || result.TimedOut) throw new TadaException($"Build/input evaluation failed for {selected}; see retained command output.");
                }
            }
        }
    }

    public Verification Verify(string id)
    {
        var verification = VerifySingle(id, out var record);
        return verification.Outcome == "PASS" && record != null ? PreserveHistory(verification, record) : verification;
    }

    private Verification VerifySingle(string id, out RunRecord? record)
    {
        string directory = RunDirectory(id);
        record = null;
        try
        {
            record = Data.Read<RunRecord>(Path.Combine(directory, "run.json"));
            if (record.Id != id) throw new TadaException("Run record identity mismatch.");
            var inventory = store.Read(record.Baseline);
            var approval = Data.Read<Approval>(store.RecordPath("approvals", record.Approval));
            var review = Data.Read<Review>(store.RecordPath("reviews", approval.Review));
            if (approval.Id != record.Approval || review.Id != approval.Review || approval.Baseline != record.Baseline || review.Baseline != record.Baseline || approval.Created > record.Started || review.Created > approval.Created)
                throw new TadaException("Run does not reference prior approval and review of the assessed baseline.");
            if (record.IntegrityGaps.Length > 0)
                return new Verification("INDETERMINATE", "L1-advisory", record.Baseline, id, [], record.IntegrityGaps);
            BuildInputs.Verify(directory, inventory, record.BuildInputsDigest);
            TaskJournal.Verify(directory, inventory, Data.Read<Config>(new Repository(store.Snapshot(record.Baseline)).Resolve("tada.json")), record);
            RuntimeInputs.Verify(directory, inventory, record);
            if (!record.Artifacts.ContainsKey("harness/" + inventory.Execution.Assembly))
                throw new TadaException("Run has no identified harness executable.");
            var actualArtifacts = CollectArtifacts(directory, inventory.Execution.MessagesFile);
            if (!record.Artifacts.OrderBy(p => p.Key).SequenceEqual(actualArtifacts.OrderBy(p => p.Key)))
                throw new TadaException("Run artifact inventory differs from its captured identities.");
            foreach (var pair in record.Artifacts)
            {
                string path = new Repository(directory).Resolve(pair.Key);
                if (CanonicalJson.FileDigest(path) != pair.Value) throw new TadaException($"Run artifact changed: {pair.Key}");
            }
            string messages = Path.Combine(directory, "harness", inventory.Execution.MessagesFile);
            if (string.IsNullOrEmpty(record.MessagesDigest) || CanonicalJson.FileDigest(messages) != record.MessagesDigest)
                throw new TadaException("Native Messages bytes changed or were not captured.");
            var command = record.Commands.LastOrDefault() ?? throw new TadaException("No harness execution record.");
            if (command.Started < record.Started || command.Finished > record.Finished || command.Finished < command.Started)
                throw new TadaException("Command timestamps do not belong to this run.");
            if (command.Cancelled) throw new TadaException("Harness execution was cancelled.");
            if (command.TimedOut) throw new TadaException("Harness did not finish within its deadline.");
            return MessagesVerifier.Verify(inventory, record.Baseline, id, messages,
                new Repository(store.Snapshot(record.Baseline)), command);
        }
        catch (Exception error) when (error is TadaException or IOException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
        { return new Verification("INDETERMINATE", "L1-advisory", record?.Baseline ?? "unestablished", id, [], [new Reason("integrity", error.Message)]); }
    }

    private Verification PreserveHistory(Verification current, RunRecord record)
    {
        var failures = new List<Reason>();
        var gaps = new List<Reason>();
        foreach (string directory in Directory.EnumerateDirectories(Path.Combine(store.Repository.State, "runs")))
        {
            string id = Path.GetFileName(directory);
            if (id == record.Id) continue;
            try
            {
                Repository.NoLink(directory);
                string intentFile = Path.Combine(directory, "intent.json");
                if (File.Exists(intentFile))
                {
                    var intent = Data.Read<RunIntent>(intentFile);
                    if (intent.Id != id) throw new TadaException("Earlier run intent identity mismatch.");
                    if (intent.Baseline != record.Baseline || intent.Started >= record.Started) continue;
                }
                string metadata = Path.Combine(directory, "run.json");
                if (!File.Exists(metadata)) throw new TadaException("Earlier run has no terminal record; its outcome is unknown.");
                var previous = Data.Read<RunRecord>(metadata);
                if (previous.Id != id) throw new TadaException("Earlier run identity mismatch.");
                if (previous.Started >= record.Started || previous.Baseline != record.Baseline ||
                    previous.Implementation != record.Implementation || previous.Environment != record.Environment) continue;
                // Re-evaluate native evidence. Cached result.json is a projection, not the authority.
                var result = VerifySingle(id, out _);
                if (result.Outcome == "FAIL")
                {
                    failures.Add(new Reason("unresolved-earlier-failure", $"Passing rerun cannot discard failure from run {id}."));
                    failures.AddRange(result.Reasons);
                }
                else if (result.Reasons.Any(r => r.Code == "integrity"))
                    gaps.Add(new Reason("history-integrity", $"Earlier run {id} no longer has verifiable evidence."));
                // A complete earlier INDETERMINATE assessment can be resolved by this valid fresh run.
            }
            catch (Exception error) when (error is TadaException or IOException or JsonException or ArgumentException)
            { gaps.Add(new Reason("history-gap", $"Run {id}: {error.Message}")); }
        }
        if (failures.Count > 0) return current with { Outcome = "FAIL", Reasons = failures.Concat(gaps).ToArray() };
        if (gaps.Count > 0) return current with { Outcome = "INDETERMINATE", Reasons = gaps.ToArray() };
        return current;
    }

    private string Implementation(Config config)
    {
        var roots = config.ImplementationRoots ?? [];
        if (roots.Length == 0) return CanonicalJson.Identity(new { identifiedAs = "no-separate-implementation-configured" });
        var selected = store.Repository.CapturePaths(config with { OracleRoots = roots });
        return CanonicalJson.Identity(selected.Select(path => new { path, digest = CanonicalJson.FileDigest(store.Repository.Resolve(path)) }).ToArray());
    }

    private static Dictionary<string, string> CollectArtifacts(string run, string messagesFile, bool beforeRun = false)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in new[] { "harness", "sut", "observer" })
        {
            string directory = Path.Combine(run, name);
            if (Directory.Exists(directory)) Visit(directory);
        }
        return result;

        void Visit(string directory)
        {
            Repository.NoLink(directory);
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                Repository.NoLink(path);
                string relative = Path.GetRelativePath(run, path).Replace('\\', '/');
                // MTP diagnostic output and the native stream are runtime evidence, not build inputs.
                if (string.Equals(relative, "harness/" + messagesFile, StringComparison.OrdinalIgnoreCase))
                {
                    if (beforeRun) throw new TadaException("Native Messages output existed before execution; do not copy stale reports into the harness build.");
                    continue;
                }
                if (string.Equals(relative, "harness/TestResults", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(relative, "sut/TestResults", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(path)) throw new TadaException("TestResults is reserved for a diagnostics directory, not assertion data.");
                    if (beforeRun && Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
                        throw new TadaException("TestResults is reserved for runtime diagnostics; immutable expected resources must be stored elsewhere.");
                    continue;
                }
                if (Directory.Exists(path)) Visit(path);
                else result.Add(relative, CanonicalJson.FileDigest(path));
            }
        }
    }

    private string RunDirectory(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new TadaException("Expected run ID printed by tada run.");
        string directory = Path.Combine(store.Repository.StateDirectory("runs"), id);
        if (Directory.Exists(directory)) Repository.NoLink(directory);
        return directory;
    }
}

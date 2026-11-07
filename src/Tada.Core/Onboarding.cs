namespace Tada;

public static class Onboarding
{
    public sealed record Initialisation(string[] Created, string[] Updated);

    public static Initialisation Initialise(string root, string? dotnetHost = null)
    {
        Directory.CreateDirectory(root);
        var repository = new Repository(root);
        var files = new Dictionary<string, string>
        {
            [".tada/WORKFLOW.md"] = Workflow,
            [".tada/context.md"] = "# Project context\n\nRecord current goals, accepted behaviour links and unresolved questions here. Treat this as a concise index, not a second functional specification.\n"
                + (dotnetHost == null ? "" : $"\n## Local SDK host\n\nInitialisation used `{dotnetHost}`. Recheck this path and SDK 10.0.403 before use, especially after moving machines. Use this host for SDK/tool commands and pass the same host through `--dotnet` to `tada run`. This is an environment note, not business authority.\n"),
            ["global.json"] = "{\"sdk\":{\"version\":\"10.0.403\",\"rollForward\":\"disable\",\"allowPrerelease\":false},\"test\":{\"runner\":\"Microsoft.Testing.Platform\"}}\n",
            ["NuGet.Config"] = "<configuration><packageSources><clear/><add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\"/></packageSources></configuration>\n",
            [".gitattributes"] = "# Approval identities use exact file bytes. Keep Git from rewriting line endings.\n* -text\n",
            [".tada/.gitattributes"] = "# Captured approval/evidence bytes must survive every checkout unchanged.\n* -text\n",
            ["Directory.Build.props"] = "<Project><PropertyGroup><RestorePackagesWithLockFile>true</RestorePackagesWithLockFile></PropertyGroup></Project>\n",
            ["Directory.Build.targets"] = "<Project/>\n",
            [".gitignore"] = "**/bin/\n**/obj/\n.tada/runs/\n.tada/scratch/\n",
            ["tests/Acceptance/Acceptance.csproj"] = """
                <Project Sdk="MSTest.Sdk/4.5.2">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType>
                    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
                    <EnableMSTestRunner>true</EnableMSTestRunner><UseVSTest>false</UseVSTest>
                    <TestingExtensionsProfile>None</TestingExtensionsProfile>
                    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
                    <ReqnrollUseIntermediateOutputPathForCodeBehind>true</ReqnrollUseIntermediateOutputPathForCodeBehind>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Reqnroll.MSTest" Version="[3.3.4]"/>
                    <PackageReference Include="Reqnroll" Version="[3.3.4]"/>
                    <PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="[3.3.4]"/>
                    <None Update="reqnroll.json" CopyToOutputDirectory="PreserveNewest"/>
                    <ReqnrollFeatureFile Include="../../features/**/*.feature" Link="Features/%(RecursiveDir)%(Filename)%(Extension)"/>
                  </ItemGroup>
                </Project>
                """,
            ["tests/Acceptance/reqnroll.json"] = "{\"formatters\":{\"message\":{\"outputFilePath\":\"tada-messages.ndjson\"}}}\n",
            ["tests/Acceptance/Bindings.cs"] = """
                using Microsoft.VisualStudio.TestTools.UnitTesting;
                using Reqnroll;

                [assembly: DoNotParallelize]

                // Replace this candidate binding with reviewed setup, HTTP observations and assertions.
                // Missing implementation is not a meaningful behavioural RED.
                [Binding]
                public sealed class AcceptanceBindings
                {
                    [Given("the agreed acceptance setup exists")]
                    public void Setup() => throw new PendingStepException();
                }
                """,
            ["tests/Acceptance/TadaRuntimeCheckpoint.cs"] = RuntimeCheckpoint,
            ["features/README.md"] = "# Business acceptance\n\nWrite human-agreed `.feature` scenarios here. Tag requirements `@REQ_NAME` and ordinary scenarios `@AC_NAME`. TADA deliberately creates no invented business requirements. The tool rejects missing cases rather than treating an empty suite as success.\n"
        };
        var created = new List<string>();
        foreach (var pair in files)
        {
            string path = Path.Combine(root, pair.Key);
            if (File.Exists(path)) continue;
            string? parent = Path.GetDirectoryName(path);
            Directory.CreateDirectory(parent!);
            // Check each existing segment after creation, before writing any content.
            if (!string.Equals(Path.TrimEndingDirectorySeparator(parent!), repository.Root, StringComparison.OrdinalIgnoreCase))
                repository.Resolve(Path.GetRelativePath(root, parent!).Replace('\\', '/'), directory: true);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                writer.Write(pair.Value + "\n");
            created.Add(pair.Key);
        }
        string config = Path.Combine(root, "tada.json");
        if (!File.Exists(config))
        {
            Data.WriteNew(config, new Config(1, ["features", "tests/Acceptance"],
                new Execution("tests/Acceptance/Acceptance.csproj", "Acceptance.dll", "tada-messages.ndjson")));
            created.Add("tada.json");
        }
        var updated = new List<string>();
        foreach (string entry in new[] { "AGENTS.md", "AGENTS.override.md" })
            if (File.Exists(Path.Combine(root, entry)) && RemoveLegacyGuidance(entry)) updated.Add(entry);
        return new Initialisation(created.ToArray(), updated.ToArray());

        bool RemoveLegacyGuidance(string relative)
        {
            string path = repository.Resolve(relative);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var reader = new StreamReader(stream, new System.Text.UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            string existing;
            try { existing = reader.ReadToEnd(); }
            catch (System.Text.DecoderFallbackException)
            {
                throw new TadaException($"Cannot decode {relative} as UTF-8 or BOM-marked Unicode. Existing instructions were left unchanged.");
            }
            var sections = new List<string>();
            if (existing.Contains("<!-- TADA:BEGIN -->", StringComparison.Ordinal) || existing.Contains("<!-- TADA:END -->", StringComparison.Ordinal))
            {
                int start = existing.IndexOf(AgentInstructions, StringComparison.Ordinal);
                if (start < 0 || existing.IndexOf("<!-- TADA:BEGIN -->", StringComparison.Ordinal) != start ||
                    existing.LastIndexOf("<!-- TADA:BEGIN -->", StringComparison.Ordinal) != start ||
                    existing.IndexOf("<!-- TADA:END -->", StringComparison.Ordinal) != start + AgentInstructions.Length - "<!-- TADA:END -->".Length ||
                    existing.LastIndexOf("<!-- TADA:END -->", StringComparison.Ordinal) != start + AgentInstructions.Length - "<!-- TADA:END -->".Length)
                    throw new TadaException($"Edited or incomplete legacy TADA guidance in {relative} requires reconciliation before plugin controls can govern the workflow.");
                string owned = AgentInstructions;
                if (start >= 2 && existing.AsSpan(start - 2, 2).SequenceEqual("\n\n")) owned = "\n\n" + owned;
                if (existing.AsSpan(start + AgentInstructions.Length).StartsWith("\n")) owned += "\n";
                sections.Add(owned);
            }
            if (existing.Contains(LegacyEntryParagraph, StringComparison.Ordinal)) sections.Add(LegacyEntryParagraph);
            if (sections.Count == 0) return false;
            // Preserve the BOM, encoding and every byte outside known generated guidance.
            stream.Position = 0;
            byte[] remaining = new byte[checked((int)stream.Length)];
            stream.ReadExactly(remaining);
            byte[] original = remaining;
            foreach (string section in sections)
            {
                byte[] encoded = reader.CurrentEncoding.GetBytes(section);
                int offset = remaining.AsSpan().IndexOf(encoded);
                if (offset < 0) throw new TadaException($"Cannot locate the legacy guidance bytes in {relative}.");
                remaining = [.. remaining.AsSpan(0, offset), .. remaining.AsSpan(offset + encoded.Length)];
            }
            string temporary = path + ".tada-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    output.Write(remaining);
                    output.Flush(flushToDisk: true);
                }
                reader.Dispose();
                stream.Dispose();
                if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
                    throw new TadaException($"Instructions changed while migrating {relative}. Retry against the current file.");
                File.Move(temporary, path, overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return true;
        }
    }

    private const string LegacyEntryParagraph = "Read `.tada/WORKFLOW.md` and `.tada/context.md` before changing this repository. Gherkin defines business acceptance. Ask the human to agree acceptance-changing decisions; routine implementation choices are delegated.\n";

    // Exact alpha.8 bytes form the migration signature.
    public const string AgentInstructions = """
        <!-- TADA:BEGIN -->
        ## TADA is enabled for this repository

        Apply the TADA workflow by default to ordinary project requests. The user does not need to mention TADA, Gherkin, tests or this file in their prompt. Before acting, read `.tada/WORKFLOW.md` and `.tada/context.md`, recover the current goal and evidence, and inspect applicable project instructions. Respect explicit user decisions and flag conflicting instructions rather than silently bypassing them.

        - For a new project or feature, discover the intended behaviour with the human, agree success and failure examples, and express agreed acceptance in executable Gherkin before implementing business behaviour. Prepare meaningful independent bindings and the immutable baseline for human review/approval. Never approve expectations on the human's behalf. Implement in small verified increments; distinguish business RED from broken setup.
        - For a bug fix, use an existing agreed executable scenario when it describes the defect; otherwise agree the missing expectation. Demonstrate the intended regression, repair it and recheck existing obligations. Never weaken assertions or erase failure history to obtain PASS.
        - For a chore or behaviour-preserving refactor, choose proportionate existing regression, build or quality checks without inventing business scenarios. Return to acceptance discussion if behaviour changes.
        - Use the SDK/tool host recorded in context when applicable; recheck its availability and the pinned SDK. Run the repository-local tool through that host (`dotnet tool run tada`), and pass the same host through `--dotnet` when needed. Keep environment setup details out of the user's project request.
        - Before claiming completion, verify the selected agreed expectations and useful quality checks, report unresolved gaps and local advisory L1 limits, and update concise Git-native context with evidence and the next action. A fresh session must recover this itself; do not ask the user to repeat the workflow.
        <!-- TADA:END -->
        """;

    public const string RuntimeCheckpoint = """
        using System.Diagnostics;

        // Cooperate with TADA's injected observer without referencing application or verifier assemblies.
        public static class TadaRuntimeCheckpoint
        {
            public static void Complete(Process process)
            {
                string? directory = Environment.GetEnvironmentVariable("TADA_RUNTIME_DIRECTORY");
                if (directory == null || process.HasExited) return;
                string run = Environment.GetEnvironmentVariable("TADA_RUN_ID") ?? throw new InvalidOperationException("TADA run identity is missing.");
                string acknowledgement = Path.Combine(directory, $"{process.Id}.checkpoint-complete");
                File.WriteAllText(Path.Combine(directory, $"{process.Id}.checkpoint-request"), run);
                var deadline = Stopwatch.StartNew();
                while (!File.Exists(acknowledgement))
                {
                    if (process.HasExited) throw new IOException("API exited before its runtime checkpoint completed.");
                    if (deadline.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("API runtime checkpoint did not complete.");
                    Thread.Sleep(10);
                }
                if (File.ReadAllText(acknowledgement) != run) throw new IOException("API runtime checkpoint belongs to another run.");
            }
        }
        """;

    public const string Workflow = """
        # TADA agent workflow

        Apply this workflow when the TADA plugin is enabled or the user explicitly requests it. The plugin supplies automatic workflow selection for ordinary software requests. Read this file and `context.md` when applying TADA to recover project knowledge. The host's plugin controls govern automatic selection in fresh sessions.

        ## Choose proportionate work

        - Feature or business defect: discover examples with the human. Put agreed behaviour in Gherkin, not a competing Markdown specification. Tag explicit requirement IDs `@REQ_...` and each ordinary scenario `@AC_...`. Resolve ambiguity that changes acceptance. Do not approve on the human's behalf.
        - Chore, maintenance or behaviour-preserving refactor: state the objective and choose appropriate existing regression, build, format, compatibility or fitness checks. Do not invent a business scenario for ceremony. Reclassify if behaviour changes. Existing acceptance obligations still apply.
        - Quality goals need concrete workloads/thresholds or explicit human review criteria; undefined targets cannot yield a green claim.

        ## Specify, verify, proceed

        1. Inspect the current code, relevant Gherkin, context and Git status. Preserve unrelated work.
        2. Agree examples and boundaries with the human. For a new API, choose one useful endpoint first; identify observable success and failure cases before coding the application.
        3. Build scenario-scoped bindings, setup, public observations and meaningful assertions. Keep the harness independent of application implementation assemblies. Review privileged observations explicitly. Each scenario owns its resources and attempts cleanup after failure.

        Keep generated databases, state and logs under the supplied `TADA_RUN_DIRECTORY`, outside immutable build outputs. Static application/harness outputs, including non-code resources, are integrity checked. The configured native Messages file and MTP's reserved `TestResults` diagnostics directory are runtime-evidence exceptions, not locations for expected assertion data.
        4. Run `dotnet restore tests/Acceptance/Acceptance.csproj` to create a dependency lock. Pin and review build inputs before baseline preparation. Configure the harness and optional SUT build project in `tada.json`.

        Include linked source, shared project references and custom imports in the appropriate selected oracle or implementation roots. Each project needs its own dependency lock. TADA uses fresh build intermediates and rejects observed inputs outside those selections or the supported installed SDK/pinned-package policy. This local policy trusts reviewed build targets and installed toolchain/cache contents; it is not an execution sandbox.

        TADA runs builds and the harness with a recorded environment containing supported OS locations and .NET/NuGet cache locations. Arbitrary ambient MSBuild properties, CLR injection variables and automatic MSBuild response files are excluded. Set scenario-specific application inputs explicitly in reviewed bindings.

        Actual restore/rebuild task journals identify invoked code and structured file inputs. SDK/pinned-package tasks remain trusted toolchain components. Standard file-reading tasks must consume captured inputs. Opaque custom tasks, shell commands and file/opaque project-evaluation expressions require exact `buildTaskContracts` declarations in `tada.json`: `origin`, `target`, `task` and `inputRoots`. The origin and declared roots join the immutable oracle snapshot. Use target `@evaluation` and task `property-functions` for opaque evaluation. Review the code and declare every read root; do not add broad trust entries merely to suppress a gap. This is an explicit local trust contract, not file-system tracing or a sandbox.
        5. Run `tada baseline prepare`. Review its printed digest using `tada baseline review --baseline <digest>`. Review captured source in `.tada/snapshots/<hex>/`, not only the inventory summary. For amendments, pass `--against <old-digest>`.
        6. The human records explicit approval with `tada baseline approve --review <review-id> --by <name>`. Approval defines the target; it does not require green tests. Use `--replaces <old-digest>` for a reviewed replacement that preserves earlier obligations.
        7. Run `tada run --baseline <digest>`. A defect should fail at its intended outcome assertion. Missing bindings, a broken build or unavailable setup are INDETERMINATE, not meaningful behavioural RED. Already-satisfied behaviour needs no artificial RED.
        8. Implement a small change to the application. Preserve the approved oracle. Repeat the run; resolve failures and evidence gaps. Never remove assertions, silently change baseline content, filter away required cases or discard failure history to obtain PASS.
        9. Run appropriate regression and quality checks. Use `tada verify --run <id>` to inspect retained evidence without executing the application again. PASS is scoped to the selected approved expectations, not complete correctness.
        10. Record a concise handoff in `.tada/context.md`: current goal, authoritative feature links, baseline and run IDs, implementation identity, commands/results, unresolved issues and the next action. Update or supersede stale entries; do not append duplicate summaries forever. Review and commit useful context, approvals and baseline records using normal Git review. Do not commit secrets or ephemeral runtime state.

        ## Results and trust

        PASS=0: complete supported evidence satisfies the approved cases. FAIL=1: a trustworthy outcome assertion demonstrates unmet behaviour. INDETERMINATE=2: integrity, infrastructure or evidence gaps prevent verification. Command error=3.

        Results are local advisory L1. A controlling local actor can forge approval or producer records. Exact hashes detect correspondence, not assertion adequacy or authentic human review. Running the application separately does not establish protected independent assurance.

        Build-input evidence identifies supported evaluated compiler/resource/reference items and preprocessed imports. Runtime observation identifies located managed assemblies, native loader events and initial/final module snapshots against captured outputs, the pre-run .NET platform and trusted Windows system locations. Native loader events retain paths even when a DLL is unloaded before polling; lost/incomplete events block PASS. Arbitrary custom-task file reads remain untraced. Preserve these limitations in reports; do not claim complete execution-input coverage.

        Before terminating each scenario-owned API, call the generated `TadaRuntimeCheckpoint.Complete(api)` helper. Put process termination, waiting and disposal in `finally` so failed observation still releases resources. The helper requests a final managed/native snapshot and waits for its acknowledgement. Normal graceful exits are captured automatically. An abruptly killed API without a final snapshot is INDETERMINATE, even when the native test runner reports success. API application code needs no TADA dependency.

        ## Bootstrap and maintenance

        `tada init` prepares project scaffolding and records the invoking SDK host in new context. Enable or disable the TADA plugin through the host's plugin controls. Initialisation removes the exact legacy alpha.8 TADA block from root agent instructions so the plugin toggle can govern automatic behaviour. Edited legacy blocks require reconciliation. Existing baseline, review, approval and run records remain project knowledge for future work. Scenario Outlines are deliberately unsupported in the initial version.

        New projects disable Git line-ending conversion with `* -text` so captured bytes survive checkouts. Existing `.gitattributes` files are preserved: review equivalent rules for your selected oracle inputs. `.tada/.gitattributes` preserves captured records independently. Formatting changes still require review; never normalise bytes to carry approval forward.

        For a chore: use the proportionate path above, execute the selected checks, preserve existing approvals and record the result in context. A `tada` business baseline is not mandatory for a documentation edit. For business behaviour changes, return to collaborative discovery and explicit baseline review.
        """;
}

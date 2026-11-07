# Build and try the development alpha

This is a working development increment, not a published or release-ready v1. Initial support: Windows x64 with .NET SDK **10.0.403**. Public feed availability of `Tada.Cli` is not assumed.

Building TADA from source also requires PowerShell 7. The build downloads a checksum-pinned Zig 0.15.2 compiler into `.artifacts/` for its small native observer (about 93 MB compressed). Nothing is installed system-wide. The packaged tool includes the observer DLL; application developers need no C compiler. See [native observation](research/native-loader-observation.md) for the build pin and boundary.

The demonstrated stack uses Reqnroll **3.3.4**, MSTest SDK/framework **4.5.2**, MTP **2.5.2** and native Cucumber Messages protocol **30.1.0**. The tool separately bundles Gherkin **35.0.0** with Cucumber.Messages **29.0.1** for parsing; that library version is not the accepted wire-protocol version. Other runner/protocol/platform combinations remain unvalidated.

## Install from this workspace

From the TADA source directory:

```powershell
dotnet build tests/Tada.Core.Tests/Tada.Core.Tests.csproj
dotnet tests/Tada.Core.Tests/bin/Debug/net10.0/Tada.Core.Tests.dll --progress off
dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages
$tadaSource = (Get-Location).Path
```

In a new application directory, create a local tool manifest. Use an explicit local-only installation feed so a similarly named public package cannot be selected for this development build:

```powershell
mkdir MyApi
cd MyApi
dotnet new tool-manifest
$feedPath = [Security.SecurityElement]::Escape("$tadaSource/.artifacts/packages")
"<configuration><packageSources><clear/><add key='local' value='$feedPath'/></packageSources></configuration>" | Set-Content NuGet.Local.Config
dotnet tool install Tada.Cli --version 0.1.0-alpha.8 --configfile NuGet.Local.Config
dotnet tool run tada --version
dotnet tool run tada init
```

Commit the tool manifest, configuration, guidance and useful context. Depending on SDK defaults the manifest can be `dotnet-tools.json` or `.config/dotnet-tools.json`; TADA captures either. Later sessions use `dotnet tool restore` with the appropriate feed configuration. A published installation command will follow an actual release.

On Windows, run `git config core.longpaths true` inside the application repository before committing or checking out deep snapshot paths. This is a repository-local setting; the snapshot digest directories can exceed Git's default Windows path limit in a deeply nested workspace. Validation commands supply `-c core.longpaths=true` without changing global Git settings.

`init` enables TADA as the project's default workflow. It creates root `AGENTS.md` or appends one bounded TADA section to an existing file, preserving its original bytes. If a root `AGENTS.override.md` exists, it receives the same section because Codex selects it ahead of `AGENTS.md`. Other existing files, including `NuGet.Config`, are preserved. Repeated initialisation changes nothing; conflicting or incomplete TADA sections are reported for reconciliation. The generated harness contains pending setup and no invented business requirements; an empty suite is deliberately not a pass.

The CLI records its .NET host in newly generated context so a portable SDK can be used without spelling its path in the project request. Recheck that host after moving machines. Installation makes the CLI available; `init` is the explicit per-project enable step and does not change global agent settings.

New projects preserve file bytes through Git checkouts with `* -text`; `.tada/.gitattributes` independently protects captured records. Existing root attributes are not overwritten. Review equivalent rules for your oracle paths if adopting TADA in an existing repository. Exact-byte checks intentionally reject approval carry-forward after actual file changes.

## Work with Codex or another agent

After enabling, start a fresh Codex chat in the application directory and ask for the project normally. For example:

> Build me a simple API for tracking my reading list.

Use your own project and wording. Do not supply TADA instructions, Gherkin mechanics, workflow-file paths or a special starter prompt. The enabled project must cause the agent to discover behaviour, agree executable acceptance with you and follow the appropriate workflow itself. If it needs coaching to do so, record an activation/usability failure. Follow the [ordinary-request trial](validation/default-workflow-trial.md) to assess this experience.

Codex discovers project instructions when a session starts; root `AGENTS.override.md` takes precedence over root `AGENTS.md`. See the [official OpenAI documentation](https://learn.chatgpt.com/docs/agent-configuration/agents-md). Nested overrides, custom instruction discovery and size limits can affect the effective guidance. Other agents must load the repository entry point through their normal instruction mechanism; installing a CLI alone cannot alter an agent that ignores project instructions. Automatic adoption by a real fresh agent remains an experience gate, distinct from testing file generation.

The core requires no model API or agent SDK. Gherkin is business authority; context Markdown indexes it and records reasoning without duplicating the functional specification.

Implement meaningful bindings with scenario-owned setup and cleanup. Observe public application outcomes; privileged observation needs its applicable scoped agreement. Do not share application business-rule assemblies with assertions. In `tada.json`:

Capture HTTP responses even when their status or body is wrong; assert the expected status/fields in outcome steps. Do not call `EnsureSuccessStatusCode` or require successful JSON parsing in an action step before assertions can inspect a valid error response. Unavailable transport/setup is INDETERMINATE; an observed response violating an agreed expectation should establish behavioural FAIL.

- `oracleRoots` selects feature/harness sources. Include material shared inputs explicitly.
- `execution.project`, `assembly` and `messagesFile` identify the harness and native formatter output.
- Optional `execution.sutProject` and `sutAssembly` select the separate application build. The harness receives `TADA_SUT_ASSEMBLY` and owns scenario-specific launch/cleanup.
- `implementationRoots` selects application sources, identified separately from the oracle.

Include linked source, custom imports and referenced projects in the appropriate selected roots; every project needs its own `packages.lock.json`. The harness cannot borrow unselected implementation sources as reviewed oracle inputs. Inputs outside the repository must belong to the supported installed SDK or an explicitly pinned package/SDK dependency; arbitrary external/shared source is unsupported. Copy it into a selected repository location for review.

Alpha.7 records evaluated compiler/resource/reference inputs and preprocessed imports in `build-inputs.json`, alongside hashed before/after MSBuild reports. Builds use fresh per-run intermediates and process project references in dependency order. SDK, reference-pack and workload-manifest identities come from the actual pinned SDK installation, rather than a project-overridable SDK property. Package paths are limited to the restored locked graph and explicitly versioned NuGet SDKs. This policy trusts installed toolchain/cache contents: it does not independently authenticate package bytes from NuGet lock hashes or sandbox reviewed build tasks.

New baselines use inventory schema 6 and explicitly record the build-input, runtime-observation and task-input policies. Older baselines remain readable historical records, but alpha.7 will not run or verify them as if the new identity policy had been captured. Use the matching historical tool to inspect its old results, or prepare/review/approve a new baseline for alpha.7. Existing approval is not silently carried forward.

Actual restore/rebuild task journals identify invoked task code and supported structured file inputs. Opaque custom tasks, shell commands and detected file/opaque evaluation require reviewed `buildTaskContracts` declarations. The origin and declared read roots join the oracle snapshot. See [build-task contracts](build-task-contracts.md) for configuration and limitations: declarations trust reviewed code and do not trace arbitrary I/O.

Builds and the harness receive a recorded environment containing OS locations and supported .NET/NuGet cache locations. Arbitrary ambient MSBuild properties, CLR injection variables and automatic MSBuild response files are excluded. Custom compiler response files are unsupported; declare settings and inputs in reviewed project files. Scenario-specific application inputs belong explicitly in reviewed bindings.

TADA injects its packaged observer into the harness and owned .NET API processes; application projects need no TADA reference. It records located managed assembly loads, ordinary Windows DLL load notifications and initial/final native snapshots with process/run identities and byte hashes. Notifications retain DLL paths even when a module unloads before polling. Expected inputs are captured outputs, the pre-run .NET platform snapshot and trusted Windows system locations. Memory-only assemblies, unidentified module paths, lost notifications and queue/path limits block verification. Both observer binaries, the environment, raw process streams and their manifest have retained identities; offline verification re-evaluates the raw streams and loader event counts.

Before killing each scenario-owned API, request its final snapshot using the helper generated by `init`:

```csharp
try { TadaRuntimeCheckpoint.Complete(api); }
finally
{
    if (!api.HasExited) api.Kill(entireProcessTree: true);
    api.WaitForExit(5000);
    api.Dispose();
}
```

Keep database/resource cleanup in an outer `finally` as well. Graceful process exits are observed automatically. A killed API without a final snapshot makes verification INDETERMINATE even if the runner succeeds. Existing projects must explicitly add/review the helper and cleanup call; `init` preserves existing files. Perform no business work after the final checkpoint. Arbitrary custom-task file reads remain untraced; reviewed contracts declare their complete read roots.

The harness also receives `TADA_DOTNET_HOST`, `TADA_RUN_DIRECTORY` and `TADA_REPOSITORY`. Use the supplied host when launching the application; do not assume that a globally installed SDK sets `DOTNET_ROOT`.

Place generated scenario databases, state and logs under `TADA_RUN_DIRECTORY`. Build outputs are integrity checked, including non-code resources such as CSV expectations. The configured native Messages file and MTP's `TestResults` directory are reserved runtime outputs; do not copy old reports or store expected assertion data there.

Restore the harness and optional application to create reviewed locks, then:

```powershell
dotnet restore tests/Acceptance/Acceptance.csproj
dotnet tool run tada baseline prepare
dotnet tool run tada baseline review --baseline sha256:<printed-digest>
```

Review captured files in `.tada/snapshots/<digest-without-prefix>/`, including assertions, setup, observers and configuration. The **human** records approval:

```powershell
dotnet tool run tada baseline approve --review <printed-review-id> --by "Your name"
dotnet tool run tada run --baseline sha256:<printed-digest>
dotnet tool run tada verify --run <printed-run-id>
```

Replace placeholders with actual IDs; do not type angle brackets literally. `run` performs locked restore, rebuilds fresh output, checks input drift, executes once and retains commands, binaries and native Messages. `verify` does not rerun the application. An intended outcome assertion establishes meaningful behavioural RED; a broken build or pending binding is INDETERMINATE. Already-satisfied behaviour needs no artificial RED.

Default output is readable terminal text, including the captured Gherkin during review. Add `--json` for scripting. Review is not approval; the printed next command is for the human, not permission for the agent to approve on their behalf.

Passing reruns recheck earlier native evidence for the same baseline, implementation and environment. Missing cached summaries cannot erase a failure. Interrupted earlier attempts or damaged history block PASS until the history is investigated and restored; complete earlier setup/evidence gaps can be resolved by valid fresh evidence. Do not delete run directories to make an assessment pass.

Implementation identity identifies selected implementation source. Exact executable/resource hashes remain protected separately in each run's artifact inventory. Temporary compiler paths are mapped consistently; a fresh output directory cannot count as an implementation fix. Environment identity includes observed SDK/package task code and file parameters as well as evaluated tool inputs. A changed observed task file at the same cache path produces a different environment. Cancelling execution terminates the owned process tree, retains command diagnostics and produces INDETERMINATE rather than behavioural RED or PASS.

Implement the application change and rerun the same approved baseline. Oracle edits, including line-ending changes, require new review and approval. For amendments, review `--against <old-baseline>` and explicitly approve `--replaces <old-baseline>`; dropping obligations is rejected by this increment.

## Run the implemented Orders controls

From the TADA source directory:

```powershell
dotnet restore examples/Orders/Api/Orders.Api.csproj
dotnet restore examples/Orders/Acceptance/Orders.Acceptance.csproj
dotnet build src/Tada.Cli/Tada.Cli.csproj -c Release
pwsh -File scripts/Test-OrdersWorkflow.ps1
```

The script uses a disposable source copy and explicitly labelled automated control approvals, not human acceptance. It checks correct Orders behaviour, intended failure for oversubscription, oracle-edit/stale-review rejection and tampered-evidence rejection. Full evidence remains under `.artifacts/orders-workflow/`. The read-only database observer is sample-specific, not a general privileged-observation approval system.

Run `pwsh -File scripts/Test-Onboarding.ps1` after packing to install the package into a new project with a fresh cache. It initialises an unimplemented HTTP host, establishes assertion RED, implements the separate Echo API and verifies GREEN under the same oracle. A maintenance build and Git-committed context handoff follow without an extra business scenario. This is stronger than a developer-cache smoke test, but is not clean-machine or actual requester/Codex acceptance.

## Maintenance and session handoff

For a chore or behaviour-preserving refactor, state the objective and run appropriate build/regression/compatibility/fitness checks. Do not invent Gherkin for ceremony. Existing acceptance obligations still apply; behavioural changes return to acceptance review.

Keep `.tada/context.md` concise: current goal, authoritative feature links, baseline/run IDs, observed results, unresolved issues and next action. Preserve useful reasoning, replace stale entries and exclude secrets. Start a fresh agent session by reading the same workflow and context to verify continuity.

## Outcomes and limits

Verification exits: `PASS=0`, `FAIL=1`, `INDETERMINATE=2`. Command errors return `3`; successful prepare/review/approval operations return `0` without asserting behavioural PASS.

This is **local advisory L1**. It cannot protect approval, binaries or evidence from an actor controlling the environment. Hash correspondence and case counts do not prove assertion adequacy or whole-application correctness. Native observation covers the ordinary Windows loader during its registered interval, not hostile manual image mapping or immutable capture of mapped memory. File bytes must remain stable under the supported policy. Evaluated build-input capture is not exhaustive custom-task file-read tracing. CLR dynamic methods are trusted generated runtime code; other unlocated/dynamic assemblies are unsupported. Scenario Outlines, protected L2 and partial supersession are unsupported. The [release checklist](release-readiness.md) tracks complete execution-input provenance, clean-machine validation, CI and requester acceptance.

Workspace paths may contain spaces. Commas and semicolons in the MSBuild logger path are unsupported in this alpha; choose a workspace without those characters. Use Git's repository-local long-path setting for deeply nested Windows workspaces as described above.

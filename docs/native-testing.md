# Native acceptance execution

The Windows/.NET alpha supports the same acceptance scenarios through Visual Studio Test Explorer, `dotnet test` with MTP and recorded `tada run` execution. Use SDK 10.0.403 with the pinned Reqnroll 3.3.4, MSTest 4.5.2 and MTP 2.5.2 stack.

## Project setup

Alpha.11 initialization generates `TadaAcceptance.cs`, `TadaRuntimeCheckpoint.cs` and `Tada.Acceptance.targets` under `tests/Acceptance`. When selecting the separate API, add this build-only reference to the acceptance project and match its path to `execution.sutProject` in `tada.json`.

```xml
<ItemGroup>
  <ProjectReference Include="../../src/My.Api/My.Api.csproj"
                    ReferenceOutputAssembly="false"
                    Private="false"
                    OutputItemType="TadaSutOutput" />
</ItemGroup>
<Import Project="Tada.Acceptance.targets" />
```

The SDK builds the API dependency in the selected configuration. The generated target stages its complete output, including runtime configuration, dependency assemblies, native libraries and content, under the acceptance output's `sut` directory. It writes a relative API path to `tada-sut.txt`. The reference remains present during both native and recorded restores so the project has one locked dependency graph.

Use the shared context in business bindings.

```csharp
var context = TadaAcceptance.Create();
string database = Path.Combine(context.WorkingDirectory, "scenario.sqlite");
var start = new ProcessStartInfo(context.DotnetHost)
{
    UseShellExecute = false,
    WorkingDirectory = context.WorkingDirectory
};
start.ArgumentList.Add(context.SutAssembly);
```

Complete the fixture with scenario-owned API startup, public observations and cleanup. Use the generated runtime checkpoint before terminating the API. Capture diagnostics, terminate and await owned processes, and remove owned database files even after setup or assertions fail. The helper prints its scenario directory for inspection. Native data and Messages use a unique temporary directory for each test process.

## Run and debug

Open the solution in Visual Studio and use Test Explorer to run all scenarios, run one scenario or debug a binding. The acceptance project's dependency builds its API. From the repository root, run either configuration directly.

```powershell
dotnet test --project tests/Acceptance/Acceptance.csproj -c Debug
dotnet test --project tests/Acceptance/Acceptance.csproj -c Release
```

`--no-build` requires previously built acceptance and API outputs in the selected configuration. Missing staged output produces a rebuild diagnostic. Native execution resolves the .NET host from `DOTNET_HOST_PATH` or `dotnet` on PATH. A supported .NET installation and loopback/process permissions are application prerequisites.

During `tada run`, the shared context uses TADA's complete run environment and separately captured API. The engine sets `TadaVerifiedBuild=true` during restore, evaluation and rebuild to suppress native staging. Partial managed-run variables produce a setup error, even when native outputs exist. Normal runner results describe the executed scenarios. TADA PASS additionally requires human approval, captured execution identities, complete observation and retained-evidence verification.

## Existing projects and evidence

Initialization preserves existing source files. Copy the new helper and target into an existing harness, add the import and reference, and change fixture setup to consume the context. Review the source diff and deliberately regenerate the changed locked graph. The feature files and business assertions can retain their exact bytes while fixture and project files change.

Updated harness bytes require a new baseline review and human approval. Preserve old baselines and runs. New build evidence uses `evaluated-dotnet-build-v4` and evidence schema 2 to retain explicit dependency roles and validate them against before/after evaluation reports. The verifier retains schema-1/v3 evidence support for historical runs. Application build-only references must identify the configured SUT, while ordinary harness references continue to require reviewed harness inputs.

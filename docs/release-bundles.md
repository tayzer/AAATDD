# Development download bundles

The download bundle lets an application developer try the packaged tool without building TADA or downloading a C compiler. It contains a local NuGet feed, installation/agent-start guidance, TADA's MIT licence, the package's third-party notices, a file manifest and SHA-256 checksums.

This remains a development preview. The [hosted checkpoint](validation/hosted-ci.md) records automated Windows installation and API controls for an exact source commit. Requester acceptance remains pending. No public feed or GitHub release is assumed. Package metadata can record a parent Git commit when built from a dirty working tree; that commit alone does not identify the uncommitted implementation. Final release artifacts must come from the validated final source commit.

## Prepare and check a bundle

After building and validating the tool package, run from the repository root:

```powershell
$bundle = & ./scripts/New-ReleaseBundle.ps1 -Package .artifacts/packages/Tada.Cli.0.1.0-alpha.9.nupkg
pwsh -File scripts/Test-ReleaseBundle.ps1 -Archive $bundle.archive
```

The output is a fresh directory under `.artifacts/release-bundles/`; existing artifacts are not overwritten. `bundle.json` records archive/package hashes. The script checks the package ID, alpha version, declared licence and exact redistributed DLL set before packaging. A changed dependency set requires deliberate notice review.

`Test-ReleaseBundle.ps1` checks the complete extracted file set and every checksum before invoking the SDK. It installs using only the extracted feed, isolated package/resolver caches and paths containing spaces. It confirms version output, preservation of existing instructions, generated workflow/context and repeatable `init`. The control suppresses SDK global-tool PATH updates and checks the user PATH remains unchanged. This transport/install control complements the [full API workflow](quickstart.md); it does not replace it.

## Try the application workflow

Extract the ZIP and follow its README. The application prerequisites are Windows x64, .NET SDK 10.0.403 and Git for Windows. Use `dotnet tool run tada` from the application directory. No model API or agent SDK is required.

Use the [TADA plugin](plugin.md) for automatic workflow selection through the host's enable/disable controls. The CLI bundle prepares and verifies projects. With the plugin enabled, open a fresh session and describe your project normally. Agree success/failure examples and Gherkin, review the executable oracle and declare approval. Build the separate API in small verified increments and preserve existing obligations during maintenance.

For the final experience check, request a bug fix, a refactor and a chore normally, then continue in another fresh agent session without telling it to read a handoff. Record any missing guidance or confusing recovery steps. A reminder to use TADA is evidence of a failed adoption attempt, even if a coached retry succeeds. The [ordinary-request trial](validation/default-workflow-trial.md) and [release checklist](release-readiness.md) keep these requester gates separate from automated controls.

Checksums establish file correspondence, not publisher authentication. Results remain local advisory L1 with the [documented provenance limits](validation/provenance-audit.md).

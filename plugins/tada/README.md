# TADA plugin

TADA (Test-Assured Development Assistance) makes your coding agent agree what success means before it writes code. For new or changed business behaviour, the agent investigates the existing system, asks the questions that change acceptance, and proposes Gherkin examples. You approve them. The approved expectations are captured byte for byte, implementation proceeds in small steps from a genuine failing check, and results are reported as `PASS`, `FAIL` or `INDETERMINATE` from retained evidence. Chores and refactors reuse existing checks without invented scenarios.

The plugin works with Codex, Claude Code and Cursor. It supports C#/.NET APIs with Reqnroll and MSTest, and requires Windows x64, a stable .NET 10.0 SDK, PowerShell 7 and Git for Windows. Results are local advisory (L1): approvals are declarations by whoever controls the workspace.

## Install

**Codex**

```powershell
codex plugin marketplace add tayzer/tada-plugins
codex plugin add tada@tada
```

**Claude Code**

```powershell
claude plugin marketplace add tayzer/tada-plugins
claude plugin install tada@tada
```

To install for one repository only, change to that application repository and add `--scope project` to the install command there. `claude plugin disable tada@tada` and `claude plugin enable tada@tada` turn it off and on.

**Cursor:** install TADA from the Cursor marketplace, or copy this folder to `~/.cursor/plugins/local/tada` and restart Cursor.

You can also register an extracted plugin ZIP as a local marketplace: run `codex plugin marketplace add <folder>` or `claude plugin marketplace add <folder>` with the extracted folder.

Start a fresh chat in the application repository and describe the project or change normally. Start a fresh chat after turning the plugin off or on.

## What this plugin runs and changes

TADA contains one skill and no hooks or MCP servers. TADA itself sends no telemetry and contacts no TADA service. The .NET SDK it invokes follows your own .NET telemetry settings.

- When a task first needs business verification, the agent runs `skills/tada/scripts/Initialize-Tada.ps1` with PowerShell 7. The script installs the `Tada.Cli` .NET tool, version-matched to this plugin, as a repository-local tool from the bundled `feed/` folder, never from a public feed. It then runs `tada init`.
- `tada init` adds project files, such as `tada.json`, `.tada/`, `features/`, `tests/Acceptance/`, `global.json` and `NuGet.Config`. It never overwrites existing files. It removes only generated TADA instruction blocks left by earlier alphas.
- `tada run` restores and builds your project and its acceptance tests with `dotnet`. Restore downloads your project's own NuGet dependencies from nuget.org, as `NuGet.Config` specifies. It then runs the tests locally and stores evidence under `.tada/runs/`.
- The bundled `feed/Tada.Cli.*.nupkg` is the compiled verifier, built from the public source in [tayzer/TADA](https://github.com/tayzer/TADA). Release checksums identify its exact bytes.

Approvals are always made by a human. The agent prints the approval command but never runs it on your behalf.

Read the installation guide, configuration reference and release notes in the [TADA repository](https://github.com/tayzer/TADA).

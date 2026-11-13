# TADA

**Test-Assured Development Assistance.** Your coding agent agrees *what success means* with you before it writes code, then proves the result against that agreement. TADA works with Codex, Claude Code and Cursor.

[![Release](https://img.shields.io/github/v/release/tayzer/TADA?include_prereleases&label=release)](https://github.com/tayzer/TADA/releases)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-lightgrey)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Hosts](https://img.shields.io/badge/hosts-Codex%20%7C%20Claude%20Code%20%7C%20Cursor-black)

**[Install](#install)** · **[First use](#first-use)** · **[Turn it on and off](#turn-it-on-and-off)** · **[Optional pieces](#optional-pieces)** · **[Commands](#commands)** · **[Configuration](#configuration)** · **[Uninstall](#update-or-uninstall)** · **[Troubleshooting](#troubleshooting)** · **[How it works](docs/how-it-works.md)**

> AI may propose and implement changes, but it must not independently redefine the conditions by which those changes are judged successful.

## What you get

You ask your agent for something in plain words. With TADA enabled, the agent:

1. **Investigates first.** It looks at the system you actually have and tells you what it verified, what it inferred and what it still doesn't know.
2. **Asks the questions that matter.** It covers business rules, edge cases, compatibility and risks. It does not ask about routine engineering choices.
3. **Writes the agreement down as Gherkin examples for you to approve.** The agent may propose them, but only you can approve them.
4. **Locks the approved expectations.** It captures them byte for byte, so the agent cannot quietly change a test to make it pass.
5. **Implements in small steps.** It shows a genuine failing check first, then makes it pass.
6. **Reports `PASS`, `FAIL` or `INDETERMINATE` from retained evidence.** A green test runner on its own is not a TADA pass.

Chores and refactors take a lighter path. They reuse existing checks, and the agent does not invent business scenarios for ceremony.

```text
You:   Orders above five units should be rejected.

Agent: I checked OrdersController, the stock reservation path and the existing
       order scenarios. Two questions before I change anything. Should the limit
       apply per order line or per order? And should a rejected order return
       400 or 422?
       ...
       Here are three proposed scenarios (features/orders.feature). Please review
       them. When you are happy, run the approval command below yourself.
```

## Requirements

| Need | Version |
| --- | --- |
| OS | Windows x64 |
| .NET SDK | 10.0.403 for the alpha.12 release and for building TADA. From source, application projects accept any stable .NET 10.0 SDK. |
| Git | Git for Windows |
| Agent host | [Codex](https://developers.openai.com/codex), [Claude Code](https://code.claude.com/docs) or [Cursor](https://cursor.com/docs/plugins) |
| Project type | C#/.NET API, with acceptance tests in Reqnroll 3.3.4 and MSTest 4.5.2 (generated for you) |

The current release is `0.1.0-alpha.12`, an early alpha. Its results are **local advisory (L1)**. See [Limits](#limits).

| Host | Release ZIP | TADA marketplace | Public directory |
| --- | --- | --- | --- |
| Codex | alpha.12 and later | From alpha.13 | Submission planned |
| Claude Code | From alpha.13 | From alpha.13 | Submission planned |
| Cursor | From alpha.13 | Team import from alpha.13 | Submission planned |

## Install

TADA has two parts:

- **The plugin** gives the agent the workflow. Most people only install this. It carries the matching verifier with it.
- **The `tada` CLI** handles approvals and verification. The plugin installs it into a project automatically when a task needs it.

### From the TADA marketplace (alpha.13 and later)

The [`tayzer/tada-plugins`](https://github.com/tayzer/tada-plugins) repository is the install source for all three hosts. Each release updates it automatically.

**Codex**

```powershell
codex plugin marketplace add tayzer/tada-plugins
codex plugin add tada@tada
```

In Codex, `/plugins` opens the plugin browser, where TADA appears under the `tada` marketplace.

**Claude Code**

```powershell
claude plugin marketplace add tayzer/tada-plugins
claude plugin install tada@tada
```

Add `--scope project` to the install command to enable TADA for the current repository only. Inside Claude Code, `/plugin` opens the same controls.

**Cursor**

Install TADA from the Cursor marketplace, once its listing is approved. Until then, or on a plan without marketplace access, download `TADA-plugin-<version>.zip` from the [releases page](https://github.com/tayzer/TADA/releases), then copy it into Cursor's local plugin folder:

```powershell
Expand-Archive .\TADA-plugin-<version>.zip -DestinationPath "$HOME\.cursor\plugins\local\tada"
```

Quit and reopen Cursor; a window reload is not always enough. TADA then appears under **Customize**. If it does not, enable **Include third-party Plugins, Skills, and other configs** in Cursor settings. Copy the folder rather than linking it, because Cursor does not reliably load linked plugins on Windows. Teams and Enterprise workspaces can import `tayzer/tada-plugins` as a team marketplace. See [plugin details](docs/plugin.md#install-and-toggle-in-cursor).

### From a release ZIP

Every [release](https://github.com/tayzer/TADA/releases) also attaches `TADA-plugin-<version>.zip` and `SHA256SUMS`, for offline machines or for checking exact bytes:

```powershell
$expected = ((Select-String SHA256SUMS -Pattern 'TADA-plugin-').Line -split '\s+')[0]
(Get-FileHash .\TADA-plugin-<version>.zip -Algorithm SHA256).Hash.ToLower() -eq $expected   # should print True
Expand-Archive .\TADA-plugin-<version>.zip -DestinationPath "$HOME\tools\tada-plugin"
codex plugin marketplace add "$HOME\tools\tada-plugin"    # or: claude plugin marketplace add ...
```

Do not delete or move the extracted folder while a host uses it. The alpha.12 ZIP supports Codex only, and its plugin ID is `tada@tada-development`.

### From source

This needs PowerShell 7 as well:

```powershell
git clone https://github.com/tayzer/TADA.git; cd TADA
dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages
$plugin = & ./scripts/New-TadaPlugin.ps1 -Package .artifacts/packages/Tada.Cli.0.1.0-alpha.13.nupkg
codex plugin marketplace add $plugin.marketplaceRoot      # or: claude plugin marketplace add ...
```

### CLI only (no agent workflow)

Use this to drive approvals and verification yourself, or from a different agent or tool.

```powershell
gh release download v0.1.0-alpha.12 -R tayzer/TADA -p "TADA-0.1.0-alpha.12-win-x64.zip"
Expand-Archive .\TADA-0.1.0-alpha.12-win-x64.zip -DestinationPath "$HOME\tools\tada-cli"
```

Then follow the `README.md` inside the extracted folder. It installs `Tada.Cli` as a repository-local .NET tool from the bundled feed. `Tada.Cli` is not on NuGet.org yet. The [quickstart](docs/quickstart.md) covers building the CLI from source.

## First use

1. Open your application folder, or an empty folder for a new project, in your agent host.
2. Start a **fresh** session.
3. Describe the work normally:

   > Build me a simple API for tracking my reading list.

   > Fix the bug where cancelled orders still reserve stock.

   > Upgrade the logging package. No behaviour change.

You do not need to mention TADA, Gherkin or any command. The first time a task needs business verification, the agent runs `tada init` in your repository. Before you approve anything, read what you are approving. The agent prints the approval command; **you** run it:

```powershell
dotnet tool run tada baseline approve --review <review-id> --by "Your Name"
```

The agent will never approve on your behalf. Commit the files under `.tada/`, `features/` and `tests/Acceptance/` along with your code. They are the agreement and its evidence.

To continue the next day, start a fresh session and say something like *"carry on with the reading list API"*. The agent recovers its place from `.tada/context.md`.

## Turn it on and off

TADA installs no global instructions. The plugin is the only switch, so turning the plugin off turns TADA off. Start a **fresh session** after any change.

| I want to… | Codex | Claude Code | Cursor |
| --- | --- | --- | --- |
| Toggle it interactively | `/plugins`, select TADA, press Space | `/plugin` | **Customize** lists installed plugins and their controls |
| Turn it off everywhere | `enabled = false` in `~/.codex/config.toml` | `claude plugin disable tada@tada --scope user` | Remove `~/.cursor/plugins/local/tada` and restart Cursor |
| Turn it on for one repository | `enabled = true` in `<repo>/.codex/config.toml` | `claude plugin enable tada@tada --scope project` | Install from a team marketplace with project scope |
| Skip it for one task | Say so: *"Don't use TADA for this one."* | Same | Same |
| Remove it completely | [Uninstall](#update-or-uninstall) | [Uninstall](#update-or-uninstall) | [Uninstall](#update-or-uninstall) |

### Off by default, on per project

**Codex.** Disable TADA in your user configuration, `~/.codex/config.toml`:

```toml
[plugins."tada@tada"]
enabled = false
```

Then enable it in each repository that should use it, in `<repo>/.codex/config.toml`:

```toml
[plugins."tada@tada"]
enabled = true
```

Codex applies project configuration only in repositories you have marked as trusted.

**Claude Code.** Install with project scope only, in each repository that should use TADA:

```powershell
claude plugin install tada@tada --scope project
```

If you already installed it for your user, run `claude plugin disable tada@tada --scope user` once, then `claude plugin enable tada@tada --scope project` in each repository.

**Cursor.** A plugin in `~/.cursor/plugins/local` applies to every workspace. For per-repository control, install TADA from a team marketplace and choose project scope. Otherwise, remove the local folder when you don't want TADA.

Commit the project-level setting (`.codex/config.toml` or `.claude/settings.json`) if your whole team should get the same default.

### What turning it off keeps

Disabling the plugin stops the agent from following the workflow. **It does not delete anything.** Your approved features, baselines, run evidence and `.tada/context.md` stay in the repository. The `tada` CLI still works, so you can run `tada verify` or `tada gate` by hand or in CI. When you re-enable the plugin, the agent picks up where you left off.

## Optional pieces

Only the plugin is required. Everything else is added when a task needs it. Each piece can be added or removed on its own.

| Piece | What it does | Added by | Remove or skip |
| --- | --- | --- | --- |
| **Plugin** (workflow) | Makes the agent follow discovery, agreement, RED/GREEN and handoff | [Install](#install) | Disable or uninstall the plugin |
| **`tada` CLI** (verifier) | Approves, runs and verifies acceptance baselines | Installed automatically by the plugin, or [by hand](#cli-only-no-agent-workflow) | `dotnet tool uninstall Tada.Cli` in the repository |
| **Acceptance baseline** | Locks the approved Gherkin plus its bindings and fixtures | `tada baseline prepare`, then review and approve | Not needed for chores and refactors with existing checks |
| **Contract and fitness obligations** | Protects agreed API contracts and NFR thresholds, such as latency budgets and architecture rules | `obligations` in `tada.json` | Leave `obligations` out |
| **Several local services** | One acceptance run across several of your .NET services | `execution.services` in `tada.json` (schema 2) | Use a single `sutProject` |
| **Enforcement ledger** *(source)* | Splits approved scenarios into **active** (must pass to merge) and **candidates** (agreed, not built yet) | `.tada/enforcement.json` | Leave out the file. `tada gate` then reports `NO_CHECKS` |
| **Verification plan** *(source)* | Coordinates acceptance, unit, contract and journey checks for a change | `tada plan prepare --file .tada/verification-plan.json` | Leave it out and run `tada run --baseline` directly |
| **CI merge gate** *(source)* | Blocks pull requests when active obligations fail | `tada gate --run <id> --base origin/main` in CI | Leave it out of CI |
| **Knowledge augment mode** *(source)* | Lets the agent propose short, evidence-backed knowledge records when a real gap exists. The default **reference** mode only reuses and updates your existing docs. | `.tada/knowledge.json` with `{ "schemaVersion": 1, "mode": "augment" }` | Delete the file to return to reference mode |
| **Native test runs** | The same scenarios in Visual Studio Test Explorer and `dotnet test` | Generated by `tada init` | Optional; recorded TADA runs work without it |

Items marked *(source)* are on `main` for alpha.13 but are not in the released alpha.12 package.

## Commands

You rarely type these yourself, because the agent runs them. The one exception is `baseline approve`, which only a human should run.

| Command | What it does |
| --- | --- |
| `tada init` | Prepares the repository. It creates files but never overwrites existing ones. |
| `tada baseline prepare` | Captures the current Gherkin, bindings and fixtures as an immutable baseline |
| `tada baseline review --baseline <digest> [--against <old>]` | Shows what you are about to approve, or what changed since an earlier baseline |
| `tada baseline approve --review <id> --by <name> [--replaces <old>]` | **Human only.** Records approval of a reviewed baseline |
| `tada run --baseline <digest> [--dotnet <path>]` | Restores and rebuilds from scratch, runs the scenarios once and keeps all evidence |
| `tada verify --run <id>` | Checks the retained evidence without re-running anything |
| `tada plan prepare --file <path>` *(source)* | Captures a verification plan |
| `tada run --plan <digest>` *(source)* | Runs a captured verification plan |
| `tada gate [--run <id>] [--base <branch>]` *(source)* | Checks the merge obligations in `.tada/enforcement.json` |
| `tada knowledge check` *(source)* | Shows the knowledge mode and flags records whose sources have changed |

Run each one as `dotnet tool run tada …` inside the repository.

**Options for every command:** `--repo <dir>` (defaults to the current directory), `--json` (structured output, no progress lines), `--help` and `--version`.

**Exit codes:** `0` PASS (or `NO_CHECKS` from `gate`), `1` FAIL, `2` INDETERMINATE, `3` command error.

## Configuration

`tada init` writes a working `tada.json`. You only edit it to add optional pieces.

```json
{
  "schemaVersion": 1,
  "oracleRoots": ["features", "tests/Acceptance"],
  "implementationRoots": ["src"],
  "execution": {
    "project": "tests/Acceptance/Acceptance.csproj",
    "assembly": "Acceptance.dll",
    "messagesFile": "tada-messages.ndjson",
    "timeoutSeconds": 120,
    "sutProject": "src/Api/Api.csproj",
    "sutAssembly": "Api.dll"
  }
}
```

The [configuration reference](docs/configuration.md) lists every key with its default, plus the `enforcement.json`, knowledge and host settings.

**Changing `tada.json` or anything under `oracleRoots` changes the agreement.** That includes line-ending changes. You will need to review and approve a new baseline afterwards. This is deliberate.

### Files TADA adds to your repository

| Path | Purpose | Commit it? |
| --- | --- | --- |
| `features/*.feature` | The agreed business behaviour, in Gherkin | Yes |
| `tests/Acceptance/` | Bindings and fixtures that run the features | Yes |
| `tada.json` | Verifier configuration | Yes |
| `.tada/context.md` | Short handoff notes for the next session | Yes |
| `.tada/WORKFLOW.md` | Detailed runner rules for the agent | Yes |
| `.tada/` baselines, reviews and approvals | Immutable approval history | Yes. Never edit them by hand. |
| `.tada/runs/`, `.tada/plan-runs/`, `.tada/scratch/` | Local run output | No. `init` adds them to `.gitignore`. |
| `global.json`, `NuGet.Config`, `Directory.Build.*`, `.gitattributes` | SDK policy, locked restore and exact-byte checkouts | Yes. They are only created when missing. |

On Windows, run `git config core.longpaths true` once in the repository, because snapshot paths can be deep.

## Update or uninstall

**Update:**

| Host | Installed from the TADA marketplace | Installed from a ZIP folder |
| --- | --- | --- |
| Codex | `codex plugin marketplace upgrade tada` | `codex plugin marketplace remove tada`, then add the new folder and run `codex plugin add tada@tada` |
| Claude Code | `claude plugin marketplace update tada` | `claude plugin marketplace remove tada`, then add the new folder and run `claude plugin install tada@tada` |
| Cursor | Marketplace installs update automatically | Replace `~/.cursor/plugins/local/tada` with the new folder, then restart Cursor |

**Moving from alpha.12:** alpha.12 used the plugin ID `tada@tada-development`. Remove it with `codex plugin remove tada@tada-development` and `codex plugin marketplace remove tada-development`, then install from the TADA marketplace. Your project files are unaffected.

A new CLI version can change captured tool bytes. Expect to review and re-approve the baseline once, so the agreement stays honest.

**Uninstall the plugin.** This keeps all project files.

| Host | Uninstall |
| --- | --- |
| Codex | `codex plugin remove tada@tada`, then `codex plugin marketplace remove tada` |
| Claude Code | `claude plugin uninstall tada@tada`, then `claude plugin marketplace remove tada` |
| Cursor | Delete `~/.cursor/plugins/local/tada` and restart Cursor |

**Remove the CLI from a repository:** run `dotnet tool uninstall Tada.Cli` in that repository.

**Remove TADA's records from a repository:** delete `.tada/`, `tada.json` and, if you no longer want them, `features/` and `tests/Acceptance/`.

> **Warning:** Deleting `.tada/` permanently removes the approval and failure history. Commit or archive it first if you might need it again.

## Troubleshooting

**The agent ignores TADA.** Start a fresh session after installing or toggling. Check that the plugin is enabled in your host and that no project setting turns it off. In Claude Code you can also invoke it explicitly with `/tada:tada`.

**Cursor doesn't show TADA.** Fully quit and reopen Cursor. Check that `~/.cursor/plugins/local/tada/.cursor-plugin/plugin.json` exists; a nested extraction folder will not load. Enable **Include third-party Plugins, Skills, and other configs**. Organisation accounts may need an admin to allow local plugin imports.

**`INDETERMINATE` instead of FAIL.** The evidence could not be trusted. Common causes are a build error, a missing step binding, the API failing to start, or a timeout. Read the reasons that `tada verify` prints. INDETERMINATE does not mean your feature is wrong.

**It asks me to approve again after a small edit.** Any byte change under `oracleRoots` is a new agreement, including whitespace and line endings. Review the diff with `tada baseline review --baseline <new> --against <old>` and approve it if it is fine.

**Path too long on checkout.** Run `git config core.longpaths true` in the repository.

**The wrong SDK was picked up.** `global.json` selects the SDK. Install a matching SDK, or pass `--dotnet <path-to-dotnet.exe>` to `tada run`. The host chosen during setup is recorded in `.tada/context.md`.

**Workspace path problems.** Spaces work. Commas and semicolons in the workspace path are not supported in this alpha.

## Limits

- Results are **local advisory L1**. Anyone who controls the machine can forge approvals or evidence. Hash checks prove that evidence matches the approved inputs, not that the assertions are good enough.
- In team CI, approvals added by a pull request need an independent check, such as required code-owner review of the protected files that `tada gate` lists.
- Passing checks do not replace proportionate code review.
- Supported today: Windows x64, .NET 10, Reqnroll, MSTest and the Microsoft Testing Platform, with ASP.NET Core APIs. Scenario Outlines are not supported yet.

The [quickstart](docs/quickstart.md) and [release notes](docs/releases/0.1.0-alpha.12.md) list the full trust boundary.

## Learn more

- [How TADA works](docs/how-it-works.md): the method, evidence-driven discovery, continuous learning and what human approval means
- [Configuration reference](docs/configuration.md)
- [Plugin details](docs/plugin.md) for Codex, Claude Code and Cursor
- [Quickstart](docs/quickstart.md): building from source and running the verifier by hand
- [Native testing](docs/native-testing.md) and [CI and PR workflows](docs/ci-pr-workflows.md)
- [Enforcement guide](docs/enforcement.md), [multi-level verification](docs/multi-level-verification.md) and [discovery and knowledge](docs/knowledge.md)
- [Releasing](docs/releasing.md) and the [directory listing kit](docs/listings.md)
- [Project charter](docs/vision.md), [decision records](docs/decisions/) and [release checklist](docs/release-readiness.md)

Source licence: [MIT](LICENSE). Historical ADRs and probe evidence retain the earlier AAATDD name.

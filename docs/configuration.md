# Configuration reference

This page lists every setting TADA reads. For installation and everyday use, start with the [README](../README.md).

Settings live in three places:

| Where | Controls | Who edits it |
| --- | --- | --- |
| Agent host settings | Whether the TADA workflow is active | You |
| `tada.json` | What the verifier captures, builds and runs | The agent proposes changes. You approve them as part of a baseline. |
| `.tada/enforcement.json` *(alpha.13)* | Which approved scenarios must pass before merge | The agent proposes changes. You review them in the pull request. |
| `.tada/knowledge.json` *(alpha.13)* | Whether the agent may propose new knowledge records | You |

## Host settings

The TADA plugin is the only activation switch. TADA writes no global or repository-wide agent instructions. `tada init` removes recognised generated instruction blocks left by alpha.7 and alpha.8, so they cannot bypass the plugin toggle.

### Codex

The plugin identifier is `tada@tada`.

```toml
# ~/.codex/config.toml (user) or <repo>/.codex/config.toml (trusted project)
[plugins."tada@tada"]
enabled = false   # true to enable
```

Codex reads project configuration only in trusted repositories. A common setup is to disable TADA in the user file and enable it in the project file of each repository that uses TADA; the project setting wins inside that repository. In the Codex CLI, `/plugins` opens the plugin browser, and Space toggles an installed plugin.

### Claude Code *(source; first release in alpha.13)*

```powershell
claude plugin install tada@tada --scope project   # this repository only
claude plugin disable tada@tada --scope user       # off by default for you
claude plugin enable tada@tada --scope project     # on for this repository
```

Project scope is recorded in the repository's `.claude/settings.json`. The skill can also be invoked explicitly as `/tada:tada`.

### Cursor *(source; first release in alpha.13)*

Cursor loads the plugin from `~/.cursor/plugins/local/tada`, which applies to every workspace. Remove the folder and restart Cursor to turn it off. For per-repository control, publish the generated marketplace to a Git repository, import it as a team marketplace (Teams and Enterprise plans), and install TADA with project scope from **Customize**. See [plugin details](plugin.md#install-and-toggle-in-cursor).

### Per task

While the plugin is enabled, you can still tell the agent not to use TADA for a particular request. The skill respects explicit user decisions.

Start a fresh session after changing any host setting. Running sessions keep the skill set they started with.

## `tada.json`

`tada init` creates this file in the repository root. Paths use forward slashes and are relative to the repository root.

The file is part of the captured oracle. Changing it requires a new baseline review and human approval.

### Top-level keys

| Key | Required | Default | Meaning |
| --- | --- | --- | --- |
| `schemaVersion` | Yes | `1` from `init` | `1` for a single application under test. `2` for `execution.services`. |
| `oracleRoots` | Yes | `["features", "tests/Acceptance"]` | Folders captured byte for byte as the protected agreement: features, bindings, fixtures, observers and shared assertion inputs. |
| `implementationRoots` | No | none | Application source folders. They are identified separately from the oracle, so implementation changes never count as acceptance changes. |
| `requirementPrefix` | No | `REQ_` | Gherkin tag prefix for requirement IDs, such as `@REQ_ORDER_LIMIT`. |
| `scenarioPrefix` | No | `AC_` | Gherkin tag prefix for scenario IDs, such as `@AC_ORDER_LIMIT_01`. |
| `obligations` | No | none | Contract and fitness roots. See [Obligations](#obligations). |
| `buildTaskContracts` | No | none | Reviewed declarations for opaque custom build tasks. See [Build task contracts](#build-task-contracts). |

### `execution`

| Key | Required | Default | Meaning |
| --- | --- | --- | --- |
| `project` | Yes | `tests/Acceptance/Acceptance.csproj` | Acceptance harness project. |
| `assembly` | Yes | `Acceptance.dll` | Harness output assembly name. |
| `messagesFile` | Yes | `tada-messages.ndjson` | File name for the native Cucumber Messages output. Use a bare file name with no path. |
| `timeoutSeconds` | No | `120` | Execution deadline, from 1 to 3600 seconds. When the deadline passes, the owned process tree is terminated and the result is INDETERMINATE. |
| `sutProject` | No | none | Schema 1 only. The separate application project to build and launch. |
| `sutAssembly` | No | none | Schema 1 only. The application assembly. The harness receives its path as `TADA_SUT_ASSEMBLY`. |
| `services` | No | none | Schema 2 only. Replaces `sutProject` and `sutAssembly`. An array of `{ "id", "project", "assembly" }` objects. |

Single application (schema 1):

```json
{
  "schemaVersion": 1,
  "oracleRoots": ["features", "tests/Acceptance"],
  "implementationRoots": ["src"],
  "execution": {
    "project": "tests/Acceptance/Acceptance.csproj",
    "assembly": "Acceptance.dll",
    "messagesFile": "tada-messages.ndjson",
    "sutProject": "src/Api/Api.csproj",
    "sutAssembly": "Api.dll"
  }
}
```

Several local services (schema 2), from [`examples/MultiService`](../examples/MultiService/tada.json):

```json
{
  "schemaVersion": 2,
  "oracleRoots": ["features", "Acceptance", "Support"],
  "implementationRoots": ["Services"],
  "execution": {
    "project": "Acceptance/Journey.Acceptance.csproj",
    "assembly": "Journey.Acceptance.dll",
    "messagesFile": "tada-messages.ndjson",
    "timeoutSeconds": 180,
    "services": [
      { "id": "orders", "project": "Services/Orders/Orders.Service.csproj", "assembly": "Orders.Service.dll" },
      { "id": "payments", "project": "Services/Payments/Payments.Service.csproj", "assembly": "Payments.Service.dll" }
    ]
  }
}
```

Fixtures reach each service through `TadaAcceptance.Create().Services["orders"]`. See [multi-level verification](multi-level-verification.md).

### Obligations

Declare agreed API contracts and non-functional thresholds so they are protected and classified like acceptance criteria.

```json
"obligations": [
  { "kind": "contract", "roots": ["tests/Acceptance/contracts"] },
  { "kind": "fitness",  "roots": ["tests/Acceptance/fitness"] }
]
```

- `kind` is `contract` or `fitness`.
- Every root must lie inside `oracleRoots`.
- Baseline review lists changes to these files separately, so a reviewer can see what kind of obligation moved.

Supporting unit tests stay outside `oracleRoots` and need no approval.

### Build task contracts

Builds are observed. Opaque custom MSBuild tasks, shell commands and file-reading evaluation expressions need a reviewed declaration of everything they read:

```json
"buildTaskContracts": [
  { "origin": "build/Custom.targets", "target": "GenerateClient", "task": "Exec", "inputRoots": ["contracts"] }
]
```

For opaque project evaluation, use target `@evaluation` and task `property-functions`. A declaration is a trust contract, not a sandbox. Do not add broad entries just to silence a gap. See [build-task contracts](build-task-contracts.md).

## `.tada/enforcement.json` *(alpha.13)*

TADA works without this file, but `tada gate` requires it. Without it, `tada gate` stops with a command error (exit code 3). With the file present, `tada gate` reports `NO_CHECKS` only when there are no active obligations and no `--run` is supplied. `NO_CHECKS` is not a behavioural pass.

```json
{
  "schemaVersion": 1,
  "baseline": "sha256:<approved baseline digest>",
  "active": ["AC_ORDER_LIMIT_01"],
  "candidates": [
    { "scenario": "AC_ORDER_LIMIT_02", "since": "2026-11-11", "reason": "Agreed; bindings pending" }
  ],
  "policy": { "candidateMaxDays": 30 }
}
```

| Key | Meaning |
| --- | --- |
| `baseline` | The approved baseline that this ledger classifies. |
| `active` | Scenarios that must pass before merge. |
| `candidates` | Approved scenarios that are not implemented yet. They run and are reported, but do not block. `since` cannot be reset. |
| `policy.candidateMaxDays` | Optional. A candidate older than this many days blocks the gate. |

A passing candidate must be moved to `active` in the same change. An active scenario cannot be demoted unless an approved amendment changed it. See the [enforcement guide](enforcement.md).

## `.tada/knowledge.json` *(alpha.13)*

This file is optional. Without it, TADA uses **reference** mode: it reuses and proposes updates to your existing documentation, decisions and history, and never creates standalone knowledge documents.

```json
{ "schemaVersion": 1, "mode": "augment" }
```

**Augment** mode also lets the agent propose one focused, evidence-backed record when a significant finding has no suitable home. Records still go through normal Git review. The file sits outside the captured oracle, so changing the mode never invalidates approved expectations. `tada knowledge check` reports the active mode and flags records whose sources have changed. See [discovery and knowledge](knowledge.md).

## `.tada/verification-plan.json` *(alpha.13)*

This file is optional. It coordinates acceptance, supporting MSTest, contract and journey checks for one change. Capture it with `tada plan prepare --file .tada/verification-plan.json` and run it with `tada run --plan <digest>`. The format is described in [multi-level verification](multi-level-verification.md).

## Command-line options

| Option | Applies to | Meaning |
| --- | --- | --- |
| `--repo <dir>` | All commands | Repository root. Defaults to the current directory. |
| `--json` | All commands | Prints structured JSON on stdout and suppresses progress lines. |
| `--dotnet <path>` | `run` | The `dotnet` host to use. Defaults to `dotnet` on `PATH`. The agent passes the host recorded in `.tada/context.md`. |
| `--against <digest>` | `baseline review` | Compares against an earlier baseline, for amendments. |
| `--replaces <digest>` | `baseline approve` | Approves an amendment that supersedes an earlier baseline. |
| `--base <revision>` | `gate` *(alpha.13)* | Compares with the target branch's committed ledger. Merge checks must pass this option. |
| `--help`, `--version` | All commands | Prints usage or the version. |

Unknown or duplicated options are rejected.

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | `PASS`, or `NO_CHECKS` from `gate`. Prepare, review and approve also return `0` on success. |
| `1` | `FAIL`. An agreed assertion failed. |
| `2` | `INDETERMINATE`. Evidence is missing, invalid or untrustworthy. |
| `3` | Command or configuration error. |

## Environment supplied to the harness

`tada run` gives the acceptance harness a recorded, minimal environment. Ambient MSBuild properties, CLR injection variables and response files are excluded.

| Variable | Meaning |
| --- | --- |
| `TADA_DOTNET_HOST` | The `dotnet` host to use when launching the application. |
| `TADA_RUN_DIRECTORY` | Per-run folder for databases, state and logs. |
| `TADA_REPOSITORY` | Repository root. |
| `TADA_RUN_ID` | Current run ID. |
| `TADA_SUT_ASSEMBLY` | Path to the application assembly (schema 1). |
| `TADA_SERVICES_FILE` | Service map (schema 2). Read it through `TadaAcceptance.Create()`. |

Native `dotnet test` and Visual Studio runs do not need these variables. The generated `TadaAcceptance.Create()` context supplies equivalent values. See [native testing](native-testing.md).

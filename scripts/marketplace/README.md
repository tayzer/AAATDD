# TADA plugin marketplace

This repository is the install source for the **TADA** (Test-Assured Development Assistance) plugin in Codex, Claude Code and Cursor. It contains version `VERSION_PLACEHOLDER` of the plugin. Release automation in [tayzer/TADA](https://github.com/tayzer/TADA) publishes it; do not edit it by hand.

With TADA enabled, your coding agent agrees executable Gherkin acceptance with you before implementing, protects the approved expectations byte for byte, and reports `PASS`, `FAIL` or `INDETERMINATE` from retained evidence. It supports C#/.NET APIs with Reqnroll and MSTest on Windows x64. Results are local advisory (L1).

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

Add `--scope project` to the install command to enable TADA for one repository only.

**Cursor**

Install TADA from the Cursor marketplace, or import this repository as a team marketplace through **Dashboard → Plugins & MCPs → Add Marketplace → Import from Repo**.

Start a fresh session in your application repository and describe the work normally.

## Update

| Host | Command |
| --- | --- |
| Codex | `codex plugin marketplace upgrade tada`, then `codex plugin add tada@tada` |
| Claude Code | `claude plugin marketplace update tada`, then `claude plugin update tada@tada` and restart Claude Code |
| Cursor | Updates arrive through the marketplace |

## Layout

| Path | Purpose |
| --- | --- |
| `.agents/plugins/marketplace.json` | Codex catalog |
| `.claude-plugin/marketplace.json` | Claude Code catalog |
| `.cursor-plugin/marketplace.json` | Cursor catalog |
| `tada/` | The shared plugin: skill, setup helper, matching verifier package and manifests for each host |

Full documentation, turning TADA on and off, configuration and the trust boundary are in the [TADA repository](https://github.com/tayzer/TADA).

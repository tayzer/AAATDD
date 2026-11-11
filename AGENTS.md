# Working on TADA

TADA is under active implementation. The user's autonomous-release goal authorises routine engineering decisions, code, tests and documentation. Preserve the first 40 accepted ADRs and original Orders feature as historical/accepted records; later instructions and implementation decisions can supersede their scope without rewriting them.

Read `README.md`, `docs/quickstart.md` and `docs/release-readiness.md`. [ADR 0043](docs/decisions/0043-adopt-the-tada-north-star.md) records the North Star that guides architecture decisions; the [North Star assessment](docs/architecture/north-star-assessment.md) holds the current gap analysis and roadmap. Design-stage documents remain historical context and must not override the newer implementation authority in ADR 0041.

## Development and validation

- Target .NET SDK 10.0.403 / net10.0. Restore locked graphs for validation. Keep dependencies minimal and record changed graphs deliberately.
- Core and CLI are under `src/`; MSTest/MTP focused and real-Message replay tests are under `tests/Tada.Core.Tests/`.
- Run `dotnet build tests/Tada.Core.Tests/Tada.Core.Tests.csproj -c Release`, then `dotnet tests/Tada.Core.Tests/bin/Release/net10.0/Tada.Core.Tests.dll --progress off`.
- Pack with `dotnet pack src/Tada.Cli/Tada.Cli.csproj -o .artifacts/packages`.
- Run `pwsh -File scripts/Test-OrdersWorkflow.ps1` for real HTTP/SQLite business, integrity and invalid-evidence controls. The script uses a disposable copy.
- Run `pwsh -File scripts/Test-EnforcementWorkflow.ps1` for the candidate/active enforcement lifecycle in a disposable Git repository with real Orders runs. Its approvals are labelled automated controls.
- Run `pwsh -File scripts/Test-Onboarding.ps1` for fresh-package-cache installation, init, a second API, maintenance checks and context handoff. It needs NuGet network access and local process/loopback permissions.
- Some restricted Windows sandboxes block Reqnroll task-host startup. Retain that failed result and rerun unchanged sources with the required permission; never report it as behavioural RED.
- Git's MSYS helper and atomic directory renames in sandbox-managed temporary folders can also be restricted. `TADA_TEST_ROOT` can place owned tests under `.artifacts/`; run the Git test with the required permission rather than skipping it or weakening snapshot publication.

## Product invariants

Business Gherkin needs human agreement. Never approve expectations on a human's behalf. Automated fixtures may create approvals only when explicitly labelled as controls in isolated test projects; that is not requester acceptance.

Hash captured oracle files exactly, preserve immutable baseline/review/approval records, and retain failures and gaps. Native runner success alone is not TADA PASS. Keep PASS, FAIL and INDETERMINATE distinct and report local advisory L1 limits.

Chores and refactors use proportionate checks without invented business scenarios. Preserve useful context in concise Git-native records. Do not turn supporting Markdown into a second business specification.

Keep unrelated dirty work intact. Do not publish packages, merge or change remote access merely to make a checklist green. The final user/Codex install-to-API acceptance is separate from automated validation.

The user explicitly authorised ongoing TADA code, test, documentation and CI updates on `codex/alpha-release-validation` for draft PR #1. Continue scoped delivery and CI repair there without repeated approval questions. [ADR 0041](docs/decisions/0041-authorise-autonomous-release-implementation.md) records the grant; merge and release/package publication remain separate gates.

## Communication

Use concise caveman-style chat by default. Keep persisted documentation, code and warnings in clear normal prose.

# TADA

**Test-Assured Development Assistance**: human-approved Gherkin business specifications, protected acceptance criteria, and independent verification in small increments. Git is the intended authority for project knowledge. Formerly AAATDD; the current alpha is explicitly L1/advisory.

**Alpha `0.1.0-alpha.12`, Windows x64, local advisory L1.** The CLI supports onboarding, exact-byte baseline capture, review/approval, real Reqnroll/MSTest/MTP execution and retained-evidence verification. The Orders example runs a separate ASP.NET Core API with disposable scenario databases. See the [release notes](docs/releases/0.1.0-alpha.12.md) and [candidate validation](docs/validation/release-candidate-12.md) for current scope and publication gates.

Start with the [quickstart](docs/quickstart.md) to build and install a repository-local tool package and use the workflow with Codex or another agent. No model API or agent SDK is required. The [release checklist](docs/release-readiness.md) records what still needs validation. No public package release is claimed.

For a packaged preview without compiling TADA, see [development download bundles](docs/release-bundles.md). Bundles contain the tool package, installation/agent-start guidance, licence notices and checksums.

The [alpha.7 checkpoint](docs/validation/increment-07.md) records 111 passing focused tests and 46 Orders controls. Installed-package development demonstrates RED/GREEN/refactor, stable unchanged-rerun identities, preserved real failure history and task-cache byte drift producing a distinct environment. Cancellation retains diagnostics and terminates owned process trees. See the [provenance audit](docs/validation/provenance-audit.md) and [build-task contracts](docs/build-task-contracts.md) for the explicit trust boundary. These are automated controls, not requester acceptance or proof of complete execution-input discovery. The [alpha.6 checkpoint](docs/validation/increment-06.md) and earlier checkpoints remain historical evidence.

The initial supported stack is Windows x64/.NET 10 with Reqnroll/MSTest/MTP. The active [autonomous-release instruction](docs/decisions/0041-authorise-autonomous-release-implementation.md) authorises implementation and routine decisions, superseding the earlier design-only boundary. Human business acceptance, preserved oracle integrity and honest L1 claims still apply.

The verifier uses C#/.NET 10, packaged as the `Tada.Cli` repository-local .NET tool. Source licence: [MIT](LICENSE). [Hosted Windows validation](docs/validation/hosted-ci.md) records package installation, API controls and redistribution checks for its historical source commit. The requester accepted ordinary installation, API development, plugin activation and fresh-chat recovery on 2026-11-09. Final publication validation and authority remain tracked in the release checklist.

Install the [TADA plugin](docs/plugin.md) and use the host's normal plugin controls to enable or disable the workflow. Describe your project or change normally in a fresh session. The plugin supplies agent behaviour and the repository-local CLI supplies verification. `tada init` prepares project files and migrates recognised legacy instruction blocks. The [ordinary-request trial](docs/validation/default-workflow-trial.md) covers enabled, disabled and re-enabled sessions. The CLI provides `tada baseline prepare`, `tada baseline review`, `tada baseline approve`, `tada run` and `tada verify`. Chores and refactors use proportionate existing checks. Store useful session handoff in `.tada/context.md`. Historical ADRs and probe evidence retain the earlier AAATDD name.

## Design documents

- [Project charter](docs/vision.md)
- [Comparative research](docs/research/initial-landscape.md)
- [Proposed AI change lifecycle](docs/architecture/change-lifecycle.md)
- [Architecture update reconciliation](docs/architecture/2026-10-26-design-reconciliation.md)
- [Execution boundaries and reuse research](docs/research/execution-boundaries.md)
- [Initial sample business specification](docs/requirements/order-stock-validation.feature)
- [First experiment executable-baseline proposal](docs/architecture/first-experiment-baseline-proposal.md)
- [Completed technical compatibility probe](docs/research/compatibility-probe-results.md)
- [Alpha delivery readiness and evidence gaps](docs/architecture/alpha-delivery-readiness.md)
- [Proposed alpha file inventory and path handling](docs/architecture/alpha-file-inventory-proposal.md)
- [Open questions and risks](docs/open-questions.md)

Routine engineering decisions are delegated by the active goal. Agents must not silently weaken approved acceptance criteria or claim release readiness merely because internal tests pass.

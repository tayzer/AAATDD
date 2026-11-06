# First Reqnroll test execution framework

Researched: 2026-11-03. Status: **Research findings** and **Proposal**. No packages were installed and no compatibility or runtime behaviour was tested. ADR 0022 selects C#/.NET and Reqnroll, not the underlying test execution framework.

## Findings

- The [Reqnroll setup guide](https://docs.reqnroll.net/latest/installation/setup-project.html#choosing-your-test-execution-framework) lists MSTest, NUnit, TUnit and xUnit. It has no universal favourite and recommends NUnit when no preference exists. Its console project template defaults to NUnit. This is upstream guidance, not an AAATDD decision.
- The [NUnit integration guide](https://docs.reqnroll.net/latest/integrations/nunit.html) lists `Reqnroll.NUnit`, `NUnit`, `NUnit3TestAdapter` and `Microsoft.NET.Test.Sdk` for its documented discovery/execution route. Package names do not establish a compatible pinned dependency graph; version and test-host selection remain necessary.
- The [parallel-execution guide](https://docs.reqnroll.net/latest/execution/parallel-execution.html) distinguishes scheduling from isolation. In-process parallel execution shares some core services and requires care with static state and feature-level lifecycle. Scenario-scoped components alone do not prove safe concurrent execution.

These pages use the moving `latest` documentation route. Recheck behaviour against the exact selected package versions before executable-baseline approval. No claim is made about the latest available versions or a tested compatibility matrix.

## Initial proposal: not selected

Use NUnit for the first harness and explicitly disable scenario parallelism for the initial experiment. This follows the upstream no-preference recommendation and keeps the first evidence/fixture/teardown exercise small. Both scenarios remain independently set up and must not rely on execution order or shared state. Serial execution does not replace per-scenario ownership or establish L2 isolation.

MSTest, xUnit and TUnit remain alternatives; this recommendation does not claim they are unsuitable. Revisit parallel execution when there is a concrete need and its fixture, SUT, discovery and evidence behaviour can be validated. Preserve ADR 0010's no-automatic-retry policy regardless of the selected framework.

Exact versions, test-host choice, adapter configuration, assertion API and package lock graph remain unapproved. This proposal selects no concrete project files and authorises no installation, restore or execution.

**Outcome:** The user subsequently selected MSTest and MTP in [ADR 0023](../decisions/0023-use-mstest-with-microsoft-testing-platform.md), then serial scenario execution in [ADR 0024](../decisions/0024-run-initial-scenarios-serially.md). The NUnit recommendation above is retained as historical reasoning, not the current direction. See the [MSTest/MTP follow-up](2026-11-03-mstest-mtp.md) for current integration research.

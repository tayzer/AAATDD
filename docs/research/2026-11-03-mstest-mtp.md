# MSTest and MTP integration for the first harness

Researched: 2026-11-03. Status: **Research findings** and **Open compatibility work**. The user selected MSTest with MTP in ADR 0023. No packages were installed, restored, built or executed.

## Findings from official documentation

- The [Reqnroll MSTest guide](https://docs.reqnroll.net/latest/integrations/mstest.html) documents its MSTest integration through `Reqnroll.MSTest`. It states MSTest v4 support starts with Reqnroll v3.2. Its package list also includes the traditional test-adapter/Test SDK route; that list alone is not a reviewed MTP project configuration.
- Microsoft's [MSTest execution guide](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-running-tests) documents MTP support through the MSTest runner, available since MSTest 3.2.0. It documents `MSTest.Sdk` as one MTP-enabled project route, with manual configuration also possible. Choosing MSTest and MTP does not itself select either SDK arrangement or a version.
- The [.NET test command documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test) distinguishes MTP and VSTest modes. Dedicated MTP selection through `global.json` requires .NET 10 SDK or later; the documented native `dotnet test` MTP experience requires MTP 1.7 or later. Command-line options differ by platform. These are documentation constraints, not an approved version set or invocation.

These moving documentation pages were checked on the research date. No claim is made about latest package releases or a tested end-to-end Reqnroll/MSTest/MTP combination.

## Required compatibility review

Select one coherent pinned SDK/package graph and an explicit MTP launch path. Review Reqnroll generation/build inputs, MSTest discovery and scenario identities, scenario hook/context lifetimes, native Cucumber Messages output/completeness, process/evidence status handling and required teardown. Preserve ADR 0010's no-automatic-retry policy. Do not copy VSTest flags, adapters or configuration into the MTP path by assumption.

The retained Messages stream and AAATDD envelope remain required under ADR 0021. A successful MTP exit or a generic platform report cannot establish approved coverage by itself. MTP supplies no L2 isolation guarantee.

## Subsequent scheduling decision

The recommendation to disable scenario parallelism was subsequently accepted in [ADR 0024](../decisions/0024-run-initial-scenarios-serially.md). Both scenarios must still have independent setup and no order dependency. The user's earlier MSTest/MTP selection did not by itself settle scheduling. [ADR 0025](../decisions/0025-use-mstest-sdk-for-acceptance-harness.md) subsequently selects `MSTest.Sdk` with `Reqnroll.MSTest`. Exact scheduling configuration, package versions and CLI invocation remain unapproved.

## Target-framework follow-up

Checked 2026-11-03: Microsoft's [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy) lists .NET 10 as active LTS through 2028-11-14, with .NET 8 and 9 support ending on 2026-11-10. Reqnroll's [compatibility page](https://docs.reqnroll.net/latest/installation/compatibility.html) lists .NET 10. The user subsequently accepted `net10.0` with a stable .NET 10 SDK for both sample projects in [ADR 0026](../decisions/0026-target-net10-for-first-sample.md). These findings do not select an exact SDK/runtime patch, prove package compatibility or establish the future AAATDD framework/verifier target.

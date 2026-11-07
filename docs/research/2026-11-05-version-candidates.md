# First experiment: direct-version candidates

Researched: 2026-11-05. Status: direct targets **ACCEPTED** in ADR 0031, with sourced availability/dependency findings. At this research checkpoint no SDK/packages had been downloaded or executed. The later separately authorised [probe report](compatibility-probe-results.md) records an actual candidate restore/build/run and native identities. Neither this research nor the probe approves the Orders executable baseline.

## Accepted direct validation targets

| Input | Accepted target | Evidence and limit |
| --- | --- | --- |
| Stable .NET SDK | `10.0.403` | Microsoft's [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) lists it with included .NET and ASP.NET Core runtimes `10.0.14`; actual process runtime selection still needs verification |
| Harness project SDK | `MSTest.Sdk` `4.5.2` | [Versioned NuGet listing](https://packages.nuget.org/packages/MSTest.Sdk/4.5.2); SDK-imported package selections must be inspected, not inferred from the package's empty conventional dependency list |
| Reqnroll integration | `Reqnroll.MSTest` `3.3.4` | [Versioned NuGet listing](https://www.nuget.org/packages/Reqnroll.MSTest/3.3.4) requires Reqnroll and generation packages at least `3.3.4`; aligned core/generator `3.3.4` is the accepted target, subject to graph review |
| SQLite provider | `Microsoft.Data.Sqlite` `10.0.14` | [Versioned NuGet listing](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.14); package availability is not evidence of native-library identity or tested behaviour |

These are the accepted starting targets for direct inputs, not a claim that similarly numbered releases imply compatibility. Approval comes from ADR 0031, not an upstream listing; no latest-version guarantee is made. Recheck release/maintenance information before selecting the executable baseline if time has elapsed.

## Findings that prevent treating this as a complete version set

- The [Reqnroll core `3.3.4` dependency listing](https://www.nuget.org/packages/Reqnroll/3.3.4) declares minimum dependencies including `Cucumber.Messages >= 30.1.0` and `Gherkin >= 35.0.0`. Minimum constraints do not identify the actual restored versions or prove that the chosen Messages schema/consumer is compatible.
- Microsoft.Data.Sqlite `10.0.14` declares `Microsoft.Data.Sqlite.Core >= 10.0.14` and SQLitePCLRaw bundle/core dependencies `>= 2.1.13`. Resolve and review the entire managed/native graph and selected platform assets; the native SQLite engine version is not established by these minimums.
- The [testfx `v4.5.2` version source](https://github.com/microsoft/testfx/blob/v4.5.2/eng/Versions.props) identifies release prefixes `4.5.2` for MSTest and `2.5.2` for MTP, alongside separate build-dependency properties. This is supporting source evidence, not proof of what a shipped SDK resolves. Inspect shipped SDK inputs and the actual lock graph before declaring exact MSTest/MTP alignment.
- Reqnroll documentation's general MSTest v4 support does not by itself validate this exact SDK, MTP launch mode, generated-test discovery, hook lifecycle and native-message combination.

## Review before executable-baseline approval

Review project-SDK imports, all exact transitive versions, package sources/integrity, selected runtime/native assets, SDK/runtime identity and commands. Demonstrate discovery of both required cases, serial independent execution, no automatic retries, complete native messages, the AAATDD envelope, fixture readiness, observer limits and required teardown under the actual MTP path. No silent fallback to VSTest is acceptable under ADR 0023.

Generating lock files or running this validation requires separately authorised implementation/preparation work; the project remains design-only. Approval of the direct targets does not approve the unresolved graph, observer exception or runnable baseline. Unexpected incompatibility returns for an explicit design amendment.

**Outcome:** The user accepted direct targets in [ADR 0031](../decisions/0031-select-direct-version-validation-targets.md), then separately authorised the [bounded probe](../architecture/compatibility-probe-plan.md). Its [completed results](compatibility-probe-results.md) record the candidate graph and technical controls. That runtime authority came from the later explicit probe approval, not the version decision; broader implementation remains unauthorised.

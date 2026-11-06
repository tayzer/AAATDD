# Retained compatibility candidate: licence inventory

Status: **RESEARCH FINDINGS — NOT DEPENDENCY OR RELEASE APPROVAL**. This is a static inspection of the already downloaded compatibility candidate. No restore, build, probe run, package installation or product implementation was performed.

## Scope and retained evidence

The [candidate lock](../../experiments/compatibility-probe/packages.lock.json) has 24 packages for `net10.0`. The separately resolved MSTest project SDK adds one inspected package. The [machine-readable inventory](data/compatibility-candidate-licence-inventory.json) records each package ID/version, its current raw archive SHA-512, declared licence, copyright/repository metadata and notice-file hashes.

Inspection read each `.nuspec` directly from the retained `.nupkg`, checked its ID/version, and compared the raw archive SHA-512 with the cache's `.nupkg.sha512` sidecar. All 25 matched their current sidecars. The MSTest SDK raw archive also matched its hash in the prior [dependency inventory](../../experiments/compatibility-probe/evidence/dependency-inventory.json). For the 24 graph packages, version and `contentHash` matched between the retained lock, prior inventory and current cache `.nupkg.metadata`.

These checks associate the research with the observed candidate; cache consistency is not publisher authentication or a complete package-signature validation. No new package source was contacted, and no package-provided code was executed.

## Declared package licences

| Declared licence expression | Count | Packages |
| --- | --- | --- |
| MIT | 18 | Microsoft.Data.Sqlite and Core; MSTest SDK, framework, adapter and analyzers; Microsoft.Testing.Platform and its MSBuild integration; telemetry and TRX abstractions; Microsoft.TestPlatform.ObjectModel; Microsoft.ApplicationInsights; Microsoft.Bcl.AsyncInterfaces; Microsoft.Extensions.DependencyModel; Cucumber expressions, HTML formatter and Messages; Gherkin. |
| BSD-3-Clause | 3 | Reqnroll, Reqnroll.MSTest and Reqnroll.Tools.MsBuild.Generation. |
| Apache-2.0 | 4 | SQLitePCLRaw bundle, core, native `lib.e_sqlite3` package and provider. |

The exact names and versions are in the JSON inventory. These are declarations in the inspected package metadata, not a conclusion that a final TADA release is compliant or that every bundled component uses the package's headline licence.

The native SQLitePCLRaw package declares Apache-2.0. Do not replace that package-level declaration with a general statement about SQLite source licensing; assess the actual redistributed native payload and accompanying material.

## Bundled notices and toolchain separation

The filename inspection found nine embedded licence/notice files:

- Reqnroll carries five MIT licence files for Cucumber expressions, HTML formatter, Messages, React components and Gherkin. The React-components notice demonstrates why NuGet package names alone are not a complete bundled-component inventory.
- Microsoft.Bcl.AsyncInterfaces and Microsoft.Extensions.DependencyModel each carry `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`.

The inventory hashes those nine archive entries. A package with no matching filename is not established to have no notice obligations: the filename filter is a bounded enumeration aid, not an exhaustive content/licence analyser.

The retained .NET SDK distribution separately contains `LICENSE.txt` headed **MICROSOFT SOFTWARE LICENSE TERMS / MICROSOFT .NET LIBRARY**, plus `ThirdPartyNotices.txt`. Their paths and current hashes are recorded separately. The project SDK package's MIT declaration does not substitute for the .NET SDK distribution terms or third-party notices. Whether toolchain components are redistributed, rather than prerequisites installed by the user, depends on the final package and installation design.

## Hash distinction discovered during inspection

The initial inspection attempted to equate raw archive SHA-512 with NuGet lock `contentHash`; the first package did not match. Further inspection showed that all 24 graph archives match their current raw-hash sidecars, while cache metadata consistently matches the different lock identities. The archives contain signature entries. The corrected research records the two hash identities separately rather than reporting raw archive hashes as verified against the lock.

This inspection has not independently recomputed the NuGet lock-content hashing algorithm or verified package signatures. The observed distinction alone establishes neither corruption nor the cryptographic relationship between those identities. A future provenance contract must specify which hash represents which object and how the association is validated; matching an arbitrary digest field is insufficient.

## Remaining release work

1. Select TADA's own project licence separately; this inventory does not select one.
2. Review the actual product, harness and sample dependency graphs, including new verifier dependencies such as any canonical JSON implementation. The compatibility graph is only a candidate runner graph.
3. Determine the exact release payload and its dependency delivery model. Distinguish development-only inputs, packages restored by consumers and files actually redistributed.
4. Collect the applicable full licence/copyright texts and notices, including embedded components, and prepare release-specific attribution material. A JSON list of expressions is not that material.
5. Recheck the final graph and release artifact identities in CI and the clean-machine installation path. Preserve toolchain terms separately where applicable.

No full dependency graph, licensing compatibility conclusion or publication approval follows from this report. The [alpha readiness audit](../architecture/alpha-delivery-readiness.md) therefore continues to mark publishable artifacts and notices incomplete.

Related: [probe results](compatibility-probe-results.md), [verifier packaging decision](../decisions/0033-use-dotnet-local-tool-for-alpha-verifier.md), [open questions](../open-questions.md).

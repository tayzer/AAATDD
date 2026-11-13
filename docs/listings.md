# Directory listing kit

This page holds what you need to list TADA in the Cursor marketplace, the Anthropic plugin directory and the OpenAI plugin directory. Each directory reviews submissions manually. Submit only after the [marketplace repository](releasing.md#one-time-setup-done) contains a published build.

## Shared listing content

| Field | Value |
| --- | --- |
| Name | `tada` |
| Display name | TADA |
| Short description (30 characters or fewer) | Test-assured development |
| Description | Your coding agent agrees what success means with you before it writes code, then proves the result against that agreement. |
| Category | Productivity (developer tools where offered) |
| Keywords | testing, gherkin, acceptance, dotnet, tdd |
| Publisher | TADA contributors |
| Website and support | https://github.com/tayzer/TADA (issues for support) |
| Licence | MIT |
| Icon | `plugins/tada/assets/icon.png`, 1024 × 1024 PNG |
| Source | https://github.com/tayzer/tada-plugins (plugin folder `tada`) |

**Long description**

> TADA (Test-Assured Development Assistance) makes your coding agent agree what success means before it writes code. For new or changed business behaviour, the agent investigates the existing system, asks the questions that change acceptance and proposes Gherkin examples for you to approve. Approved expectations are captured byte for byte, so the agent cannot quietly weaken a test to make it pass. Implementation proceeds in small steps from a genuine failing check, and results are reported as PASS, FAIL or INDETERMINATE from retained evidence. Chores and refactors reuse existing checks without invented scenarios. Supports C#/.NET APIs with Reqnroll and MSTest on Windows x64. Results are local advisory (L1).

**What it runs (disclosure)**

The same text is in the plugin README, which the Anthropic directory shows as the listing description.

- One skill. No hooks, MCP servers or connectors. TADA itself sends no telemetry and contacts no TADA service.
- When verification is needed, a bundled PowerShell 7 script installs the version-matched `Tada.Cli` .NET tool from the plugin's own `feed/` folder as a repository-local tool, then runs `tada init`.
- `tada run` restores and builds the user's project with `dotnet`. Restore downloads that project's own NuGet dependencies from nuget.org. Tests run locally, and evidence is stored in the repository under `.tada/`.
- The bundled `feed/Tada.Cli.<version>.nupkg` is compiled from the public source in `tayzer/TADA`. Each release's `SHA256SUMS` and `release-manifest.json` identify its exact bytes and the validation run.

**Data handling answers**

| Question | Answer |
| --- | --- |
| Reads or stores personal data? | It can. TADA reads the user's repository, and it keeps byte-exact local snapshots of acceptance files, test inputs and run evidence in that repository. Any personal data in those files, such as names in acceptance examples or test data, is read and retained there. TADA itself sends none of it to TADA or any other service; see the next row for the network requests the .NET SDK makes. |
| Sends data to services other than declared connectors? | TADA itself contacts no service. The .NET SDK that TADA runs makes its usual network requests: restore requests package metadata and packages from the NuGet sources in the user's `NuGet.Config` (nuget.org by default), and the SDK sends usage telemetry unless the user has opted out (for example with `DOTNET_CLI_TELEMETRY_OPTOUT=1`). No repository content is uploaded. |
| Data retention | Evidence stays in the user's repository until the user deletes it. TADA keeps nothing elsewhere. |
| Intended for people under 18? | No; it is a professional developer tool. |

## Anthropic plugin directory (Claude)

Submit at [claude.ai/directory/manage](https://claude.ai/directory/manage): **Submit new → Plugin bundle**. You need a paid Claude plan, a role that can submit, and your GitHub account connected on claude.ai with push access to `tayzer/tada-plugins`. See [Submit your plugin](https://claude.com/docs/plugins/submit).

- [ ] Repository `tayzer/tada-plugins`, plugin path `tada`.
- [ ] Track the `main` branch, which receives every published build, or a specific release tag.
- [ ] Run **Validate**. Run `claude plugin validate ./tada` locally first; the release workflow's package already passes it with no findings.
- [ ] Expect a **policy hold** for reviewer inspection, not a block. `feed/Tada.Cli.<version>.nupkg` is a binary larger than 256 KiB, and the setup script installs a package. In review notes, point to the source repository, the release manifest and the disclosure above.
- [ ] Enter the data handling answers above.
- [ ] Choose **GitHub push webhook** so that new releases are picked up promptly.

## OpenAI plugin directory (Codex)

Submit at [platform.openai.com/plugins](https://platform.openai.com/plugins): **Upload new or existing plugin**. You need organisation owner rights, or **Apps Management Write**, and a verified publisher identity under organisation settings. See [Submit plugins](https://developers.openai.com/codex/submit-plugins).

- [ ] Upload `TADA-plugin-<version>.zip` from the GitHub release. The root `plugin.json` carries the OpenAI interface under `extensions.com.openai.interface`, which the upload reads, and `.codex-plugin/plugin.json` carries the identical interface for Codex-format readers. Both contain every required field (`displayName`, `shortDescription`, `longDescription`, `developerName`, `category`, `capabilities`, `composerIcon` and `logo`). The release workflow checks these fields and fails if the two interfaces differ.
- [ ] Fix any automated findings. As a skills-only plugin, it needs no MCP test cases or listing URLs, but `websiteURL` is set.
- [ ] Choose the verified developer identity, submit for review and publish after approval.
- [ ] Upload each new release's ZIP to update the listing.

## Cursor marketplace

Submit at [cursor.com/marketplace/publish](https://cursor.com/marketplace/publish). Cursor reviews every plugin manually. See [Cursor plugins](https://cursor.com/docs/plugins/building).

- [ ] Submit the public repository `https://github.com/tayzer/tada-plugins`. Its root `.cursor-plugin/marketplace.json` lists `tada`, and `tada/.cursor-plugin/plugin.json` carries the name, version, description, licence and logo.
- [ ] Mention that the plugin is Windows-only and needs PowerShell 7 and a .NET 10 SDK.
- [ ] After approval, users install from **Customize** with project or user scope. Update the README install section from "once its listing is approved" to a direct link.

## Before each submission

- [ ] The release is published and `tayzer/tada-plugins` contains it.
- [ ] The release notes describe any change in what the plugin runs or contacts.
- [ ] The icon still meets each directory's rules: square, 512 px or larger, PNG, under 2 MB.

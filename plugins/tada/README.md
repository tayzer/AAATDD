# TADA plugin

The packaged plugin includes its matching verifier package and marketplace catalogs. It works with Codex, Claude Code and Cursor. Requires Windows x64, a stable .NET 10.0 SDK and Git for Windows. Results are local advisory L1.

Extract the plugin ZIP to a persistent directory, then install it in your agent host.

## Codex

From the extracted directory:

```powershell
codex plugin marketplace add (Get-Location).Path
codex plugin add tada@tada-development
```

In Codex CLI, `/plugins` opens the plugin browser, and Space turns the installed plugin on or off.

## Claude Code

From the extracted directory:

```powershell
claude plugin marketplace add (Get-Location).Path
claude plugin install tada@tada-development --scope user
```

Use `--scope project` to install for the current repository only. `claude plugin disable tada@tada-development` and `claude plugin enable tada@tada-development` turn it off and on.

## Cursor

Copy the extracted directory to `~/.cursor/plugins/local/tada`, keeping its `feed` folder, then quit and reopen Cursor. TADA appears under **Customize**. Remove the folder and restart Cursor to turn it off.

## Use

Start a fresh chat in the application repository and describe the project or change normally. The skill prepares the verifier when business verification needs it. Human agreement and approval remain required for business acceptance. Start a fresh chat after turning the plugin off or on.

Keep the extracted directory available while a host uses it. Source checkouts build the package feed and catalogs with `scripts/New-TadaPlugin.ps1` before installation.

Read the installation guide, configuration reference and release notes in the [repository](https://github.com/tayzer/TADA).

param(
    [Parameter(Mandatory)][string]$Workspace,
    [Parameter(Mandatory)][string]$Baseline,
    [Parameter(Mandatory)][string]$ReferenceRun,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$workspaceRoot = (Resolve-Path -LiteralPath $Workspace).Path
$ownedRoot = [IO.Path]::GetFullPath((Join-Path $repository '.artifacts/onboarding')) + [IO.Path]::DirectorySeparatorChar
if (!$workspaceRoot.StartsWith($ownedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Tool-cache mutation is restricted to an owned onboarding fixture.' }
$cache = Join-Path $workspaceRoot 'tool-cache'
$reference = Get-Content -LiteralPath (Join-Path $workspaceRoot ".tada/runs/$ReferenceRun/run.json") -Raw | ConvertFrom-Json
$approval = Get-Content -LiteralPath (Join-Path $workspaceRoot ".tada/approvals/$($reference.approval).json") -Raw | ConvertFrom-Json
if ($reference.baseline -ne $Baseline -or $approval.by -ne 'automated onboarding control; not human acceptance') { throw 'Expected an explicitly automated onboarding control baseline.' }
$task = (Resolve-Path -LiteralPath (Join-Path $cache 'reqnroll.tools.msbuild.generation/3.3.4/build/netstandard2.0/Reqnroll.Tools.MsBuild.Generation.dll')).Path
for ($currentPath = $task; $null -ne $currentPath; $currentPath = [IO.Path]::GetDirectoryName($currentPath)) {
    if (([IO.File]::GetAttributes($currentPath) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'The private task control cannot modify a linked location.' }
    if ($currentPath -eq $workspaceRoot) { break }
}
$before = [IO.File]::ReadAllBytes($task)
$beforeDigest = 'sha256:' + [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($before))
$referenceTasks = Get-Content -LiteralPath (Join-Path $workspaceRoot ".tada/runs/$ReferenceRun/task-inputs.json") -Raw | ConvertFrom-Json
if (!($referenceTasks.inputs | Where-Object { $_.path -eq $task -and $_.digest -eq $beforeDigest -and 'task-assembly' -in $_.uses })) { throw 'The reference run did not observe the intended task assembly bytes.' }

function WriteTaskBytes([byte[]]$Bytes) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($true) {
        try { [IO.File]::WriteAllBytes($task, $Bytes); return }
        catch [IO.IOException] { if ($watch.Elapsed.TotalSeconds -ge 10) { throw }; Start-Sleep -Milliseconds 100 }
        catch [UnauthorizedAccessException] { if ($watch.Elapsed.TotalSeconds -ge 10) { throw }; Start-Sleep -Milliseconds 100 }
    }
}

$previousCache = $env:NUGET_PACKAGES
$previousCliHome = $env:DOTNET_CLI_HOME
$env:NUGET_PACKAGES = $cache
$env:DOTNET_CLI_HOME = Join-Path $workspaceRoot 'dotnet-home'
Push-Location $workspaceRoot
try {
    # A PE overlay changes the actual task file's exact bytes while preserving executable
    # behaviour. This tests observed cache-byte identity, not an artificial package version bump.
    [byte[]]$changed = $before + [Text.Encoding]::ASCII.GetBytes('TADA_PRIVATE_TASK_BYTE_IDENTITY_CONTROL')
    WriteTaskBytes $changed
    $changedDigest = 'sha256:' + [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($changed))
    $output = & $Dotnet tool run tada run --baseline $Baseline --dotnet $Dotnet --json
    $exitCode = $LASTEXITCODE
    $result = ($output -join "`n") | ConvertFrom-Json
    if ($exitCode -ne 0) { throw "Private task-byte control expected PASS: $($output -join "`n")" }
    $current = Get-Content -LiteralPath ".tada/runs/$($result.runId)/run.json" -Raw | ConvertFrom-Json
    $tasks = Get-Content -LiteralPath ".tada/runs/$($result.runId)/task-inputs.json" -Raw | ConvertFrom-Json
    if (!($tasks.inputs | Where-Object { $_.path -eq $task -and $_.digest -eq $changedDigest -and 'task-assembly' -in $_.uses })) { throw 'The actual task journal did not identify the changed task bytes.' }
    if ($current.implementation -ne $reference.implementation -or $current.environment -eq $reference.environment) { throw 'Task-only cache-byte drift failed to distinguish the environment while preserving implementation source identity.' }
    $report = [pscustomobject]@{referenceRun=$ReferenceRun;run=$result.runId;outcome=$result.outcome;task=$task;originalDigest=$beforeDigest;changedDigest=$changedDigest;sameImplementation=$true;differentEnvironment=$true;originalEnvironment=$reference.environment;changedEnvironment=$current.environment;limitation='Private cache PE overlay changes file identity while preserving task behaviour; it does not authenticate NuGet package bytes.'}
} finally {
    try { WriteTaskBytes $before }
    finally { $env:NUGET_PACKAGES = $previousCache; $env:DOTNET_CLI_HOME = $previousCliHome; Pop-Location }
}
$restoredDigest = 'sha256:' + (Get-FileHash -LiteralPath $task -Algorithm SHA256).Hash.ToLowerInvariant()
if ($restoredDigest -ne $beforeDigest) { throw 'Private task-byte control did not restore the original cache bytes.' }
$report | Add-Member -NotePropertyName originalBytesRestored -NotePropertyValue $true
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $workspaceRoot '.tada/task-environment-control.json') -Encoding utf8
Write-Host 'Private task-byte drift changes environment identity; original cache bytes restored.'
$report

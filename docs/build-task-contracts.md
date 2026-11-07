# Build-task input contracts

TADA's Windows/.NET development alpha records MSBuild task execution during locked restore and actual rebuild. Task journals identify the originating project/import, target, task name, loaded task assembly and supported structured file parameters. These supplement evaluated compiler/resource/reference inputs and preprocessed imports; they do not trace arbitrary file-system reads.

## Supported trust boundary

The installed SDK and explicitly pinned package tasks remain trusted toolchain components. Supported standard file-reading tasks, such as `ReadLinesFromFile`, must use selected repository inputs, fresh generated files or trusted toolchain files. An unselected assertion-data file prevents PASS even when the task is declared in a reviewed project.

Opaque custom tasks and shell commands require an explicit reviewed contract. For example, a custom generator in `tests/Acceptance/Acceptance.csproj` that reads `test-data` uses this additional `tada.json` property:

```json
{
  "buildTaskContracts": [
    {
      "origin": "tests/Acceptance/Acceptance.csproj",
      "target": "GenerateExpectations",
      "task": "Exec",
      "inputRoots": ["test-data"]
    }
  ]
}
```

The example is a configuration fragment, not a complete project. Use the exact origin, target and task shown in the diagnostic. Wildcard task/target identities are unsupported. All paths are repository-relative. The contract origin and every declared root automatically join the immutable oracle snapshot; changing those files requires a new baseline, review and approval.

An empty `inputRoots` array explicitly declares that the custom code reads no additional repository files. Review the command and any scripts before making that declaration. Declare indirect inputs too. A directory containing an executable script does not automatically account for files the script reads elsewhere.

File/opaque project evaluation, including detected static file-reading property functions and `Exists(...)` conditions, uses a contract whose `target` is `@evaluation` and whose `task` is `property-functions`. Its `origin` is the containing project, props or targets file. Review and declare every evaluation read root.

These contracts trust reviewed code to honour its declarations. They are not an operating-system sandbox, an independent monitor of arbitrary I/O or proof that a declaration is complete. SDK/package code, stable on-disk file identities and the cooperative local process remain explicit local advisory L1 assumptions. Unsupported or undeclared code prevents verification; adding a contract solely to remove a diagnostic defeats the review boundary.

## Retained evidence and migration

New baselines use inventory schema 6, build policy `evaluated-dotnet-build-v3` and task policy `msbuild-tasks-and-declared-inputs-v2`. Observed SDK/package task assemblies and file parameters join the environment identity, including files absent from evaluated compiler inputs. Runs retain `task-inputs.json`, the packaged logger and raw `build/*.tasks.ndjson` reports. Offline verification checks their hashes, task lifecycle, required scalar inputs and command linkage without rebuilding the application. MSBuild legitimately omits empty item-array parameters, such as an empty static-web-assets copy list.

Older baselines remain readable historical records. They cannot acquire task-input evidence or approval for the new policy by default. Prepare, review and approve a new baseline; retain old evidence and use its matching historical tool when inspecting its original execution.

See the [quickstart](quickstart.md) and [release checklist](release-readiness.md). Automated fixture approvals establish control behaviour only; the human still approves real business expectations and trust declarations.

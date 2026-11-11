# TADA development context

## Current change

Implement change-aware multi-level verification as agreed on 2026-11-10. ADR 0042 records the accepted delivery boundaries. `docs/requirements/change-aware-verification.feature` records the new product expectations. `docs/multi-level-verification.md` documents the supported plan and multi-service interfaces.

The implementation targets alpha.13. Initial discovery selects proportionate unit, service, contract and journey checks. Captured supporting checks retain delegated engineering scope. Business baselines retain human approval. Local named .NET services share a repository and own scenario resources. Empty justified selections report NO_CHECKS.

## Delivery and validation

PR #1 was already merged. The repository resolves to `tayzer/TADA`. Work continues on `feat/change-aware-verification`, based on current main `943486f`. The integration retains main's runtime-checkpoint fixes, pinned CI actions and required PR gate.

Focused/replay checks, legacy Orders controls, installed-package multi-service controls and exact plugin-ZIP setup checks are the validation entry points. Their scripts retain machine-readable records under `.artifacts/`. Automated fixture approvals are isolated controls. Requester acceptance of alpha.13 remains separate from these checks and the earlier accepted alpha.12 release.

The multi-service control exercises native Debug/Release journeys, service and producer/consumer contract checks, retained-evidence tampering, a real compensation defect, restored implementation and a no-test chore revision. Initial implementation findings and failed attempts remain in `.artifacts/multi-service/`.

## Local SDK host

Use `E:\Development\git\AAATDD\experiments\compatibility-probe\.tools\dotnet\dotnet.exe`, SDK 10.0.403. Recheck availability before use. Pass the same host through `--dotnet` for recorded runs. PowerShell 7 is available. Process/task-host permissions can be required on this Windows sandbox.

## Next action

Validate the final source/package checkpoint and review the feature PR's required checks. Keep publication and merge under their existing separate authority. The installed plugin cache is separate from source/package delivery.

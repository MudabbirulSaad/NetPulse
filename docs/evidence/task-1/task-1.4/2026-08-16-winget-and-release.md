# Task 1.4 Distribution Evidence (WinGet + GitHub Release)

Date: 2026-08-16
Scope: Distinction level distribution proof

## Captured now

- Local manifest validation executed:
  - `winget validate --manifest distribution\winget\manifests\m\MudabbirulSaad\NetPulse\1.0.0`
  - Result: validation succeeded (package dependency line shown as non-validated local dependency, expected for `Microsoft.DotNet.DesktopRuntime.10`).
- Output: [2026-08-16-winget-validate-output.txt](2026-08-16-winget-validate-output.txt)
- Download hash check completed:
  - `artifacts/winget-download/NetPulseSetup.msi` SHA-256 = `8B7DD051FFF1DF0B28843246EB1775526C51C39D35FF50C1BE9F6AD08A9D0592`
- Distribution metadata present:
  - `distribution/winget/manifests/m/MudabbirulSaad/NetPulse/1.0.0/*`
- Runtime dependency declared:
  - `Microsoft.DotNet.DesktopRuntime.10`

## External submission state

- PR created for manifest route:
  - [microsoft/winget-pkgs#418335](https://github.com/microsoft/winget-pkgs/pull/418335)
- Current status captured at 2026-08-16 19:42:00 (AEST):
  - PR is now **ready** (`isDraft=false`).
  - `license/cla` check is passing.
  - Validation workflow checks `08`, `09`, and `10` are now complete and passed.
  - Merge state remains blocked because review is still required (`reviewDecision: REVIEW_REQUIRED`).
  - Reviewer status: no completed reviews yet, no explicit review requests, but assigned maintainer is present (`stephengillie`).
  - Policy bot comment confirms validation guide path is available; all required checks are still green.

## Still required for final-grade submission

- Open PR status transition to final accepted state.
- Continue to monitor for maintainer review/merge on WinGet.
- Capture at least one sandbox install-uninstall run using local manifests.
- Include public link/ID of GitHub checks proving the full submission path is complete.

# Task 1.4 Distribution Evidence (WinGet + GitHub Release)

Date: 2026-08-16
Scope: Task 1.4 public distribution proof

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
  - The policy gate was neutral and requested manual review; the remaining automated checks passed.

## Status update - 2026-08-17

- A fresh hosted Windows deployment run completed the install, two-launch restart, 1.0.0 to 1.0.1 replacement, uninstall, and LocalAppData retention sequence.
- WinGet policy validation initially routed `ShortDescription` to manual review under `Policy-Test-2.7`.
- The validation artifact identified the networking description as an adult-theme match. The sentence was rewritten and pushed to the existing pull-request branch.
- The repeated policy gate passed, followed by the remaining package validation gates, and the PR gained `Validation-Completed`.
- The historical `Policy-Test-2.7` label remains visible, but its current check result is successful.
- The pull request remains open and mergeable with `REVIEW_REQUIRED` and no recorded reviews. Maintainer approval is the only remaining external event; the submission is not described as merged.

Correction evidence: [2026-08-17-winget-policy-correction.txt](2026-08-17-winget-policy-correction.txt)

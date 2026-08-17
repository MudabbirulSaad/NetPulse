# Task 1.3 Deployment + Runtime Path (Clean-Machine Checks)

Date: 2026-08-16
Scope: Clean environment validation for install, restart, upgrade, uninstall

## What must be demonstrated for full credit

1. Install `NetPulseSetup.msi` as admin on a clean Windows 11 x64 machine.
2. Confirm the ten published files exist under `%ProgramFiles%\NetPulse`.
3. Start NetPulse from Start menu.
4. Run default targets and observe monitoring output.
5. Close app while checks are active; confirm clean exit.
6. Restart and confirm existing settings/history are restored.
7. Install a higher version with the same upgrade code and confirm replacement.
8. Uninstall and confirm binaries + shortcut are removed while local data remains.

## Reproducible deployment gate

- `scripts/installer/Test-NetPulsePackage.ps1` checks the package version, ten-file inventory, and stable upgrade code.
- `scripts/installer/Test-NetPulseCleanDeployment.ps1` runs the complete sequence on a clean elevated Windows runner:
  - install version 1.0.0;
  - check the ten installed files and Start menu shortcut;
  - launch, close cleanly, and launch again;
  - compare settings and history across the restart;
  - install a temporary 1.0.1 package with the same upgrade code;
  - confirm that 1.0.0 is replaced;
  - uninstall and check that binaries and the shortcut are removed while LocalAppData remains.
- The Windows workflow uploads the MSI logs and a short deployment summary as `clean-deployment-evidence`.
- Temporary upgrade output is isolated from the 1.0.0 release output and is never published as a release package.
- Local package checks completed for both versions:
  - [1.0.0 package result](2026-08-17-task1.3-package-validation.txt)
  - [1.0.0 and temporary 1.0.1 comparison](2026-08-17-task1.3-upgrade-package-validation.txt)

## Evidence boundary

- The local machine has usable Task 1.2 screenshots and restart records, but it is not a clean environment.
- Windows Sandbox is not installed on this machine, so the public clean-environment record comes from the fresh `windows-latest` runner.
- A workflow run must pass before this section is marked complete. The uploaded `deployment-summary.txt` is the primary clean-machine record; its MSI logs provide the detailed installer trace.

## Manual capture order, if a separate VM is used

- Install from MSI.
- Open app and capture target list in running state.
- Capture one manual `Test connection` action.
- Close app and capture shutdown behavior.
- Restart app and capture restored targets.
- Create a second local MSI with higher version for upgrade test and capture replacement.
- Uninstall and capture Program Files and `%LocalAppData%\NetPulse` status.

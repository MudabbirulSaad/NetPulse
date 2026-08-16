# Windows Deployment Checks

This checklist separates checks that can run on every development machine from the administrator and clean-machine gates that need an appropriate Windows environment.

## Build and inspect the MSI

From the repository root:

```powershell
dotnet build installer\NetPulse.Setup\NetPulse.Setup.wixproj -c Release -p:Platform=x64
pwsh -NoProfile -File scripts\installer\Test-NetPulsePackage.ps1
```

The structural check opens the Windows Installer database without installing it. It asserts the 1.0.0 product version, stable upgrade code, and exact ten-file runtime inventory.

## Install and uninstall

Open PowerShell as Administrator, change to the repository root, and run:

```powershell
pwsh -NoProfile -File scripts\installer\Install-NetPulse.ps1
pwsh -NoProfile -File scripts\installer\Uninstall-NetPulse.ps1
```

Both scripts run Windows Installer silently, reject non-elevated sessions, accept success or restart-required exit codes, and write verbose logs under the ignored `artifacts` directory.

## Clean-machine checklist

Use a Windows 11 x64 virtual machine or Windows Sandbox with networking enabled.

1. Install the Microsoft .NET Desktop Runtime 10 x64 prerequisite.
2. Copy only the release MSI into the clean environment.
3. Install the MSI from an elevated PowerShell session.
4. Confirm `NetPulse` appears in Installed apps and the Start menu.
5. Inspect `%ProgramFiles%\NetPulse` and compare its ten files with [the dependency inventory](dependency-inventory.md).
6. Start NetPulse from the Start menu and allow the three first-run targets to populate.
7. Exercise an HTTP target and an explicit-resolver DNS target.
8. Close NetPulse while checks are active, then confirm the process exits.
9. Restart NetPulse and confirm settings and history return.
10. Install a later test MSI with the same UpgradeCode and a higher product version; confirm the previous binaries are replaced.
11. Uninstall NetPulse and confirm the Program Files directory and Start menu entry are removed.
12. Confirm `%LocalAppData%\NetPulse` still contains user-owned settings, history, and logs.

Record the operating-system build, desktop-runtime version, MSI SHA-256, installed file list, and the outcome of each step. Store assessment-only captures outside the public repository.

## Required manual gates

Administrator installation, live network behaviour, Start menu behavior, upgrade replacement, and data retention cannot be established by the structural script alone. Complete those checks in the clean environment before publishing a release.

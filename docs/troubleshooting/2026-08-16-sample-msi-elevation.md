# Task 1.1 MSI requires elevation

## Context

The Task 1.1 package is intentionally a per-machine x64 MSI. It installs under
`C:\Program Files\SampleApp` and creates a Start menu shortcut for all users.

## Symptom

Running a silent installation from a non-elevated terminal returned Windows Installer
exit code 1603. The verbose log contained error 1925: the current account did not have
sufficient privileges to complete an installation for all users.

## Cause

Windows protects Program Files and all-users installer registration. A per-machine MSI
must be launched with administrator approval.

## Command

From an elevated PowerShell terminal at the repository root:

```powershell
msiexec.exe /i ".\task-1.1\SampleApp.Setup\bin\x64\Release\SampleAppSetup.msi" /passive /norestart /l*v ".\sample-install.log"
```

After collecting the required run evidence, remove it with:

```powershell
msiexec.exe /x ".\task-1.1\SampleApp.Setup\bin\x64\Release\SampleAppSetup.msi" /passive /norestart /l*v ".\sample-uninstall.log"
```

Expected installation contents are `SampleApp.exe`, `SampleApp.dll`,
`SampleApp.deps.json`, and `SampleApp.runtimeconfig.json`.

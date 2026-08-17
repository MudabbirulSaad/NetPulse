# NetPulse Task 1 Completion Report

## Project scope

NetPulse is a Windows desktop utility that checks HTTP endpoints and DNS resolvers. It was built as a framework-dependent .NET 10 WPF application and packaged as a per-machine x64 MSI. Settings, history, and logs are stored under `%LocalAppData%\NetPulse`.

This public report contains product and deployment records only. Student details, Canvas pages, and assessment-only screenshots belong in the private submission copy.

## Task 1.1 — sample application and MSI

The separate sample console application was built in Release mode and packaged with WiX. The recorded workflow covers:

1. elevated MSI installation;
2. launch from the installed package;
3. the required `Deployment Activity 1: Pass Task Completed!` output; and
4. elevated MSI removal.

The non-elevated failure is also recorded because the package installs under Program Files and therefore requires administrator approval.

Evidence: [Task 1.1 milestone](evidence/task-1/task-1.1/2026-08-16-sample-app-milestone.md)

## Task 1.2 — NetPulse application

NetPulse 1.0.0 was installed under `C:\Program Files\NetPulse`. The installed folder contains the application, project assemblies, runtime configuration, and the four third-party runtime assemblies expected by the package inventory.

The installed application was then used for the following checks:

1. launch from the Program Files installation;
2. live HTTP and DNS sampling;
3. a manual OpenReels check, including the `Checking` state and completed HTTP 200 result;
4. clean shutdown while monitoring was active; and
5. restart with the saved target configuration and earlier history still available.

The settings SHA-256 value was unchanged across the restart. The history file grew as new samples were collected, which is the intended persistence behaviour.

Evidence: [Task 1.2 milestone](evidence/task-1/task-1.2/2026-08-16-netpulse-milestone.md) and [restart record](evidence/task-1/task-1.2/2026-08-17-restart-and-manual-check.txt)

## Task 1.3 — deployment and dependency path

The release MSI contains ten installed files and uses the stable upgrade code `{3E03781C-78F4-497C-BC86-F9E3FF497334}`. A temporary version 1.0.1 MSI is built in an isolated output directory for the upgrade test; it is not distributed as a release.

The Windows workflow now performs the full deployment sequence on a fresh hosted Windows machine:

1. install 1.0.0 silently and retain the MSI log;
2. check the installed file inventory and Start menu shortcut;
3. launch, close, and launch the application again;
4. compare settings and history across the restart;
5. install 1.0.1 and confirm that it replaces 1.0.0;
6. uninstall 1.0.1; and
7. confirm that installed files and the shortcut are removed while LocalAppData remains.

The complete sequence passed in [Windows workflow run 32019017246](https://github.com/MudabbirulSaad/NetPulse/actions/runs/32019017246). The run uploaded `deployment-summary.txt` and the three Windows Installer logs as the `clean-deployment-evidence` artifact.

Evidence: [Task 1.3 deployment record](evidence/task-1/task-1.3/2026-08-16-deployment-and-restart.md) and [clean Windows result](evidence/task-1/task-1.3/2026-08-17-task1.3-clean-windows-run.txt)

## Task 1.4 — public distribution

Version 1.0.0 is available from the public GitHub release with an MSI and SHA-256 checksum. The WinGet manifests pass the automated package checks in pull request [microsoft/winget-pkgs#418335](https://github.com/microsoft/winget-pkgs/pull/418335).

The pull request is open, not a draft, and mergeable. All automated validation gates are green. No author action is currently requested; the remaining step is the WinGet maintainer decision.

Evidence: [Task 1.4 distribution record](evidence/task-1/task-1.4/2026-08-16-winget-and-release.md)

## Troubleshooting notes

- Both MSIs install per machine. A non-elevated silent install is blocked by Windows policy, so deployment commands must be run from an elevated PowerShell session.
- Uninstall removes application files and shortcuts but intentionally leaves `%LocalAppData%\NetPulse` so a later reinstall can restore the user's targets and history.
- Windows Sandbox is not installed on the development machine. The clean-environment package sequence therefore runs on a fresh GitHub-hosted Windows runner, with the local GUI captures kept as the visible application evidence.

See the [troubleshooting record](evidence/task-1/troubleshooting/2026-08-16-troubleshooting-log.md) for command outputs and resolutions.

## Evidence map

- [Task 1 evidence index](evidence/task-1/evidence-index.md)
- [Assessment evidence index](evidence/assessment-index.md)
- [Evidence checklist](evidence/evidence-checklist.md)
- [WinGet distribution record](distribution/winget.md)

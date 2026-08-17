# Troubleshooting Log (Task 1 Completion)

Date: 2026-08-16

## 1) Task 1.1 MSI install blocked by privileges (non-admin context)

Symptom

- Install completed with Windows Installer error 1925 and exit code 1603 when run in non-admin PowerShell.

Likely cause

- MSI is per-machine and targets `C:\Program Files\`.

Action taken

- Captured `artifacts/task-1.1/sample-install.log` and `artifacts/task-1.1/sample-uninstall.log` to confirm expected failure mode.
- Added this to evidence as an explicit elevated prerequisite.

Result

- Confirmed requirement: admin context is mandatory for this package.

## 2) User data persistence check

Symptom

- User data path and file timestamps needed for persistent behavior proof.

Action taken

- Recorded presence and timestamps of:
  - `%LocalAppData%\NetPulse\settings.json`
  - `%LocalAppData%\NetPulse\history.json`
  - `%LocalAppData%\NetPulse\logs\netpulse-YYYYMMDD.log`

Result

- The settings hash remained unchanged after restart and the history file continued to grow as new checks completed.

## 3) NetPulse MSI CLI blocked in non-admin context

Symptom

- CLI install/uninstall using direct `msiexec` commands returned exit code `1625` in this environment.

Likely cause

- Organization/device policy requires elevated context for managed MSI operations and blocks silent privileged transitions.

Action taken

- Captured the full run in `docs/evidence/task-1/task-1.2/2026-08-16-netpulse-cli-status.txt` and `artifacts/netpulse-install-uninstall-cli-20260816-195459.txt`.
- Kept the successful install/uninstall workflow in admin context via:
  - `artifacts/install-netpulse.log`
  - `artifacts/uninstall-netpulse.log`

Result

- Non-admin CLI path is correctly documented as environment policy blocked; admin script path works and preserves `%LocalAppData%\NetPulse` data on uninstall.

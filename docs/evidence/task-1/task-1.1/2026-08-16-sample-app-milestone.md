# Task 1.1 Evidence (SampleApp)

Date: 2026-08-16
Scope: `task-1.1`

## What to prove

1. Build the Task 1.1 console sample in Release.
2. Install the generated MSI from elevated PowerShell.
3. Run the console and capture the required message.
4. Uninstall the MSI and confirm cleanup.

## Existing public evidence

- `task-1.1\SampleApp\bin\Release\net10.0\win-x64\SampleApp.exe`
- `task-1.1\SampleApp.Setup\bin\x64\Release\SampleAppSetup.msi`
- `task-1.1\SampleApp\Program.cs`
- `task-1.1\README.md`
- `artifacts/task-1.1/sample-install-admin.log`
- `artifacts/task-1.1/sample-uninstall-admin.log`
- `docs/evidence/task-1/task-1.1/2026-08-16-sample-app-console-output.txt`
- `docs/evidence/task-1/task-1.1/2026-08-16-sample-app-cli-status.txt`

## Evidence captured (CLI and admin gate)

- `artifacts/task-1.1/sample-install-admin.log`
  - Elevated install completed successfully (`Exit code 0`).
  - Success line: `Product: SampleApp -- Installation completed successfully.`
- `artifacts/task-1.1/sample-uninstall-admin.log`
  - Elevated uninstall completed successfully (`Exit code 0`).
- `artifacts/task-1.1/sample-install.log`
  - Non-admin run reached `Install` and failed with `1603` because elevated install is required.
- `artifacts/task-1.1/sample-uninstall.log`
  - Non-admin uninstall returned `1605` (`This action is only valid for products that are currently installed.`).
- `docs/evidence/task-1/task-1.1/2026-08-16-sample-app-console-output.txt`
  - Captures required text:
    - `Deployment Activity 1: Pass Task Completed!`
    - `Press any key to exit...`
- `docs/evidence/task-1/task-1.1/2026-08-16-sample-app-cli-status.txt`
  - Records the CLI run sequence and exit codes in one file.

## Troubleshooting note

- Elevated execution is mandatory because this package is per-machine and writes under `C:\Program Files`.
- Required text output is already captured and linked.

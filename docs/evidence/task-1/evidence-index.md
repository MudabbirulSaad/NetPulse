# Task 1 Evidence Index

Date format: `YYYY-MM-DD-shortdesc.md/.txt/.png`

- `task-1/task-1.1/` -> Task 1.1 sample app workflow
- `task-1/task-1.2/` -> NetPulse install, run, and uninstall workflow
- `task-1/task-1.3/` -> clean-machine and upgrade checks
- `task-1/task-1.4/` -> WinGet and release checks
- `task-1/screenshots/` -> public screenshots only
- `task-1/troubleshooting/` -> issue notes and resolutions

## Current Evidence Map

| Task | Outcome | Evidence file | Evidence status |
|---|---|---|---|
| Task 1.1 install + run + uninstall | Install/uninstall command gates and required console output text are now captured. Non-admin elevation failures are documented and accepted. | [2026-08-16-sample-app-milestone.md](task-1.1/2026-08-16-sample-app-milestone.md) | complete |
| NetPulse milestone (Task 1.2) | Install/uninstall logs, installed file inventory, live dashboard, manual check, clean shutdown, and restart persistence are captured. | [2026-08-16-netpulse-milestone.md](task-1.2/2026-08-16-netpulse-milestone.md) | complete |
| Clean-machine and deployment checks (Task 1.3) | Fresh Windows run 32019017246 passed install, dependency inventory, two launches, 1.0.0 to 1.0.1 replacement, uninstall, and LocalAppData retention. | [2026-08-16-deployment-and-restart.md](task-1.3/2026-08-16-deployment-and-restart.md) | complete |
| WinGet and release path (Task 1.4) | Manifest validations and all 08-10 checks pass; PR now blocked only by required maintainer review. | [2026-08-16-winget-and-release.md](task-1.4/2026-08-16-winget-and-release.md) | partial |
| Troubleshooting evidence | Elevation gate documented for Task 1.1 and local-state retention behavior documented elsewhere. | [2026-08-16-troubleshooting-log.md](troubleshooting/2026-08-16-troubleshooting-log.md) | partial |

## Priority next actions before final submission

1. Keep a separate VM or Windows Sandbox screenshot only if the rubric specifically requires visible clean-machine proof.
2. Record the WinGet maintainer decision when pull request 418335 changes state.
3. Export the final private submission report with student details and Canvas-only screenshots kept outside the public repository.

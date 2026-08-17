# Task 1.2 / NetPulse Milestone Evidence

Date: 2026-08-16
Scope: `NetPulse` main app + main MSI package

## Package and installation records

- Installed app binaries are present:
  - `C:\Program Files\NetPulse\CommunityToolkit.Mvvm.dll`
  - `C:\Program Files\NetPulse\DnsClient.dll`
  - `C:\Program Files\NetPulse\NetPulse.App.deps.json`
  - `C:\Program Files\NetPulse\NetPulse.App.dll`
  - `C:\Program Files\NetPulse\NetPulse.App.exe`
  - `C:\Program Files\NetPulse\NetPulse.App.runtimeconfig.json`
  - `C:\Program Files\NetPulse\NetPulse.Core.dll`
  - `C:\Program Files\NetPulse\NetPulse.Infrastructure.dll`
  - `C:\Program Files\NetPulse\Serilog.dll`
  - `C:\Program Files\NetPulse\Serilog.Sinks.File.dll`
- The installed application reports version `1.0.0` in the Windows uninstall registry.
- User-owned data files are present and persistent:
  - `%LocalAppData%\NetPulse\settings.json`
  - `%LocalAppData%\NetPulse\history.json`
  - `%LocalAppData%\NetPulse\logs\netpulse-20260816.log`
- Local script logs captured:
  - `artifacts/install-netpulse.log`
  - `artifacts/uninstall-netpulse.log`
  - These logs show successful install and uninstall via the project scripts, with retention message confirming:
    - `User settings, history, and logs remain in %LocalAppData%\NetPulse.`
- MSI structure check output:
  - [2026-08-16-netpulse-msi-structure-test.txt](2026-08-16-netpulse-msi-structure-test.txt)
- CLI install/uninstall status in non-admin context:
  - [2026-08-16-netpulse-cli-status.txt](2026-08-16-netpulse-cli-status.txt)
  - Install command exit `1625` with `This installation is forbidden by system policy. Contact your system administrator.`
  - Uninstall command exit `1625` with the same policy restriction.

## GUI and restart record

- NetPulse was opened from `C:\Program Files\NetPulse\NetPulse.App.exe`.
- The dashboard displayed live HTTP and DNS results for the three supplied targets and one saved local target.
- A manual OpenReels check moved through `Checking` and returned `Healthy` with HTTP 200.
- The application closed cleanly and reopened with the same settings hash and all four saved targets.
- History remained available and continued to grow after the restart.
- Detailed timestamps, hashes, and log entries:
  - [2026-08-17-restart-and-manual-check.txt](2026-08-17-restart-and-manual-check.txt)
- Screenshots:
  - [manual check in progress](../screenshots/2026-08-17-task1.2-manual-check-running.png)
  - [dashboard with completed live results](../screenshots/2026-08-17-task1.2-manual-check-complete.png)
  - [saved targets after restart](../screenshots/2026-08-17-task1.2-restart-restored-targets.png)

## Remaining boundary

The normal-machine Task 1.2 launch, monitoring, manual check, clean shutdown, and restart evidence is complete. The separate clean-machine upgrade and uninstall path is recorded under Task 1.3.

## Notes

- Keep evidence screenshots in `docs/evidence/task-1/screenshots` with date-stamped names.

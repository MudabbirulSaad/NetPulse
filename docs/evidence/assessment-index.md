# Assessment Evidence Index

This index maps each implementation area to durable public project evidence and a separate manual capture gate. Student details, assignment PDFs, and raw captures do not belong in this repository.

| Area | Public project evidence | Manual capture gate |
|---|---|---|
| Task 1.1 application | `task-1.1/SampleApp`, Release project settings, success message | Release build output and running console |
| Task 1.1 installer | WiX source, stable identifiers, embedded cabinet, Start menu shortcut | Elevated install, Program Files contents, launch, uninstall |
| Target rules | Core model, validator, first-run defaults, deterministic tests | Invalid URL, DNS resolver, interval, timeout, and target-count scenarios |
| HTTP monitoring | Cancellable probe and handler-based status/failure tests | Healthy endpoint, HTTP error, offline network |
| DNS and ICMP | Explicit-resolver probe, typed result details, independent ICMP tests | Resolver success, NXDOMAIN, blocked ICMP, unreachable resolver |
| Scheduling | `INetPulseSession`, target loops, per-target locks, cancellation tests | Pause/resume, run-all, close during active sampling |
| Persistence | Atomic JSON store, rolling-history and corruption tests | Restart recovery and corrupt-file warning |
| Dashboard | WPF views/view models and [sanitized dashboard](../screenshots/dashboard.png) | Three defaults plus accessible state presentation |
| Target management | Add/edit/test/enable/delete workflows and view-model tests | Save-unreachable choice and deletion confirmation |
| Metrics | Rolling statistics and built-in WPF sparkline tests | Min/average/max and graph gaps after failures |
| Reliability | Sanitized Serilog output and shutdown/network-loss records | Disconnect/reconnect and log inspection |
| Deployment | MSI authoring, dependency inventory, package-inspection script | Clean x64 install, installed file list, upgrade, uninstall retention |
| CI | Windows workflow and Dependabot configuration | Passing GitHub workflow link |
| Release | Release notes, MSI checksum, semantic tag | Public release page and downloaded checksum comparison |
| WinGet | Versioned manifests and submission record | `winget validate`, Windows Sandbox install, pull-request link |

Use the [evidence checklist](evidence-checklist.md) while collecting captures. Store local originals under the ignored `docs/evidence/local/` path or outside the repository.

## Current task index

See the remaining-manual-gate tracker here:

- [Task 1 evidence index](task-1/evidence-index.md)

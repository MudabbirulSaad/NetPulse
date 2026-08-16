# NetPulse Project Plan

## Product goal

NetPulse is a local Windows utility for monitoring HTTP endpoints and DNS resolvers without a cloud account or remote telemetry. It presents current health, latency, recent history, and readable failure reasons.

## Release 1.0 scope

- HTTP/HTTPS checks with response status and latency
- Explicit A-record queries through a selected DNS resolver
- Separate ICMP reachability for resolver targets
- Add, edit, enable, disable, test, and delete targets
- Five-second minimum polling with per-check timeouts
- Rolling history and real min/average/max latency
- Local JSON persistence and rolling text logs
- WPF dashboard, WiX MSI, Windows CI, GitHub Release, and WinGet manifest

The release excludes accounts, cloud storage, alerts, packet capture, scanning, scraping, and OpenReels integration.

## Delivery gates

1. A separate sample application completes the baseline MSI workflow.
2. Core rules and network probes pass deterministic tests.
3. The monitoring session runs without overlapping checks or UI blocking.
4. The WPF application persists user targets and handles network loss.
5. The NetPulse MSI installs all required runtime DLLs and uninstalls its binaries cleanly.
6. Windows CI produces a release MSI.
7. Version 1.0.0 is attached to a public GitHub Release and submitted to WinGet.

## OpenReels isolation contract

NetPulse may make ordinary public DNS and HTTPS checks for `openreels.app`. It must not access or modify OpenReels source code, credentials, databases, private APIs, DNS configuration, deployment systems, or infrastructure. Checks must remain low frequency and must never become load or security testing.

# Runtime Dependency Inventory

NetPulse 1.0.0 is published as a framework-dependent Windows x64 application. The installer deliberately keeps the genuine runtime dependency files visible so the packaged result can be inspected and explained.

## Platform prerequisite

- Windows 10 version 1809 or later, or Windows 11, on x64
- Microsoft .NET Desktop Runtime 10 (x64), represented in WinGet as `Microsoft.DotNet.DesktopRuntime.10`

## Installed application files

| File | Purpose | Source |
|---|---|---|
| `NetPulse.App.exe` | Native Windows application host | .NET SDK publish output |
| `NetPulse.App.dll` | WPF views, view models, and composition root | This repository |
| `NetPulse.App.deps.json` | Runtime dependency graph | .NET SDK publish output |
| `NetPulse.App.runtimeconfig.json` | Framework and runtime settings | .NET SDK publish output |
| `NetPulse.Core.dll` | Domain model and app-facing session contract | This repository |
| `NetPulse.Infrastructure.dll` | Probes, scheduling, persistence, and logging | This repository |
| `CommunityToolkit.Mvvm.dll` | Observable state and command support | NuGet `CommunityToolkit.Mvvm` 8.4.2 |
| `DnsClient.dll` | Explicit-resolver DNS queries | NuGet `DnsClient` 1.8.0 |
| `Serilog.dll` | Structured logging API | NuGet `Serilog` 4.4.0 |
| `Serilog.Sinks.File.dll` | Daily rolling local log files | NuGet `Serilog.Sinks.File` 7.0.0 |

Program database (`.pdb`) files are retained in local build artifacts for diagnosis but are not installed by the MSI. The publish is not single-file and is not self-contained.

## User-owned data

The MSI does not package or remove `%LocalAppData%\NetPulse`. Settings, history, corruption backups, and logs remain user-owned across upgrades and uninstallation.

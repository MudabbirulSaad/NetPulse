# NetPulse

NetPulse is a lightweight Windows network health monitor built with C# and WPF. It performs asynchronous HTTP, DNS, and ICMP checks against configurable targets, displays real response latency and health state, stores a short local history, and ships as a WiX-based MSI installer.

## Project status

NetPulse is under active development. The first release targets Windows x64 and .NET 10 Desktop Runtime.

## Planned capabilities

- HTTP and HTTPS availability checks with status and latency
- Explicit DNS resolver queries with separate ICMP reachability
- Accessible health states that use text and icons in addition to colour
- Configurable targets, intervals, and timeouts
- Rolling local metrics and a compact latency graph
- Local JSON persistence and structured logs
- WiX MSI packaging, Windows CI, and a public WinGet manifest

## Architecture

The repository separates the WPF application, core monitoring module, infrastructure adapters, tests, and installer authoring. The UI interacts through a single `INetPulseSession` interface so networking and persistence remain independently testable.

- [Project plan](docs/project-plan.md)
- [Architecture](docs/architecture/architecture.md)
- [Backlog](docs/backlog.md)
- [Evidence checklist](docs/evidence/evidence-checklist.md)

## Build prerequisites

- Windows 10/11 x64
- .NET SDK 10.0.400 or a compatible patch selected by `global.json`
- Visual Studio 2026 with the .NET desktop development workload, or the .NET CLI
- HeatWave Community Edition for Visual Studio installer authoring

```powershell
dotnet tool restore
dotnet restore NetPulse.sln
dotnet build NetPulse.sln -c Release
dotnet test NetPulse.sln -c Release --no-build
```

## OpenReels isolation

`https://openreels.app` is only a default public demonstration target. NetPulse does not import, modify, deploy, or require access to any OpenReels source code, credentials, database, API, DNS record, or infrastructure.

## Privacy

NetPulse stores configuration, history, and logs locally under `%LocalAppData%\NetPulse`. It does not include telemetry, user accounts, or a cloud backend.

## License

Licensed under the [MIT License](LICENSE).

# ADR-001: Platform and packaging

## Status

Accepted

## Decision

Target .NET 10 WPF on Windows x64. Publish framework-dependent and non-single-file. Build MSI packages with WiX 4.0.6 and declare the .NET 10 Desktop Runtime as a prerequisite and WinGet dependency.

## Consequences

- The application uses an actively supported runtime and modern WPF tooling.
- The MSI remains compact and exposes genuine application dependencies for inspection.
- A clean machine must install `Microsoft.DotNet.DesktopRuntime.10` before launching NetPulse.

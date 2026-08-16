# .NET Desktop Runtime Prerequisite

NetPulse 1.0.0 is a framework-dependent x64 WPF application targeting `net10.0-windows`. It requires the Microsoft .NET Desktop Runtime 10 for x64; the SDK and ASP.NET Core runtime are not required on an end-user computer.

## Check the installed runtime

```powershell
dotnet --list-runtimes
```

The output must contain an x64 `Microsoft.WindowsDesktop.App 10.x` entry. If `dotnet` is unavailable or that entry is absent, install the current .NET Desktop Runtime 10 x64 from Microsoft before running the MSI.

The WinGet manifest declares `Microsoft.DotNet.DesktopRuntime.10` as a package dependency so WinGet can resolve the prerequisite. Direct MSI users must install it themselves.

## Why the runtime is separate

A framework-dependent publish keeps the MSI small and makes the application’s own libraries and NuGet dependencies visible. Servicing of the shared .NET runtime remains with Microsoft. NetPulse does not download or silently install a runtime.

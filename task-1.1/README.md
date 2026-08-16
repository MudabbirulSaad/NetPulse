# Task 1.1 Sample Application

This solution keeps the baseline deployment exercise separate from NetPulse.

```powershell
dotnet build Task1.1.sln -c Release
```

Expected outputs:

- `SampleApp\bin\Release\net10.0\win-x64\SampleApp.exe`
- `SampleApp.Setup\bin\x64\Release\SampleAppSetup.msi`

The MSI installs the framework-dependent x64 sample under `C:\Program Files\SampleApp`, creates a Start menu shortcut, supports major upgrades, and removes installed binaries during uninstall. The .NET 10 Desktop Runtime is a prerequisite.

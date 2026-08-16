# WinGet Distribution

NetPulse uses the package identifier `MudabbirulSaad.NetPulse`. Version 1.0.0 is represented as the current three-file WinGet 1.12 multi-file manifest under `distribution/winget/manifests`.

## Release binding

| Field | Value |
|---|---|
| Release | `v1.0.0` |
| Installer | `NetPulseSetup.msi` |
| Architecture | x64 |
| Installer type/scope | MSI / machine |
| SHA-256 | `8b7dd051fff1df0b28843246eb1775526c51c39d35ff50c1be9f6ad08a9d0592` |
| ProductCode | `{05FFADFC-5218-46B2-8466-019AD6B9FE48}` |
| UpgradeCode | `{3E03781C-78F4-497C-BC86-F9E3FF497334}` |
| Runtime dependency | `Microsoft.DotNet.DesktopRuntime.10` >= 10.0.0 |

The installer URL is version-specific and immutable. The hash is the digest reported for the public GitHub release asset.

## Local validation

```powershell
$manifest = 'distribution\winget\manifests\m\MudabbirulSaad\NetPulse\1.0.0'
winget validate --manifest $manifest
$download = 'artifacts\winget-download\NetPulseSetup.msi'
Invoke-WebRequest -Uri 'https://github.com/MudabbirulSaad/NetPulse/releases/download/v1.0.0/NetPulseSetup.msi' -OutFile $download
(Get-FileHash -Algorithm SHA256 $download).Hash
winget show --id Microsoft.DotNet.DesktopRuntime.10 --exact
```

The first command applies strict manifest/schema checks. The download and hash commands test the public release binding without changing Windows Package Manager settings. The final command confirms the declared runtime package exists in the configured source.

On 2026-08-16, strict validation succeeded, the public download produced the declared 376,832-byte MSI and SHA-256, and WinGet resolved `Microsoft.DotNet.DesktopRuntime.10` version 10.0.11. `winget download --manifest` was not used because this computer has the administrator-controlled `LocalManifestFiles` setting disabled.

## Windows Sandbox gate

Copy the version directory into a clean Windows Sandbox instance with networking enabled, then run:

```powershell
winget settings --enable LocalManifestFiles
winget install --manifest . --accept-package-agreements --accept-source-agreements
winget list --id MudabbirulSaad.NetPulse --exact
```

Open NetPulse from the Start menu, allow the three first-run targets to sample, restart once to confirm persistence, then uninstall:

```powershell
winget uninstall --id MudabbirulSaad.NetPulse --exact
```

Confirm Program Files and the shortcut are removed while `%LocalAppData%\NetPulse` remains. Keep the sandbox capture with private assessment evidence.

## Community submission

Copy the `manifests/m/MudabbirulSaad/NetPulse/1.0.0` directory into a branch based on `microsoft/winget-pkgs:master`. The pull request must contain only this one package version. Record the pull-request URL here after submission.

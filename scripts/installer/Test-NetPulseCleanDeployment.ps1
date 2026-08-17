[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $BaseMsiPath,

    [Parameter(Mandatory)]
    [string] $UpgradeMsiPath,

    [Parameter()]
    [string] $EvidenceDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\clean-deployment')
)

$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'This deployment test requires an elevated PowerShell session.'
}

$baseMsi = (Resolve-Path -LiteralPath $BaseMsiPath).Path
$upgradeMsi = (Resolve-Path -LiteralPath $UpgradeMsiPath).Path
$evidenceRoot = [IO.Path]::GetFullPath($EvidenceDirectory)
[IO.Directory]::CreateDirectory($evidenceRoot) | Out-Null

$installDirectory = Join-Path $env:ProgramFiles 'NetPulse'
$installedExecutable = Join-Path $installDirectory 'NetPulse.App.exe'
$localDataDirectory = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'NetPulse'
$settingsFile = Join-Path $localDataDirectory 'settings.json'
$historyFile = Join-Path $localDataDirectory 'history.json'
$startMenuShortcut = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\NetPulse\NetPulse.lnk'
$installLog = Join-Path $evidenceRoot 'install-1.0.0.log'
$upgradeLog = Join-Path $evidenceRoot 'upgrade-1.0.1.log'
$uninstallLog = Join-Path $evidenceRoot 'uninstall-1.0.1.log'
$summaryFile = Join-Path $evidenceRoot 'deployment-summary.txt'

$expectedFiles = @(
    'CommunityToolkit.Mvvm.dll',
    'DnsClient.dll',
    'NetPulse.App.deps.json',
    'NetPulse.App.dll',
    'NetPulse.App.exe',
    'NetPulse.App.runtimeconfig.json',
    'NetPulse.Core.dll',
    'NetPulse.Infrastructure.dll',
    'Serilog.dll',
    'Serilog.Sinks.File.dll'
)

function Invoke-WindowsInstaller {
    param(
        [Parameter(Mandatory)][ValidateSet('install', 'uninstall')][string] $Operation,
        [Parameter(Mandatory)][string] $PackagePath,
        [Parameter(Mandatory)][string] $LogPath
    )

    $switch = if ($Operation -eq 'install') { '/i' } else { '/x' }
    $arguments = "$switch `"$PackagePath`" /qn /norestart /l*v `"$LogPath`""
    $process = Start-Process -FilePath 'msiexec.exe' -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -notin 0, 3010) {
        throw "Windows Installer $Operation failed with exit code $($process.ExitCode). See $LogPath"
    }
}

function Wait-ForFile {
    param(
        [Parameter(Mandatory)][string] $Path,
        [Parameter()][int] $TimeoutSeconds = 20
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not (Test-Path -LiteralPath $Path)) {
        if ([DateTime]::UtcNow -ge $deadline) {
            throw "Timed out waiting for $Path"
        }
        Start-Sleep -Milliseconds 250
    }
}

function Start-AndCloseNetPulse {
    param([Parameter(Mandatory)][string] $Label)

    $process = Start-Process -FilePath $installedExecutable -PassThru
    Wait-ForFile -Path $settingsFile
    Wait-ForFile -Path $historyFile
    Start-Sleep -Seconds 3
    $process.Refresh()
    if ($process.HasExited) {
        throw "NetPulse exited unexpectedly during $Label."
    }

    $closed = $process.CloseMainWindow()
    if (-not $closed -or -not $process.WaitForExit(15000)) {
        throw "NetPulse did not close cleanly during $Label."
    }

    return $process.Id
}

function Get-NetPulseRegistrations {
    $registryPaths = @(
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    return @(Get-ItemProperty $registryPaths -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -eq 'NetPulse' })
}

$summary = [Collections.Generic.List[string]]::new()
$summary.Add("Clean Windows deployment test: $([DateTimeOffset]::Now.ToString('yyyy-MM-dd HH:mm:ss zzz'))")
$summary.Add("Base package: $baseMsi")
$summary.Add("Upgrade package: $upgradeMsi")

try {
    if ((Get-NetPulseRegistrations).Count -ne 0 -or (Test-Path -LiteralPath $installDirectory)) {
        throw 'NetPulse was already installed; this run is not a clean deployment test.'
    }

    Invoke-WindowsInstaller -Operation install -PackagePath $baseMsi -LogPath $installLog

    $installedFiles = @(Get-ChildItem -LiteralPath $installDirectory -File | Select-Object -ExpandProperty Name | Sort-Object)
    $missingFiles = @($expectedFiles | Where-Object { $_ -notin $installedFiles })
    $unexpectedFiles = @($installedFiles | Where-Object { $_ -notin $expectedFiles })
    if ($missingFiles.Count -gt 0 -or $unexpectedFiles.Count -gt 0) {
        throw "Installed file set mismatch. Missing: $($missingFiles -join ', '). Unexpected: $($unexpectedFiles -join ', ')."
    }
    if (-not (Test-Path -LiteralPath $startMenuShortcut)) {
        throw 'The Start menu shortcut was not installed.'
    }
    $summary.Add("Base install: passed ($($installedFiles.Count) files and Start menu shortcut)")

    $firstProcessId = Start-AndCloseNetPulse -Label 'first launch'
    $settingsHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $settingsFile).Hash
    $historyLengthBefore = (Get-Item -LiteralPath $historyFile).Length

    $secondProcessId = Start-AndCloseNetPulse -Label 'restart launch'
    $settingsHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $settingsFile).Hash
    $historyLengthAfter = (Get-Item -LiteralPath $historyFile).Length
    if ($settingsHashAfter -ne $settingsHashBefore) {
        throw 'Settings changed unexpectedly across the restart check.'
    }
    if ($historyLengthAfter -lt $historyLengthBefore) {
        throw 'History was not retained across the restart check.'
    }
    $summary.Add("Restart: passed (processes $firstProcessId then $secondProcessId; settings retained; history retained)")

    Invoke-WindowsInstaller -Operation install -PackagePath $upgradeMsi -LogPath $upgradeLog
    $registrations = Get-NetPulseRegistrations
    $installedVersions = @($registrations | Select-Object -ExpandProperty DisplayVersion -Unique)
    if ($registrations.Count -ne 1 -or $installedVersions.Count -ne 1 -or $installedVersions[0] -ne '1.0.1') {
        throw "Upgrade registration mismatch. Installed versions: $($installedVersions -join ', ')."
    }
    $summary.Add('Major upgrade: passed (1.0.0 replaced by 1.0.1)')

    Invoke-WindowsInstaller -Operation uninstall -PackagePath $upgradeMsi -LogPath $uninstallLog
    if ((Get-NetPulseRegistrations).Count -ne 0) {
        throw 'NetPulse remains registered after uninstall.'
    }
    if (Test-Path -LiteralPath $installedExecutable) {
        throw 'Installed application files remain after uninstall.'
    }
    if (Test-Path -LiteralPath $startMenuShortcut) {
        throw 'The Start menu shortcut remains after uninstall.'
    }
    if (-not (Test-Path -LiteralPath $settingsFile) -or -not (Test-Path -LiteralPath $historyFile)) {
        throw 'User settings or history were removed by uninstall.'
    }
    $summary.Add('Uninstall: passed (binaries and shortcut removed; LocalAppData retained)')
    $summary.Add('Result: PASS')
} catch {
    $summary.Add("Result: FAIL - $($_.Exception.Message)")
    throw
} finally {
    $summary | Set-Content -LiteralPath $summaryFile -Encoding utf8
}

$summary

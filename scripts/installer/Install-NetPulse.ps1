[CmdletBinding()]
param(
    [Parameter()]
    [string] $MsiPath = (Join-Path $PSScriptRoot '..\..\installer\NetPulse.Setup\bin\x64\Release\NetPulseSetup.msi'),

    [Parameter()]
    [string] $LogPath = (Join-Path $PSScriptRoot '..\..\artifacts\install-netpulse.log')
)

$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell session.'
}

$resolvedMsi = (Resolve-Path -LiteralPath $MsiPath).Path
$resolvedLog = [IO.Path]::GetFullPath($LogPath)
$logDirectory = Split-Path -Parent $resolvedLog
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null

$arguments = "/i `"$resolvedMsi`" /qn /norestart /l*v `"$resolvedLog`""
$process = Start-Process -FilePath 'msiexec.exe' -ArgumentList $arguments -Wait -PassThru

if ($process.ExitCode -notin 0, 3010) {
    throw "NetPulse installation failed with Windows Installer exit code $($process.ExitCode). See $resolvedLog"
}

if ($process.ExitCode -eq 3010) {
    Write-Output 'NetPulse installed successfully. Windows requested a restart.'
} else {
    Write-Output 'NetPulse installed successfully.'
}

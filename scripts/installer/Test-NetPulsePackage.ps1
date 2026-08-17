[CmdletBinding()]
param(
    [Parameter()]
    [string] $MsiPath = (Join-Path $PSScriptRoot '..\..\installer\NetPulse.Setup\bin\x64\Release\NetPulseSetup.msi'),

    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $ExpectedVersion = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$resolvedMsi = (Resolve-Path -LiteralPath $MsiPath).Path
$expectedFiles = @(
    'NetPulse.App.exe',
    'NetPulse.App.dll',
    'NetPulse.App.deps.json',
    'NetPulse.App.runtimeconfig.json',
    'NetPulse.Core.dll',
    'NetPulse.Infrastructure.dll',
    'CommunityToolkit.Mvvm.dll',
    'DnsClient.dll',
    'Serilog.dll',
    'Serilog.Sinks.File.dll'
)

$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.OpenDatabase($resolvedMsi, 0)

function Get-MsiValues {
    param([Parameter(Mandatory)][string] $Query)

    $view = $database.OpenView($Query)
    $view.Execute() | Out-Null
    try {
        while ($record = $view.Fetch()) {
            $record.StringData(1)
        }
    } finally {
        $view.Close() | Out-Null
    }
}

$fileValues = Get-MsiValues -Query 'SELECT `FileName` FROM `File`'
$actualFiles = @($fileValues | ForEach-Object {
    ($_ -split '\|')[-1]
})

Write-Verbose "Expected MSI files: $($expectedFiles -join ', ')"
Write-Verbose "Actual MSI files: $($actualFiles -join ', ')"

$missingFiles = @($expectedFiles | Where-Object { $_ -notin $actualFiles })
$unexpectedFiles = @($actualFiles | Where-Object { $_ -notin $expectedFiles })
if ($missingFiles.Count -gt 0 -or $unexpectedFiles.Count -gt 0) {
    throw "MSI file table mismatch. Missing: $($missingFiles -join ', '). Unexpected: $($unexpectedFiles -join ', ')."
}

$version = @(Get-MsiValues -Query "SELECT `Value` FROM `Property` WHERE `Property` = 'ProductVersion'")[0]
$upgradeCode = @(Get-MsiValues -Query "SELECT `Value` FROM `Property` WHERE `Property` = 'UpgradeCode'")[0]

if ($version -ne $ExpectedVersion) {
    throw "Expected MSI version $ExpectedVersion but found $version."
}

if ($upgradeCode -ne '{3E03781C-78F4-497C-BC86-F9E3FF497334}') {
    throw "Unexpected UpgradeCode: $upgradeCode"
}

Write-Output "NetPulse MSI structure passed: version $version, $($actualFiles.Count) files, stable UpgradeCode $upgradeCode."

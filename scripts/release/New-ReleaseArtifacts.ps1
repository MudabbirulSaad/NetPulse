[CmdletBinding()]
param(
    [Parameter()]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '1.0.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$releaseDirectory = Join-Path $repoRoot 'artifacts\release'
$releaseBuildDirectory = (Join-Path $repoRoot 'artifacts\release-build\bin') + [IO.Path]::DirectorySeparatorChar
$msiSource = Join-Path $repoRoot 'installer\NetPulse.Setup\bin\x64\Release\NetPulseSetup.msi'
$msiDestination = Join-Path $releaseDirectory 'NetPulseSetup.msi'
$checksumDestination = Join-Path $releaseDirectory 'NetPulseSetup.sha256'

Push-Location $repoRoot
try {
    dotnet restore NetPulse.sln -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed.' }

    dotnet build NetPulse.sln -c Release --no-restore -p:Platform=x64 -p:BaseOutputPath=$releaseBuildDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Release solution build failed.' }

    dotnet test NetPulse.sln -c Release --no-build --no-restore -p:Platform=x64 -p:BaseOutputPath=$releaseBuildDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Release test suite failed.' }

    dotnet build installer\NetPulse.Setup\NetPulse.Setup.wixproj -c Release -p:Platform=x64 -p:VersionPrefix=$Version
    if ($LASTEXITCODE -ne 0) { throw 'Release MSI build failed.' }

    & (Join-Path $repoRoot 'scripts\installer\Test-NetPulsePackage.ps1') -MsiPath $msiSource
    if ($LASTEXITCODE -ne 0) { throw 'MSI structure check failed.' }

    [IO.Directory]::CreateDirectory($releaseDirectory) | Out-Null
    Copy-Item -LiteralPath $msiSource -Destination $msiDestination -Force

    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $msiDestination).Hash.ToLowerInvariant()
    "$hash  NetPulseSetup.msi" | Set-Content -LiteralPath $checksumDestination -Encoding ascii

    Write-Output "NetPulse $Version release artifacts created:"
    Write-Output $msiDestination
    Write-Output $checksumDestination
    Write-Output "SHA-256: $hash"
} finally {
    Pop-Location
}

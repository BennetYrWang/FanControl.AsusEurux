[CmdletBinding()]
param(
    [string]$FanControlDir = 'C:\Program Files (x86)\FanControl',

    [ValidatePattern('^[0-9A-Za-z._-]+$')]
    [string]$Version = '0.1.0',

    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot 'artifacts'
}

$apiPath = Join-Path $FanControlDir 'FanControl.Plugins.dll'
if (-not (Test-Path -LiteralPath $apiPath)) {
    throw "FanControl.Plugins.dll was not found under '$FanControlDir'."
}

$pluginProject = Join-Path $projectRoot 'src\FanControl.AsusEurux\FanControl.AsusEurux.csproj'
dotnet build $pluginProject -c Release -p:FanControlDir="$FanControlDir" -p:Version="$Version"
if ($LASTEXITCODE -ne 0) {
    throw "Plugin build failed with exit code $LASTEXITCODE."
}

$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagePath = Join-Path $outputPath "package-$Version"
New-Item -ItemType Directory -Path $stagePath -Force | Out-Null

$buildPath = Join-Path $projectRoot 'src\FanControl.AsusEurux\bin\Release\net10.0-windows'
$packageFiles = @(
    (Join-Path $buildPath 'FanControl.AsusEurux.dll'),
    (Join-Path $buildPath 'AsusEurux.Core.dll'),
    (Join-Path $projectRoot 'LICENSE'),
    (Join-Path $projectRoot 'README.md'),
    (Join-Path $projectRoot 'README.zh-CN.md'),
    (Join-Path $projectRoot "RELEASE_NOTES_v$Version.md")
)

foreach ($file in $packageFiles) {
    Copy-Item -LiteralPath $file -Destination $stagePath -Force
}

$archivePath = Join-Path $outputPath "FanControl.AsusEurux-$Version.zip"
Compress-Archive -Path (Join-Path $stagePath '*') -DestinationPath $archivePath -CompressionLevel Optimal -Force

$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = "$archivePath.sha256"
Set-Content -LiteralPath $checksumPath -Value "$hash  $(Split-Path -Leaf $archivePath)" -NoNewline

Write-Host "Created '$archivePath'."
Write-Host "Created '$checksumPath'."

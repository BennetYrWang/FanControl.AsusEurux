param(
    [string]$FanControlDir = 'C:\Program Files (x86)\FanControl'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$pluginProject = Join-Path $projectRoot 'src\FanControl.AsusEurux\FanControl.AsusEurux.csproj'
$pluginDir = Join-Path $FanControlDir 'Plugins'

if (-not (Test-Path -LiteralPath (Join-Path $FanControlDir 'FanControl.Plugins.dll'))) {
    throw "FanControl.Plugins.dll was not found under '$FanControlDir'."
}

if (Get-Process -Name 'FanControl' -ErrorAction SilentlyContinue) {
    throw 'FanControl is running. Fully exit it before installing the plugin.'
}

dotnet build $pluginProject -c Release -p:FanControlDir="$FanControlDir"
if ($LASTEXITCODE -ne 0) {
    throw "Plugin build failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
$buildDir = Join-Path $projectRoot 'src\FanControl.AsusEurux\bin\Release\net10.0-windows'
try {
    Copy-Item -LiteralPath (Join-Path $buildDir 'FanControl.AsusEurux.dll') -Destination $pluginDir -Force
    Copy-Item -LiteralPath (Join-Path $buildDir 'AsusEurux.Core.dll') -Destination $pluginDir -Force
}
catch [System.UnauthorizedAccessException] {
    throw "Access to '$pluginDir' was denied. Run PowerShell as Administrator, then run this script again."
}

Write-Host "Installed ROG EURUX plugin to '$pluginDir'. Fully restart FanControl to load it."

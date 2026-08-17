[CmdletBinding()]
param(
    [ValidatePattern('^\d+$')]
    [string]$Version = '273',

    [string]$Destination
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $projectRoot '.deps\FanControl'
}

$destinationPath = [System.IO.Path]::GetFullPath($Destination)
$apiPath = Join-Path $destinationPath 'FanControl.Plugins.dll'
$versionPath = Join-Path $destinationPath '.version'

if ((Test-Path -LiteralPath $apiPath) -and
    (Test-Path -LiteralPath $versionPath) -and
    ((Get-Content -Raw -LiteralPath $versionPath).Trim() -eq $Version)) {
    Write-Host "FanControl V$Version plugin API already exists at '$destinationPath'."
    return
}

New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
$archivePath = Join-Path $destinationPath "FanControl_$Version`_net_10_0.zip"
$downloadUrl = "https://github.com/Rem0o/FanControl.Releases/releases/download/V$Version/FanControl_$Version`_net_10_0.zip"

try {
    Write-Host "Downloading FanControl V$Version plugin API..."
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entry = $archive.Entries |
            Where-Object { $_.Name -eq 'FanControl.Plugins.dll' } |
            Select-Object -First 1
        if ($null -eq $entry) {
            throw "FanControl.Plugins.dll was not found in '$downloadUrl'."
        }

        $inputStream = $entry.Open()
        try {
            $outputStream = [System.IO.File]::Create($apiPath)
            try {
                $inputStream.CopyTo($outputStream)
            }
            finally {
                $outputStream.Dispose()
            }
        }
        finally {
            $inputStream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    Set-Content -LiteralPath $versionPath -Value $Version -NoNewline
    Write-Host "Restored FanControl.Plugins.dll to '$destinationPath'."
}
finally {
    if (Test-Path -LiteralPath $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }
}

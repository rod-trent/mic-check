<#
  Builds a Teams app package (appPackage.zip) ready to sideload or submit.

  Usage:
    pwsh ./scripts/package.ps1 -Host "myapp.example.com" -AppId "<GUID>"

  -Host   The HTTPS host serving /index.html, /config.html, /styles.css, /app.js
          (no protocol, no trailing slash). Example: miccheck.example.com
  -AppId  A GUID for the Teams app id. Generate one with: [guid]::NewGuid()
#>
param(
  [Parameter(Mandatory = $true)][string]$Host,
  [Parameter(Mandatory = $false)][string]$AppId = [guid]::NewGuid().ToString()
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pkgDir = Join-Path $root "appPackage"
$build = Join-Path $root "build"
$staging = Join-Path $build "appPackage"

# Make sure icons exist.
if (-not (Test-Path (Join-Path $pkgDir "color.png"))) {
  Write-Host "Icons missing — generating placeholders..."
  python (Join-Path $PSScriptRoot "make-icons.py")
}

# Fresh staging copy.
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item (Join-Path $pkgDir "*") $staging -Recurse

# Inject host + app id into the manifest.
$manifestPath = Join-Path $staging "manifest.json"
$m = Get-Content $manifestPath -Raw
$m = $m.Replace("REPLACE_WITH_YOUR_HOST", $Host)
$m = $m.Replace("REPLACE_WITH_YOUR_APP_GUID", $AppId)
Set-Content -Path $manifestPath -Value $m -Encoding utf8

# Zip it (manifest.json + icons must be at the archive root).
$zip = Join-Path $build "appPackage.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force

Write-Host ""
Write-Host "Built $zip" -ForegroundColor Green
Write-Host "  Host : $Host"
Write-Host "  AppId: $AppId"
Write-Host "Sideload it in Teams: Apps > Manage your apps > Upload an app."

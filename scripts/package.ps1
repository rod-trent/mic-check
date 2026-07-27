<#
  Builds a Teams app package (build/appPackage.zip) ready to sideload or submit.

  Usage:
    pwsh ./scripts/package.ps1 -BaseUrl "https://rod-trent.github.io/mic-check"
    pwsh ./scripts/package.ps1 -BaseUrl "https://myapp.example.com" -AppId "<GUID>"

  -BaseUrl  The HTTPS base URL where index.html, config.html, styles.css, app.js,
            privacy.html and terms.html are served (no trailing slash). A path
            segment is supported (e.g. GitHub Pages project sites).
  -AppId    A GUID for the Teams app id. Defaults to a freshly generated one.
            Use a STABLE value for a real published app.
#>
param(
  [Parameter(Mandatory = $true)][string]$BaseUrl,
  [Parameter(Mandatory = $false)][string]$AppId
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

# Resolve a STABLE app id: explicit -AppId wins, else .appid.txt (committed),
# else a fresh GUID (fine for throwaway test packages only).
if ([string]::IsNullOrWhiteSpace($AppId)) {
  $idFile = Join-Path $root ".appid.txt"
  if (Test-Path $idFile) { $AppId = (Get-Content $idFile -Raw).Trim() }
  else { $AppId = [guid]::NewGuid().ToString() }
}
$pkgDir = Join-Path $root "appPackage"
$build = Join-Path $root "build"
$staging = Join-Path $build "appPackage"

# Derive the bare domain for validDomains (no scheme, no path).
$base = $BaseUrl.TrimEnd("/")
$domain = ([System.Uri]$base).Host
if ([string]::IsNullOrWhiteSpace($domain)) {
  throw "Could not parse a host from -BaseUrl '$BaseUrl'. Include the scheme, e.g. https://host/path"
}

# Make sure icons exist.
if (-not (Test-Path (Join-Path $pkgDir "color.png"))) {
  Write-Host "Icons missing — generating placeholders..."
  python (Join-Path $PSScriptRoot "make-icons.py")
}

# Fresh staging copy.
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item (Join-Path $pkgDir "*") $staging -Recurse

# Inject base URL, domain, and app id into the manifest.
$manifestPath = Join-Path $staging "manifest.json"
$m = Get-Content $manifestPath -Raw
$m = $m.Replace("{{CONTENT_BASE}}", $base)
$m = $m.Replace("{{VALID_DOMAIN}}", $domain)
$m = $m.Replace("{{APP_ID}}", $AppId)
Set-Content -Path $manifestPath -Value $m -Encoding utf8

# Zip it (manifest.json + icons must sit at the archive root).
$zip = Join-Path $build "appPackage.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force

Write-Host ""
Write-Host "Built $zip" -ForegroundColor Green
Write-Host "  Base URL: $base"
Write-Host "  Domain  : $domain"
Write-Host "  App id  : $AppId"
Write-Host "Sideload it in Teams: Apps > Manage your apps > Upload an app."

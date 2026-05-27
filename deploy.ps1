#Requires -Version 5
<#
.SYNOPSIS
    Build the mod and deploy it to an r2modman profile for local testing.
.DESCRIPTION
    Copies the DLL to BepInEx\plugins\stysk1-Scopophobia\ and upserts the mod entry in
    mods.yml so r2modman recognises the mod. The scp096 AssetBundle is copied next to the
    DLL — ScopophobiaPlugin.cs loads it from disk at runtime via AssetBundle.LoadFromFile.
.EXAMPLE
    ./deploy.ps1
.EXAMPLE
    ./deploy.ps1 -Profile "MyTestProfile"
.EXAMPLE
    ./deploy.ps1 -ProfileRoot "C:\custom\r2modmanPlus-local\LethalCompany\profiles\MyProfile"
#>
[CmdletBinding()]
param(
    [string]$Profile     = "Default",
    [string]$ProfileRoot = "",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# Resolve profile root from name, or use the explicit override.
$r2Base = "$env:APPDATA\r2modmanPlus-local\LethalCompany\profiles"
if (-not $ProfileRoot) { $ProfileRoot = Join-Path $r2Base $Profile }
if (-not (Test-Path $ProfileRoot)) { throw "Profile folder not found: $ProfileRoot" }

$pluginDir = Join-Path $ProfileRoot "BepInEx\plugins\stysk1-Scopophobia"
$modsYml   = Join-Path $ProfileRoot "mods.yml"

# ── Build ────────────────────────────────────────────────────────────────────
Write-Host "Restoring tools..." -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build "$root\src\Scopophobia.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$outDir = Join-Path $root "src\bin\$Configuration\netstandard2.1"
$dll    = Join-Path $outDir "Scopophobia.dll"
if (-not (Test-Path $dll)) { throw "Build output not found: $dll" }

# ── Deploy DLL + scp096 bundle ───────────────────────────────────────────────
if (-not (Test-Path $pluginDir)) { New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null }

# Remove NetcodePatcher backup DLLs and other stray build artefacts from prior
# deploys. If Scopophobia_original.dll is present, BepInEx loads it as a second
# plugin and the second scp096 AssetBundle.LoadFromFile call fails with
# "another AssetBundle with the same files is already loaded".
Get-ChildItem -Path $pluginDir -Filter "Scopophobia_original.*" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $pluginDir -Filter "*.deps.json" -ErrorAction SilentlyContinue | Remove-Item -Force

Copy-Item $dll $pluginDir -Force
Write-Host "Deployed Scopophobia.dll -> $pluginDir" -ForegroundColor Green

$bundle = Join-Path $root "Thunderstore\BepInEx\plugins\scp096"
if (Test-Path $bundle) {
    Copy-Item $bundle $pluginDir -Force
    Write-Host "Deployed scp096          -> $pluginDir" -ForegroundColor Green
}

# ── Upsert mods.yml ──────────────────────────────────────────────────────────
# r2modman reads mods.yml at the profile root to display installed mods.
# Without an entry here the DLL still loads, but the mod is invisible in the UI.
$manifest = Get-Content (Join-Path $root "Thunderstore\manifest.json") -Raw | ConvertFrom-Json
$ver      = $manifest.version_number -split '\.'
$deps     = ($manifest.dependencies | ForEach-Object { "    - $_" }) -join "`n"

$entry = @"
- manifestVersion: 1
  name: stysk1-Scopophobia
  authorName: stysk1
  websiteUrl: $($manifest.website_url)
  displayName: Scopophobia
  description: $($manifest.description)
  gameVersion: '0'
  networkMode: both
  packageType: other
  installMode: managed
  installedAtTime: $([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
  loaders: []
  dependencies:
$deps
  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: $([int]$ver[0])
    minor: $([int]$ver[1])
    patch: $([int]$ver[2])
  enabled: true
"@

if (-not (Test-Path $modsYml) -or ((Get-Content $modsYml -Raw).Trim() -match '^\[?\]?$')) {
    [System.IO.File]::WriteAllText($modsYml, $entry)
} else {
    $existing = [System.IO.File]::ReadAllText($modsYml)
    $blocks   = ($existing -split '(?m)(?=^- manifestVersion:)') |
                Where-Object { $_.Trim() -ne '' -and $_ -notmatch '  name: stysk1-Scopophobia\b' } |
                ForEach-Object { $_.TrimEnd() + "`n" }
    [System.IO.File]::WriteAllText($modsYml, (($blocks + $entry) -join '').TrimStart())
}

Write-Host "Updated mods.yml  -> $modsYml" -ForegroundColor Green

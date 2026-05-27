#Requires -Version 5
<#
.SYNOPSIS
    Build the mod and assemble a Thunderstore-format zip in ./dist for sharing or upload.
.DESCRIPTION
    Produces dist/<name>-<version>.zip (name + version from Thunderstore/manifest.json).
    Layout matches Thunderstore expectations: manifest.json, icon.png, README.md, CHANGELOG.md
    at the root, and BepInEx/plugins/{Scopophobia.dll, scp096} inside.
    ScopophobiaPlugin.cs loads the scp096 AssetBundle from disk via AssetBundle.LoadFromFile,
    so the bundle must ship alongside the DLL.
.EXAMPLE
    ./package.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$tsDir    = Join-Path $root "Thunderstore"
$manifest = Get-Content (Join-Path $tsDir "manifest.json") -Raw | ConvertFrom-Json
$name    = $manifest.name
$version = $manifest.version_number

Write-Host "Packaging $name v$version..." -ForegroundColor Cyan

Write-Host "Restoring tools..." -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed." }

Write-Host "Building ($Configuration)..." -ForegroundColor Cyan
dotnet build "$root\src\Scopophobia.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$dll = Join-Path $root "src\bin\$Configuration\netstandard2.1\Scopophobia.dll"
if (-not (Test-Path $dll)) { throw "Build output not found: $dll" }

$dist        = Join-Path $root "dist"
$staging     = Join-Path $dist "_staging"
$stagingPlug = Join-Path $staging "BepInEx\plugins"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stagingPlug | Out-Null

Copy-Item (Join-Path $tsDir "manifest.json")  (Join-Path $staging "manifest.json")  -Force
Copy-Item (Join-Path $tsDir "icon.png")       (Join-Path $staging "icon.png")       -Force
Copy-Item (Join-Path $tsDir "README.md")      (Join-Path $staging "README.md")      -Force
Copy-Item (Join-Path $tsDir "CHANGELOG.md")   (Join-Path $staging "CHANGELOG.md")   -Force
Copy-Item $dll                                (Join-Path $stagingPlug "Scopophobia.dll") -Force

$bundle = Join-Path $tsDir "BepInEx\plugins\scp096"
if (-not (Test-Path $bundle)) { throw "scp096 bundle not found: $bundle" }
Copy-Item $bundle                             (Join-Path $stagingPlug "scp096") -Force

$zip = Join-Path $dist "$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force
Remove-Item $staging -Recurse -Force

Write-Host "Packaged: $zip" -ForegroundColor Green

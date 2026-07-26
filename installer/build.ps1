# Publishes MacAccents self-contained (bundles the .NET runtime, so end users
# need nothing installed) and compiles the Inno Setup installer.
#
# Usage:  powershell -ExecutionPolicy Bypass -File installer\build.ps1 [-Version 1.2.3]

param(
    # Version for the assembly and installer. Defaults to the csproj/iss values.
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'MacAccents.csproj'
$publishDir = Join-Path $repoRoot 'publish'

Write-Host '==> Publishing self-contained (win-x64)...' -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
$publishArgs = @($project, '-c', 'Release', '-r', 'win-x64', '--self-contained', '-o', $publishDir)
if ($Version) { $publishArgs += "-p:Version=$Version" }
dotnet publish @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# Locate the Inno Setup compiler.
$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $iscc) {
    throw 'Inno Setup (ISCC.exe) not found. Install it with: winget install JRSoftware.InnoSetup'
}

Write-Host '==> Compiling installer...' -ForegroundColor Cyan
$isccArgs = @()
if ($Version) { $isccArgs += "/DMyAppVersion=$Version" }
$isccArgs += (Join-Path $PSScriptRoot 'MacAccents.iss')
& $iscc @isccArgs
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }

Write-Host '==> Done. Installer written to installer\dist.' -ForegroundColor Green

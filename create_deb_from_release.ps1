#!/usr/bin/env pwsh

function Write-Info($msg) { Write-Host "[INFO] $msg" }
function Write-Err($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
# reuse same ReleaseDir as linux_install.ps1
$ReleaseDir = Join-Path $ScriptDir 'RadI0/bin/Release/net10.0/linux-x64'

param(
    [string]$ReleasePath = $ReleaseDir,
    [string]$Version = $(if (Test-Path (Join-Path $ScriptDir 'version.txt')) { Get-Content (Join-Path $ScriptDir 'version.txt') -Raw } else { '0.0.0' }),
    [string]$OutDir = $(Join-Path $ScriptDir 'out')
)

if (-not (Test-Path $ReleasePath -PathType Container))
{
    Write-Err "Release directory not found: $ReleasePath"
    exit 1
}

if (-not (Get-ChildItem -Path $ReleasePath -Force -ErrorAction SilentlyContinue | Select-Object -First 1))
{
    Write-Err "No files found in release directory: $ReleasePath"
    exit 1
}

$pkgName = "radi0"
$arch = "amd64"
$versionClean = $Version.Trim()
$debFilename = "${pkgName}_${versionClean}_${arch}.deb"

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

# create temp build root
$tmpRoot = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "${pkgName}_pkg_$([Guid]::NewGuid().ToString('N'))")
New-Item -ItemType Directory -Path $tmpRoot -Force | Out-Null

try {
    $optDir = Join-Path $tmpRoot 'opt'
    $pkgOpt = Join-Path $optDir 'RadI0'
    New-Item -ItemType Directory -Path $pkgOpt -Force | Out-Null

    Write-Info "Copying release files into package layout ($pkgOpt)"
    Get-ChildItem -Path $ReleasePath -Force | ForEach-Object {
        $src = $_.FullName
        $dest = Join-Path $pkgOpt $_.Name
        if ($_.PSIsContainer) { Copy-Item -Path $src -Destination $dest -Recurse -Force }
        else { Copy-Item -Path $src -Destination $dest -Force }
    }

    # Ensure main executable is executable
    $mainExe = Join-Path $pkgOpt 'RadI0'
    if (Test-Path $mainExe) {
        Write-Info "Making $mainExe executable"
        & chmod +x $mainExe
    }

    # Create DEBIAN control
    $debianDir = Join-Path $tmpRoot 'DEBIAN'
    New-Item -ItemType Directory -Path $debianDir -Force | Out-Null

    $maintainer = "${env:USER} <${env:USER}@localhost>"
    $control = @"
Package: $pkgName
Version: $versionClean
Section: utils
Priority: optional
Architecture: $arch
Maintainer: $maintainer
Description: RadI0 radio application
"@

    $controlPath = Join-Path $debianDir 'control'
    $control | Out-File -FilePath $controlPath -Encoding UTF8 -Force

    Write-Info "Building .deb package"
    $debPath = Join-Path $OutDir $debFilename

    $fakeroot = Get-Command fakeroot -ErrorAction SilentlyContinue
    $dpkg = Get-Command dpkg-deb -ErrorAction SilentlyContinue
    if (-not $dpkg) { Write-Err "dpkg-deb not found. Install dpkg-deb (from dpkg package) and retry."; exit 1 }

    if ($fakeroot) {
        Write-Info "Using fakeroot to build package"
        & fakeroot -- dpkg-deb --build $tmpRoot $debPath
    }
    else {
        Write-Info "fakeroot not found. Building package without fakeroot (ownership in package may be current user)."
        & dpkg-deb --build $tmpRoot $debPath
    }

    if (Test-Path $debPath) {
        Write-Info "Package created: $debPath"
    }
    else {
        Write-Err "Failed to create package"
        exit 1
    }
}
finally {
    Write-Info "Cleaning temporary build root: $tmpRoot"
    try { Remove-Item -LiteralPath $tmpRoot -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

Write-Info "Done."

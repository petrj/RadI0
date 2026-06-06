#!/usr/bin/env pwsh

function Write-Info($msg)
{
    Write-Host "[INFO] $msg"
}

function Write-Err($msg)
{
    Write-Host "[ERROR] $msg" -ForegroundColor Red
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ReleaseDir = Join-Path $ScriptDir 'RadI0/bin/Release/net10.0/linux-x64'
$TargetDir = '/opt/RadI0'

if (-not (Test-Path $ReleaseDir -PathType Container))
{
    Write-Err "Release directory not found: $ReleaseDir"
    Write-Err "Build the Linux release first and rerun this script."
    exit 1
}

if (-not (Get-ChildItem -Path $ReleaseDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1))
{
    Write-Err "No files found in release directory: $ReleaseDir"
    exit 1
}

Write-Info "Found release directory: $ReleaseDir"

if (-not (Test-Path $TargetDir -PathType Container))
{
    Write-Info "$TargetDir does not exist. Creating it with sudo."
    & sudo mkdir -p $TargetDir
    & sudo chown -R "$env:USER:$env:USER" $TargetDir
}
else
{
    Write-Info "$TargetDir already exists."
}

# Check writability by attempting to create a temp file
$writable = $false
try
{
    $tmp = Join-Path $TargetDir (".writetest_$([Guid]::NewGuid())")
    New-Item -Path $tmp -ItemType File -Force | Out-Null
    Remove-Item -Path $tmp -Force
    $writable = $true
}
catch
{
    $writable = $false
}

if (-not $writable)
{
    Write-Info "$TargetDir is not writable by $env:USER. Adjusting ownership with sudo."
    & sudo chown -R "$env:USER:$env:USER" $TargetDir
}

# Remove existing files from target (top-level children)
if (Get-ChildItem -Path $TargetDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1)
{
    Write-Info "Removing existing files from $TargetDir"
    Get-ChildItem -Path $TargetDir -Force | ForEach-Object -Process{
        $full = $_.FullName
        try
        {
            Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop
        }
        catch
        {
            Write-Info "Failed to remove ${full}: ${_}"
        }
    }
}

Write-Info "Copying release files to $TargetDir"
Get-ChildItem -Path $ReleaseDir -Force | ForEach-Object -Process {
    $src = $_.FullName
    $dest = Join-Path $TargetDir $_.Name
    if (Test-Path $dest)
    {
        Remove-Item -LiteralPath $dest -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($_.PSIsContainer)
    {
        Copy-Item -Path $src -Destination $dest -Recurse -Force -ErrorAction Stop
    }
    else
    {
        Copy-Item -Path $src -Destination $dest -Force -ErrorAction Stop
    }
}

$MainExecutable = Join-Path $TargetDir 'RadI0'
if (Test-Path $MainExecutable)
{
    Write-Info "Making $MainExecutable executable"
    & chmod +x $MainExecutable
}
else
{
    Write-Info "Main executable $MainExecutable not found, skipping chmod."
}

$DesktopFile = Join-Path $TargetDir 'RadI0.desktop'
if (Test-Path $DesktopFile)
{
    $desktopDir = Join-Path $env:HOME 'Desktop'
    if (-not (Test-Path $desktopDir))
    {
        New-Item -ItemType Directory -Path $desktopDir -Force | Out-Null
    }

    Write-Info "Copying $DesktopFile to $desktopDir"
    Copy-Item -Path $DesktopFile -Destination $desktopDir -Force
}
else
{
    Write-Info "Desktop file $DesktopFile not found, skipping desktop copy."
}

Write-Info "Installation complete. Files copied to $TargetDir"

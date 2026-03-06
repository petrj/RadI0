$scriptDir = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
Set-Location $scriptDir

$consoleProjectFolder = Join-Path -Path $scriptDir -ChildPath "RadI0\"
$consoleReleaseFolder = Join-Path -Path $consoleProjectFolder -ChildPath "bin\release\net10.0\"

if (-not (Test-Path $consoleReleaseFolder))
{
    throw "folder $consoleReleaseFolder not found"
}

if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)) 
{
    $installFolder = Join-Path -Path ([Environment]::GetFolderPath("ProgramFiles")) -ChildPath "RadI0"
    
    $IsAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

    if (-not $IsAdmin) {
        Start-Process powershell `
            -Verb RunAs `
            -ArgumentList "-File `"$PSCommandPath`""

        Write-Host "Installation complete"
        exit
    }

    if (-not (Test-Path $installFolder))
    {
        New-Item -ItemType Directory -Path $installFolder
    }    

} else
{
    $installFolder =  Join-Path -Path "/opt/" -ChildPath "RadI0"
}

if (-not (Test-Path $installFolder))
{
    throw "folder $installFolder not found"
}

Get-ChildItem -Path $installFolder -Recurse | Remove-Item -Force -Recurse
Copy-Item -Path $consoleReleaseFolder/* -Destination $installFolder -Recurse -Force


if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)) 
{ 

} else
{
    $appPath = Join-Path $installFolder -ChildPath "RadI0"
    chmod +x $appPath 
}

Write-Host "Installation complete"
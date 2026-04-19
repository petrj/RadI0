$scriptDir = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
Set-Location $scriptDir

foreach ($Runtime in @(
    "linux-x64",
    "linux-arm64",
    "linux-arm",
    "win-x64",
    "win-x86",
    "win-arm64"
    ))
    {
        ./MakeRelease.ps1 $Runtime
    }
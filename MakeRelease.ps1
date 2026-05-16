param($Runtime,[switch]$Clear,[switch]$AllRuntimes)
Set-Location $PSScriptRoot

./LoadBuildModule.ps1

if ($Clear)
{
    Write-Host "Clearing ..."
    ./Clear.ps1
}

if (-not (Test-Path "version.txt"))
{
    throw "Version file 'version.txt' not found"
} else
{
    Write-Host "Building version $(Get-Content "version.txt").Trim() ..."
}

$version = (Get-Content "version.txt").Trim()

if ($AllRuntimes)
{

foreach ($Runtime in @(
    "linux-x64",
    "linux-arm64",
    "linux-arm",
    "win-x64",
    "win-x86",
    "win-arm64"
    ))
    {
        Build-Project -Runtime $Runtime `
            -Configuration "Release" `
            -Compress `
            -Version $version `
            -SolutionPath $PSScriptRoot `
            -ProjectName "RadI0" `
            -ProjectFolder "RadI0"
    }
} else
{
    Build-Project -Runtime $Runtime `
            -Configuration "Release" `
            -Compress `
            -Version $version `
            -SolutionPath $PSScriptRoot `
            -ProjectName "RadI0" `
            -ProjectFolder "RadI0"
}


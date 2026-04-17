
$scriptDir = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)

Set-Location $scriptDir

$Version = Get-Content -Path "version.txt"

$radIOProjectFolder = Join-Path -Path $scriptDir -ChildPath "RadI0\"
$radIOReleaseFolder = Join-Path -Path $radIOProjectFolder -ChildPath "bin\release\net10.0\"

$releaseFileName = "RadI0"

./Clear.ps1
dotnet build $radIOProjectFolder\RadI0.csproj --configuration=release -property:Version=$Version

$rid = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
$releaseFileName += ".$rid"


if (Test-Path "/etc/os-release")
{
    $osInfo = Get-Content /etc/os-release
    $nameLine = $osInfo | Where-Object { $_ -like "ID=*" }
    $distro = $nameLine.Split("=")[1].Replace('"','')
    $releaseFileName += ".$distro"

    $kernel = (uname -r)
    $releaseFileName += ".$kernel"
}

$releaseFileName += ".";
$releaseFileName += $Version;
$releaseFileName += ".zip";

$radIOReleaseFolder

Compress-Archive `
    -Path (Get-ChildItem -Path $radIOReleaseFolder -File | Select-Object -ExpandProperty "FullName") `
    -CompressionLevel Fastest `
    -DestinationPath $releaseFileName `
    -Force `
    -Verbose

Write-Host "Saved to $releaseFileName"

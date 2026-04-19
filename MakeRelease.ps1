Param($Runtime)

$Runtimes = @(
    "linux-x64",
    "linux-arm64",
    "linux-arm",
    "win-x64",
    "win-x86",
    "win-arm64"
    )

if ([String]::IsNullOrWhiteSpace($Runtime))
{
    for ($i=1; $i -le $Runtimes.Count; $i++)
    {
        $r = $Runtimes[$i-1]
        Write-host "$i) $r"
    }

    Write-host "Select Runtime [1]: " -NoNewline
    $numAsString = Read-Host

    if ($numAsString -eq "")
    {
        $numAsString = "1"
    }

    $num = 0;
    if ([int]::TryParse($numAsString,[ref] $num) `
        -and ($num -ge 1) `
        -and ($num -le $Runtimes.Count))
        {
            $Runtime = $Runtimes[$num-1]
        } else
        {
            throw "Invalid input"
        }
} else
{
    if (-not $Runtimes.Contains($Runtime))
    {
        throw "Invalid input Runtime param"
    }
}

$scriptDir = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
Set-Location $scriptDir

$Version = Get-Content -Path "version.txt"

$radIOProjectFolder = Join-Path -Path $scriptDir -ChildPath "RadI0"
$radIOReleaseFolder = Join-Path -Path $radIOProjectFolder -ChildPath "bin\Release\net10.0\$Runtime\publish"

$releaseFileName = "RadI0"
$releaseFileName += ".v";
$releaseFileName += $Version;
$releaseFileName += ".";
$releaseFileName += "$Runtime";

Write-Host "Creating RadI0 release"
Write-Host "    Configuration  : Release"
Write-Host "    Runtime        : $Runtime"
Write-Host "    Publish folder : $radIOReleaseFolder"
Write-Host "    Release name   : $releaseFileName"

dotnet publish $radIOProjectFolder\RadI0.csproj -c Release -r $Runtime

if (Test-Path $releaseFileName)
{
    Write-Host "Deleting old $releaseFileName"
    Remove-Item $releaseFileName -Force -Verbose
}

if ($Runtime.StartsWith("linux"))
{
    $releaseFileName += ".tar.xz";

    Write-Host "Creating $releaseFileName ...."

    tar -cvJf $releaseFileName -C $radIOReleaseFolder .
} else
{
    $releaseFileName += ".7z";

    Write-Host "Creating $releaseFileName ...."

    7z a -mx=9 $releaseFileName $radIOReleaseFolder/*
}

Write-Host "Relase Saved to $releaseFileName"

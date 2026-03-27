$scriptPath = $PSScriptRoot
cd $PSScriptRoot

Write-Host 'Clearing local NuGet caches...'
dotnet nuget locals --clear all

foreach ($folder in `
    @(
    "RadI0\bin",
    "RadI0\obj",
    "Examples\bin",
    "Examples\obj",
    "RTLSDR\bin",
    "RTLSDR\obj",
    "RTLSDR.Audio\bin",
    "RTLSDR.Audio\obj",
    "RTLSDR.Common\bin",
    "RTLSDR.Common\obj",
    "RTLSDR.FM\bin",
    "RTLSDR.FM\obj",
    "RTLSDR.DAB\bin",
    "RTLSDR.DAB\obj",
    "Tests\bin",
    "Tests\obj",
    ".vs"
     ))
{
    $fullPath = [System.IO.Path]::Combine($scriptPath,$folder)

    Write-Host "Clearing $folder..."



    if (-not $fullPath.EndsWith("\"))
    {
            $fullPath += "\"
    }

    if (Test-Path -Path $fullPath)
    {
	    Remove-Item -Path $fullPath -Recurse -Force
    }
}


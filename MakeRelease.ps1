param($Runtime,[switch]$Clear,[switch]$AllRuntimes)

Push-Location $PSScriptRoot

if ($Clear)
{
    Write-Host "Clearing ..."
    ./Clear.ps1
}

function Build-Project
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Runtime,

        [Parameter(Mandatory = $false)]
        [string]$Configuration = "Release"
    )
    process
    {
        # Define supported runtimes in a single place for easy maintenance
        $AllowedRuntimes = @(
            "linux-x64", "linux-arm64", "linux-arm",
            "win-x64", "win-x86", "win-arm64"
        )

        # 1. Interactive Runtime selection if not provided via parameter
        if ([string]::IsNullOrWhiteSpace($Runtime))
        {
            Write-Host "Available Runtimes:" -ForegroundColor Cyan
            for ($i = 0; $i -lt $AllowedRuntimes.Count; $i++)
            {
                Write-Host ("{0}) {1}" -f ($i + 1), $AllowedRuntimes[$i])
            }

            $selection = Read-Host "Select Runtime [default: 1]"
            if ([string]::IsNullOrWhiteSpace($selection)) { $selection = "1" }

            $idx = -1

            if ([int]::TryParse($selection, [ref]$idx) -and $idx -ge 1 -and $idx -le $AllowedRuntimes.Count)
            {
                $Runtime = $AllowedRuntimes[$idx - 1]
            }
            else
            {
                throw "Invalid selection."
            }
        }
        # Validate provided Runtime parameter
        elseif ($Runtime -notin $AllowedRuntimes)
        {
            throw "Invalid Runtime '$Runtime'. Supported values: $($AllowedRuntimes -join ', ')"
        }

        # 2. Setup paths and versioning
        # Using Push-Location to ensure we work from the script's root directory
        Push-Location $PSScriptRoot
        try
        {
            if (-not (Test-Path "version.txt"))
            {
                throw "Version file 'version.txt' not found in $PSScriptRoot"
            }

            $version = (Get-Content "version.txt").Trim()
            $projectPath = "RadI0/RadI0.csproj"
            $publishDir = "RadI0/bin/$Configuration/net10.0/$Runtime/publish"
            $baseFileName = "RadI0.v$version.$Runtime"

            if ($Runtime -like "linux*")
            {
                $archiveName = "$baseFileName.tar.xz"
            }
            else
            {
                $archiveName = "$baseFileName.7z"
            }

            Write-Host "--- Starting Build Process ---" -ForegroundColor Green
            Write-Host "Configuration : $Configuration"
            Write-Host "Runtime       : $Runtime"
            Write-Host "Output Name   : $archiveName"

            # 3. Execute dotnet publish
            dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained
            if ($LASTEXITCODE -ne 0) { throw "Dotnet publish failed with exit code $LASTEXITCODE." }

            if (Test-Path $archiveName) { Remove-Item $archiveName -Force }

            # 4. Create Archive based on OS platform
            if ($Runtime -like "linux*")
            {
                Write-Host "Compressing to TAR.XZ..." -ForegroundColor Gray

                & tar -cvJf $archiveName -C $publishDir .
            }
            else
            {
                Write-Host "Compressing to 7z..." -ForegroundColor Gray

                & 7z a -mx=9 $archiveName "./$publishDir/*"
            }

            Write-Host "Success! Build saved to: $archiveName" -ForegroundColor Green

            Return Get-Item -Path $archiveName
        }
        catch
        {
            Write-Error "Build failed: $_"
            throw
        }
    }
}

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
        Build-Project -Runtime $Runtime -Configuration "Release"
    }
} else
{
    Build-Project -Runtime $Runtime -Configuration "Release"
}

Pop-Location

cd $PSScriptRoot

Function Get-SecureStringFromUserInput
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$false)]
        [string] $Message = 'Enter password:',

        [Parameter(Mandatory=$false)]
        [string] $EnvironmentVariable = $null
    )
    Process
    {
        Write-Host $Message -NoNewline

        if (-not ([String]::IsNullOrEmpty($EnvironmentVariable)))
        {
            $plainToken = $EnvironmentVariable
            Write-Host ".. using environment variable"
        } else
        {

            $secureToken = Read-Host -AsSecureString

            $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
            try
            {
                $plainToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
            }
            finally
            {
                [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
            }
        }

        Write-Output $plainToken
    }
}

$token = Get-SecureStringFromUserInput -Message "Enter SonarQube token:" -EnvironmentVariable $env:SONAR_TOKEN
$key = Get-SecureStringFromUserInput -Message "Enter SonarQube project key:" -EnvironmentVariable $env:SONAR_KEY
$url = Get-SecureStringFromUserInput -Message "Enter SonarQube project url:" -EnvironmentVariable $env:SONAR_URL

$sonarExclusions = "**/bin/**,**/obj/**,Tests/**"
$sonarTool = "dotnet-sonarscanner"
$testProject = "Tests/Tests.csproj"
$testResultsDir = Join-Path $PSScriptRoot "TestResults"
$testResultsPattern = Join-Path $testResultsDir "*.trx"
$coveragePattern = Join-Path $testResultsDir "*" "coverage.opencover.xml"

$installedTool = dotnet tool list -g 2>$null | Select-String -Pattern "^$sonarTool\s" | ForEach-Object { $_.Line }
if (-not $installedTool)
{
    Write-Host "$sonarTool not found. Installing..."
    dotnet tool install --global $sonarTool
}
else
{
    Write-Host "$sonarTool is already installed."
}

if (Test-Path $testResultsDir)
{
    Remove-Item -Recurse -Force $testResultsDir
}
New-Item -ItemType Directory -Path $testResultsDir | Out-Null

dotnet sonarscanner begin /k:"$key" /d:sonar.host.url="$url" /d:sonar.token="$token" /d:sonar.exclusions="$sonarExclusions" /d:sonar.cs.vstest.reportsPaths="$testResultsPattern" /d:sonar.cs.opencover.reportsPaths="$coveragePattern"
dotnet build

Write-Host "Running unit tests and generating TRX + OpenCover coverage report..."
dotnet test $testProject --logger "trx;LogFileName=TestResults.trx" --results-directory "$testResultsDir" --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

if (-not (Get-ChildItem -Path $coveragePattern -ErrorAction SilentlyContinue))
{
    Write-Host "WARNING: No coverage.opencover.xml file found in $testResultsDir"
}

dotnet sonarscanner end /d:sonar.token="$token"
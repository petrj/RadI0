cd $PSScriptRoot

function Get-SecureStringFromUserInput
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string] $Message = 'Enter password:',

        [Parameter(Mandatory=$false)]
        [switch] $AsPlainText
    )

    process {
        Write-Host $Message -NoNewline
        $secureToken = Read-Host -AsSecureString

        $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
        try {
            $plainToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
        }
        finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
        }

        if ($AsPlainText) {
            Write-Output $plainToken
        }
        else {
            Write-Output $secureToken
        }
    }
}

if ([string]::IsNullOrEmpty($env:SONAR_TOKEN))
{
    $token = Get-SecureStringFromUserInput -AsPlainText -Message "Enter SonarQube token:"
}
else
{
    Write-Host "Using SONAR_TOKEN from environment variable."
    $token = $env:SONAR_TOKEN
}

if ([string]::IsNullOrEmpty($env:SONAR_KEY))
{
    $key = Get-SecureStringFromUserInput -AsPlainText -Message "Enter SonarQube project key:"
}
else
{
    Write-Host "Using SONAR_KEY from environment variable."
    $key = $env:SONAR_KEY
}

dotnet tool install --global dotnet-sonarscanner
dotnet sonarscanner begin /k:"$key" /d:sonar.host.url="http://sonarqube.diz"  /d:sonar.token="$token"
dotnet build
dotnet sonarscanner end /d:sonar.token="$token"
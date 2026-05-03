Set-Location $PSScriptRoot

Import-Module ../Powershell/BuildModule/BuildModule.psd1

$token = Get-SecureStringFromUserInput -Message "Enter github access token:" -EnvironmentVariable $env:GITHUB_TOKEN

#Publish-Nuget -ProjectName "RTLSDR" -PackageVersion "1.5.0" -SolutionPath $PSScriptRoot -Token $token
#Publish-Nuget -ProjectName "RTLSDR.Common" -PackageVersion "1.5.4" -SolutionPath $PSScriptRoot -Token $token
#Publish-Nuget -ProjectName "RTLSDR.Audio" -PackageVersion "1.5.0" -SolutionPath $PSScriptRoot -Token $token
Publish-Nuget -ProjectName "RTLSDR.FM" -PackageVersion "1.5.1" -SolutionPath $PSScriptRoot -Token $token
#Publish-Nuget -ProjectName "RTLSDR.DAB" -PackageVersion "1.6.1" -SolutionPath $PSScriptRoot -Token $token
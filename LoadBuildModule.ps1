# Loading Powershell.Modules BuildModule

$maxVersion = Get-ChildItem "$env:HOME/.nuget/packages/powershell.modules/" | Select-Object -Property Name -ExpandProperty Name | sort-object -Descending | Select-Object -First 1
$modulePath = "$env:HOME/.nuget/packages/powershell.modules/$maxVersion/Powershell.Modules/"

if (Get-Module -Name BuildModule)
{
    Write-Host "Reloading BuildModule module version $maxVersion..."
    Remove-Module BuildModule
} else
{
    Write-Host "Loading BuildModule module version $maxVersion from $modulePath ..."
}

Import-Module $modulePath/BuildModule/BuildModule.psd1

#$FullPackageName="16be7b03-4f94-4253-9e99-bf965fe11010_1.0.0.0_arm__ptkdchz042z66"
$FullPackageName="16be7b03-4f94-4253-9e99-bf965fe11010_ptkdchz042z66"

$PackageFolder="C:\Data\Users\DefaultAccount\AppData\Local\Packages\$FullPackageName"
if(!(Test-Path -Path $PackageFolder))
{
    Write-Host "Package Folder not found"
    Exit
}

if(!(Test-Path -Path "$PackageFolder\LocalState\update.ps1"))
{
    Exit
}

."$PackageFolder\LocalState\update.ps1"
Remove-Item "$PackageFolder\LocalState\update.ps1"
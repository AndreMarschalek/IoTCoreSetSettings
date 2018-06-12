#$FullPackageName="16be7b03-4f94-4253-9e99-bf965fe11010_1.0.0.0_arm__ptkdchz042z66"
$FullPackageName="16be7b03-4f94-4253-9e99-bf965fe11010_ptkdchz042z66"

$PackageFolder="C:\Data\Users\DefaultAccount\AppData\Local\Packages\$FullPackageName"
if(!(Test-Path -Path $PackageFolder))
{
    Write-Host "Package Folder not found"
    Exit
}
hostname > $PackageFolder\LocalState\boot.cfg
netsh interface ipv4 show config >> $PackageFolder\LocalState\boot.cfg
date >> $PackageFolder\LocalState\boot.cfg
# IoTCoreSetSettings
Windows IoT Core Proof of Concept UWP App to change Networking Settings

#### why
because UWP Apps are running in a "secured sandboxed" environment its impossible to change some settings directly from the App itself

#### install
###### Visual Studio
1. deploy the Project to your IoT Core device
2. in Visual Studio switch to "Output Window" and "Show output from: Build"
3. find a line similiar like this one and copy the Full package name

"1>Deployment complete (2011ms). Full package name: "16be7b03-4f94-4253-9e99-bf965fe11010_1.0.0.0_arm__ptkdchz042z66"

###### Device
1. edit boot.ps1 and update.ps1 from scripts and replace the content of $FullPackageName with the copied one and remove "1.0.0.0_arm__"
2. copy the content of scripts to your IoT Core device (for example c:\)
  
_(the scheduler is not able to directly run powershell scripts)_  
  
create a new scheduled task that runs as admin after boot and points to boot.bat  
<code>Powershell
schtasks /create /ru Administrator /rp REPLACEME /sc onstart /tn "Booter" /tr "C:\scripts\boot.bat"
</code>

create a new scheduled task that runs as admin every x minutes and points to update.bat  
<code>Powershell
schtasks /create /ru Administrator /rp REPLACEME /sc minute /mo 5 /tn "Updater" /tr "C:\scripts\update.bat"
</code>

(to delete)  
<code>Powershell
schtasks /delete /tn "Booter|Updater" /f
</code>

#### how it works
###### read
after device reboot the "Booter" task call boot.bat, a "helper" to start the powershell script boot.ps1  
this powershell script generates a file named "boot.cfg" in the "LocalState" Folder from the App  
the App parse this file to fill the GUI (boot.cfg contains as last line a timestamp from the last run to help troubleshooting)  
###### change
a tap on "save" creates a powershell script which will perform the needed changes named "update.ps1" in the "LocalState" Folder from the App  
the scheduled task "Updater" checks every x Minutes if a file named "update.ps1" exists in the "LocalState" Folder and process the changes  
having said that its clear you have a delay between the tap on "save" until the changes become active, worst case its the maximum "/mo value"

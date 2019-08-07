# AutoNetowkrSelect
Turn off wifi when there's ethernet, turn on wifi when there's no ethernet
* Also checks when you logon or unlock
* Only useful for laptops and managers/mobile users with a dock

## Install
Create a shortcut to AutoNetworkSelect.exe and place it in shell:startup
* Preferably not from your downloads/code folder...

## Settings
You can use `wmic nic get NetConnectionID` or `netsh interface show interface` to show the names of network interfaces you have.

## Attribution
* Icons: www.iconsdb.com
* NativeWifi: https://archive.codeplex.com/?p=managedwifi
  * Added Disconnect method

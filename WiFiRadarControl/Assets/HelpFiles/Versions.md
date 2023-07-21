# Simple Wi-Fi Analyzer Versions

## Version 1.9 supports technically invalid QR codes

I've got reports that some devices are creating non-standard wifi: URLs where the fields are in the wrong order. This version will allow these incorrect URLs.


## Version 1.8 Supports latest Wi-Fi standards

The newest [MSDN](https://learn.microsoft.com/en-us/uwp/api/windows.devices.wifi.wifiphykind?view=winrt-22621&source=docs) lists  a new Wi-Fi PHY kind which the app now reports. It also now handles the 6 GHz Wi-Fi bands.

A new part of the user experience is that you can scan for "all" Wi-Fi access points, or just those "close" to you. This reduces the visual clutter when you are in an area with a lot of Wi-Fi access points.

The Excel and CSV output in the Table now match the table display.

## Version 1.7 Tracks network changes

This version will track network changes better. The Wi-Fi name is updated in the status area. For networks with multiple access points (APs), the code will attempt to determine which AP the device is currently connected to and will highlight it in the RADAR screen.

Additionally, the Log output include more information about the access points.

The secret feature is more full-featured and allows for saving the resulting data as Excel or CSV.

## Version 1.4 + 1.5 + 1.6: Minor bug fixes

A small number of people report crashing problems; this version should fix that. Clearly it didn't for version 1.4, and hence the version 1.5 and later version 1.6 :-)

## Version 1.3: Add share for QR codes

Version 1.3 adds sharing and copying for QR codes and URLs. 
Also added: the WIFI: startup path has better error handling and should crash less.

## Version 1.2; Adding Connect, WIFI: URL and QR; October 2022

Version 1.2 adds some automation features:
1. It will show the QR code of your hotspot
2. It is a handler for the WIFI: protocol and will automatically connect to a hotspot or AP
3. Because it's a WIFI: handler, when you take a picture of a QR code with a hotspot, it will open the hotspot

Version 1.2 also fixes some style bugs and updates the Help tab to include the version information

## Version 1.1; Adding Hotspot; September 2022

Version 1.1 adds support for a Hotspot tab. This lets the user examine properties of the currently configured Wi-Fi Mobile Hotspot (otherwise known as a SoftAP). The user can also see some information on the list of current hotspot clients (remote devices that are accessing the hotspot) and can change the configuration.

## Version 1.0; initial version; August 2022

Initial version containing tabs for
- RADAR display showing access points sorted by strength
- Chart display showing crowding levels for each channel and band in use
- Table with complete information on each AP
- Log and Help display
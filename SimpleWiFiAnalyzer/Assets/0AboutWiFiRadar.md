# Project Overview

# Marketing Info

**Display Name** Simple Wi-Fi Analyzer
**Description**
Simple Wi-Fi Analyzer provides basic information about the Wi-Fi environment you are in. It provides a graphical RADAR-like display of the Wi-Fi access points in range, shows a frequency breakdown of the access points, and provides a table-like like of the details of each access point.

**Properties**

**Category** Utilities + Tools
[**Privacy Policy**](https://shipwrecksoftware.wordpress.com/2019/02/24/common-privacy-policy/)
[**Web Site**](https://shipwrecksoftware.wordpress.com/2022/09/06/simple-wi-fi-analyzer/) 
**Support Contact** shipwrecksoftware@live.com
**Store Listing**
**Key features** includes Wi-Fi analyzer and Wi-Fi network analyzer


# Status
## Project current status and todo

* Add menu instead of silly buttons for copy?
* **DONE** convert IANA network type
* **DONE** Enable the progress ring while scanning
* **DONE** Add status that shows which network user is connected to

* **DONE** Output as HTML to paste into Excel!
* **DONE** Add "copy as CSV and "copy as Excel" to table!
* **DONE** Help system (standard Markdown style, of course)

## Bugs
* check all NOTE: TODO: DBG:
* click on table is slow for no good reason?
* **DONE** notify the user on error e.g. the try/catch on ScanAsync

## Project far backlog
* Continuous scans add 2D location Map to map out physical strength of specific network

User actions
* save scan
* ignore AP
* filter to AP
* give names to APs
* collapse multiple APs with same SSID? (e.g., ToomreHouse)


# Details about Mobile Access Points

Result: all of these APIs are for the "OnDemand" hotspot -- that's where there's a special app (My Phone?) that knows about special phones and can ask the phone to start a hotspot and then connect to it. So it's even remotely able to start a local hotspot :-(

WiFiAccessStatus
WiFiOnDemandHotspotAvailability { Available, Unavailable }
WiFiOnDemandHotspotCellularBars { .. 0 to 5 ..}
WiFiOnDemandHotspotConnectionResult :: .Status { .. many error codes .. }
WiFiOnDemandHotspotConnectTriggerDetails :: RequestedNetwork==WiFiOnDemandHotspotNetwork ConnectAsync()
WiFiOnDemandHotSpotNetwork :: GetOrCreateById + GetProperties, UpdateProperties
WiFiOnDemandHotspotNetworkProperties :: Availability CellularBar DisplayName IsMetered Password RemainingBatteryPercent Ssid


# Microsoft Documentation

See [Wi-Fi problems and your home layout](https://support.microsoft.com/en-us/windows/wi-fi-problems-and-your-home-layout-e1ed42e7-a3c5-d1be-2abb-e8fad00ad32a). It includes a list of search terms to try in the Windows Store:
* Wi-Fi network analyzer app from Microsoft Store [link](https://support.microsoft.com/windows/64203838-4029-7bba-8231-00c9d8f4d971#Category=Windows_11)
* Search for **Wi-Fi analyzer" (see below)
* Use the **netsh wlan show wlanreport** command from an elevated command prompt
* Walk around to find where your network is most powerful


Info on building an app at [Build a Wi-Fi Scanner in the UWP](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/july/modern-apps-build-a-wi-fi-scanner-in-the-uwp)


Don't bother looking at  [Advanced troubleshooting wirelss network connectivity](https://docs.microsoft.com/en-us/windows/client-management/advanced-troubleshooting-wireless-network-connectivity)



## What other apps are missing

### [WiFi Analyzer](https://apps.microsoft.com/store/detail/wifi-analyzer/9NBLGGH33N0N?hl=en-us&gl=US)

Reported issues
* Give alias to devices
* Allow saving to a log file
* Needs clear advice on network congestion and recommended channel
* UX isn't very clear

Most common use case is figuring out what channels are congested in order to switch channels on the router.

### [WiFi Analyzer and Scanner](https://apps.microsoft.com/store/detail/wifi-analyzer-and-scanner/9NBLGGH5QK8Q?hl=en-us&gl=US)

* Takes too long to realize the AP is gone (10 seconds)
* Too many ads
* Constantly switches recommendations (meaning: in my app, the connect AP should be discounted -- it will swamp the recommendations!)
* Should not be recommending channel 4 on 2.4 GHz!

**Loves**:
* Shows variation over time!


### [Free Wi-Fi Analyzer Tool](https://apps.microsoft.com/store/detail/free-wifi-analyzer-tool/9NBLGGH5XZ1Z?hl=en-us&gl=US)  **264**

* Would like to see connected equipement on networks
* Doesn't work with WPA2-Enterprise (!)

**Loves**

* Used by a small-business network person
* Likes the AP vendor information
* CLever graphing

### [Real WiFi](https://apps.microsoft.com/store/detail/real-wifi/9NRFGDVZ05RQ?hl=en-us&gl=US)

* Doesn't work with dongle-based Wi-Fi

### [Easy to use WiFi Analyzer](https://apps.microsoft.com/store/detail/easy-to-use-wifi-analyzer/9N75W2M2D55F?hl=en-us&gl=US)

No reviews

### [WiFi Scout](https://apps.microsoft.com/store/detail/wifi-scout/9NBLGGH5XCQ9?hl=en-us&gl=US)

* Can't pick adapter
* Doesn't support 5 Ghz?\
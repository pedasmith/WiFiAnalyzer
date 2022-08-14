# Project Overview

## Project current status and todo

* **DONE** attach hotspots to the radar points. Hotspots should be marked with **?** for first scan, a W for a wireless point on second scan and C for the connected adapter
* **DONE** data is combo: found data and user data?
* **DONE** data is name+BSSID or full data when hover?
* **DONE** Show either table OR rings OR Scan Result text OR CSV OR ...
* Click on towers to show more data
* Show a central 'dot' (and faint ring radar lines?)(reticule)
* Better ring size selection -- e.g. the jump from 1 to 2 is bad

## Bugs

* **DONE** Cant click radar->table but can click radar->log->table
* **DONE** click on grid a column selected results in a crash

## Project far backlog
* Help system (standard Markdown style, of course)
* convert frequency to ban+ width
* Provide recommendation
* add Map to map out physical strength of specific network

User actions
* save scan
* ignore AP
* filter to AP



## Microsoft Documentations

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
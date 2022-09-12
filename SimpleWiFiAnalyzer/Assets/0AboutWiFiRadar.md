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
* Create handy Hotspot tab
* Create hotspot from WIFI: URL
* Connect to network from WIFI: URL
* Continuous scans add 2D location Map to map out physical strength of specific network

User actions
* save scan
* ignore AP
* filter to AP
* give names to APs
* collapse multiple APs with same SSID? (e.g., ToomreHouse)

# Details about the WIFI URL

Section 7 from the [Wi-Fi alliance](https://www.wi-fi.org/download.php?file=/sites/default/files/private/WPA3_Specification_v3.0.pdf). The Wi-Fi URL spec is just [7] IETF RFC 3986, Uniform Resource Identifier (URI): Generic Syntax, https://tools.ietf.org/html/rfc3986)

WIFI URI
This section defines the URI representation for Wi-Fi credentials using the "WIFI" URI scheme. The URI can be encoded 
in a QR code to provide a convenient means to provisioning the credentials to devices.
7.1 URI format
The URI is defined by [7] and formatted by the WIFI-qr ABNF rule:
```
WIFI-qr = “WIFI:” [type “;”] [trdisable “;”] ssid “;” [hidden “;”] [id “;”] [password “;”] [publickey “;”] “;”
type = “T:” *(unreserved) ; security type
trdisable = “R:” *(HEXDIG) ; Transition Disable value
ssid = “S:” *(printable / pct-encoded) ; SSID of the network
hidden = “H:true” ; when present, indicates a hidden (stealth) SSID is used 
id = “I:” *(printable / pct-encoded) ; UTF-8 encoded password identifier, present if the password 
has an SAE password identifier
password = “P:” *(printable / pct-encoded) ; password, present for password-based authentication
public-key = “K:” *PKCHAR ; DER of ASN.1 SubjectPublicKeyInfo in compressed form and encoded in 
“base64” as per [6], present when the network supports SAE-PK, else absent

printable = %x20-3a / %x3c-7e ; semi-colon excluded
PKCHAR = ALPHA / DIGIT / %x2b / %x2f / %x3d

From [7]: unreserved is ALPHA / DIGIT / "-" / "." / "_" / "~"

```
## Spec, continued
In this version of the specification, the URI supports provisioning of credentials for Wi-Fi networks using password-based 
authentication, and for unauthenticated (open and Wi-Fi Enhanced Open™) Wi-Fi networks.
If the "type" is present, its value is set to "WPA" and it indicates password-based authentication is used.
If the "type" is absent, it indicates an unauthenticated network (open or Wi-Fi Enhanced Open).
NOTE: This specification does not define usage of the WIFI URI with WEP shared key.
The value of "trdisable", if present, is set to a hexadecimal representation of the Transition Disable bitmap field (defined in 
Section 8). 
NOTE: "trdisable" allows transition modes to be disabled at initial configuration of a network profile, and therefore provides 
protection against downgrade attack on a first connection (e.g., before a Transition Disable indication is received from an 
AP).
The values of "ssid", "password", and "id" are, in general, octet strings. Octets that do not correspond to characters in the 
printable set defined in this ABNF rule are percent-encoded.
NOTE: The semi-colon is excluded from the printable set as defined in this ABNF rule, and therefore is percent-encoded.
NOTE: When the password is used with WPA2-Personal (including WPA3-Personal transition mode), it comprises only 
ASCII-encoded characters. When the password is used with only SAE, it comprises octets with arbitrary values. The SAE 
password identifier is a UTF-8 string.
Devices parsing this URI shall ignore semicolon separated components that they do not recognize in the WIFI-qr 
instantiation. Ignoring unknown components allows devices to be forward compatible with future extensions to this 
specification


## Everything wrong with the WPA3 WiFi: URL scheme

There's a clever new way to specific a Wi-Fi hotspot: use a QR Code! The Wi-Fi alliance, as part of their WPA3 spec, has create a Wi-Fi URL that can be easily embedded into a QRCode; the QR Code can then be used to connect to a Wi-Fi hotspot. But there's a bunch of problems with their spec.

1. Incomplete definitions. The spec says "... ignore semicolon separated **components** that they do not recognize ... ". Although the writers presumably meant that the sections like "S:*name*;" are the components, the word **component** isn't actually defined in the spec
2. Challenging error case parsing. The spec is very consistant: every component has a single-letter "opcode" followed by a colon. But that pattern isn't actually part of the spec. In theory, a malformed component could be considered to be a merely a component that isn't recognized. For example, a properly formatted SSID components might look like this: "S:example;". A malformed one might have a bad percent-encoded value: "S:%ZZ;". Note that %ZZ is not a valid percent-encoded value. The spec says we should *accept* this and simply assume that it's some new component that we just don't reconize.
3. Not formward-looking for names (ASCII, not UTF8). Although mobile Wi-Fi hotspots today cannot use Unicode characters (like cat-faces), that might change in the future. The WPA3 spec, instead of simply allowing UTF8 in the URL, doesn't specify the character encoding of the SSID and Passphrases. 
4. Rigid ordering. The spec requires each item to be a specific order. For example, you can't put the passphrase before the SSID. This is contrary to most expectations, and adds to the burden of the parser.
5. Bad semicolon requirements. For not obvious reason the URL is required to end with two semicolons. This is contrary to normal practice, where semicolons seperate list elements. Most small languages don't require any ending list seperator, let alone two of them.
6. No examples of correct and incorrect URLS. All parsers, when coded, should have unit tests that include a standard set of URLS.
7. Old-fashioned capitalization of the scheme.  [link](https://www.rfc-editor.org/rfc/rfc7595). Schemes are case-insenstive but should be registered as lower-case. ALl modern scheme examples are lower-case.
8. Doesn't handle incorrect // well. For example, imagine that this correct URL wifi:R:1122;S:starpainter;P:deeznuts;; is incorrectly marked with a double-slash like this: wifi://R1122;S:starpainter;P:deeznuts;;. Instead of failing, or handling the slashes, instead the parser must assume that the //R:1122; is merely an unrecognized new component with a password the deeznuts.
9. Can't distinguish between wanting to **connect** to a hot spot (the common case) and wanting to **establish** a hot spot (less common, but has clear use cases). There should be a way to specify that the hotspot should be created. Proposal: have a 'F' (flags) that allows a comma-seperated list of flags, one of which is 'create'. For example: wifi:S:starpainter;P:deeznuts;F:create;; will create a hotspot called starpoainter and the existing wifi:S:starpainter;P:deeznuts;; would connect to it.
10. No way to specify preferred band for creating hotspots. When creating a hotspot, the user might have a preferred band (2.4 GHz, 5 GHz, 6 Ghz), and that preference might be an ordered list. Proposal: **B:6,5,2.4;** will ask to use bands 6, 5, and 2.4.
11. The meaning of T (Type) is not clear. It can be either not present or WPA (meaning WPA using passwords). But what if it's some other value? For the H value, it's either not present, "true", or is an error. But the same clarity isn't present for T (type).

The scheme is not registered with [IANA](https://www.iana.org/assignments/uri-schemes/uri-schemes.xhtml). See [registration](https://www.iana.org/assignments/uri-schemes/prov/wifi).

# Down the garden path: On Demand Hotspot

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
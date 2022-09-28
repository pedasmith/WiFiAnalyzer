# Everything wrong with the Wifi: URL scheme

## Missing Features

In Windows, you have to specify whether you're connecting just the one time, or if it should be automatic. There is no way to specify this in the official definition

There are two things you might be doing with a wifi: URL: you might be connecting (the common case, and covered), or you might be setting up a Mobile Hotspot or AP. This is not covered. One solution is to have a 'wifisetup:' URL. Note that to set up a Wi-Fi you will need e.g. the band and channel information which is not present in the URL definition.

The URL fields specification is underdefined. Each field (e.g, "S:ssid") is definied seperatately but consistantly -- that is, each field in practice is <opcode>:<value>;. However, this is not technically required, meaning that future field might use a different specification. At the same time, parsers are told to accept potential new fields even though the new field definition is unknown.

Instead, the protocol should clearly define what a field must be. The field definitions should also clearly state that they will never include an unencoded semi-colon so that we can always split the URL into fields based on semi-colons.

The protocol clearly provides a rigid and required ordering of the fields. But the examples (e.g., in wikipedia) ignore those ordered.

The protocol requires that each wifi url end with two semi-colons. These are also absent in real-world examples.

The **T:WPA type is required** for Android. If you specify a url wifi:S:star216;P:deeznuts;; without the T:WPA;, and it's a Windows Hotspot, and you try to connect from Android, then it will fail. Why? Because Android will insist on opening as an open hotspot (but with a password), and fail.

## Links

The Wifi: URL scheme is defined by the [Wi-Fi.org](https://www.wi-fi.org/) in the [WPA-3 specification](https://www.wi-fi.org/download.php?file=/sites/default/files/private/WPA3_Specification_v3.0.pdf)

## wifi: scheme Summary

This is from page 24 of the WPA3™ Specification Version 3.0
```
WIFI-qr = “WIFI:” [type “;”] [trdisable “;”] ssid “;” [hidden “;”] [id “;”] [password “;”] [publickey “;”] “;”
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
```

|Command|Meaning
|-----|-----
|T|**Type**, must be WPA or nothing. Won't work as nothing on Android.
|R|**Transition Disable**
|S|**SSID** to connect to
|H|**Hidden**, must be true if present
|I|Password **Identifier**
|P|**Password**, must be percent encoded
|K|public-**key** in ASN.1 format

Examples are in the [Wikipedia](https://en.wikipedia.org/wiki/Wi-Fi) article:
```
A URI using the WIFI scheme can specify the SSID, encryption type, password/passphrase, and if the SSID is hidden or not, so users can follow links from QR codes, for instance, to join networks without having to manually enter the data.[121] A MECARD-like format is supported by Android and iOS 11+.[122]

Common format: WIFI:S:<SSID>;T:<WEP|WPA|blank>;P:<PASSWORD>;H:<true|false|blank>;
Sample WIFI:S:MySSID;T:WPA;P:MyPassW0rd;;
```

Explanation at [zxing](https://github.com/zxing/zxing/wiki/Barcode-Contents) which talks about the Android URI scheme. It's got more stuff in it than the WPA3 one.

Example: **wifi:S:starpainter;P:deeznuts**
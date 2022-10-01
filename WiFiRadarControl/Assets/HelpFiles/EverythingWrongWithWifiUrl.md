# Everything wrong with the Wifi: URL scheme

## Missing Features

In Windows, you have to specify whether you're connecting just the one time, or if it should be automatic. There is no way to specify this in the official definition

There are two things you might be doing with a wifi: URL: you might be connecting (the common case, and covered), or you might be setting up a Mobile Hotspot or AP. This is not covered. One solution is to have a 'wifisetup:' URL. Note that to set up a Wi-Fi you will need e.g. the band and channel information which is not present in the URL definition.

The protocol spec doesn't clearly define the fields. Each field today (e.g, "S:ssid") is definied seperatately but consistantly -- that is, each field in practice is *<opcode>:<value>;*, and neither the opcode nor the value includes a semicolon and the opcode never includes a colon. A good spec would first define what a field looks like and then list the meaning of the opcodes and details about the value. 


The protocol clearly provides a rigid and required ordering of the fields. But the examples (e.g., in wikipedia) ignore those ordered, and many examples in Github and the web use different orders.

The protocol requires that each wifi url end with two semi-colons. These are also absent in some real-world examples (and it's a silly requirement)

The **T:WPA type is required** for Android. If you specify a url wifi:S:star216;P:deeznuts;; without the T:WPA;, and it's a Windows Hotspot, and you try to connect from Android, then it will fail. Why? Because Android will insist on opening as an open hotspot (but with a password), and fail.

The **WIFI: scheme must be uppercase** for Apple. This is contrary to all URL RFCs where all schemes are case insensitive. It is also contrary to the WPA3 BNF definition, where the wifi is put in quotes. Quoted strings in RFCs are always case insensitive (IMHO, now an error with modern protocols)

## WIFI: parsers on Github

Github search is not powerful enough to find "wifi:" with the colon. Some links I did find by search for wifi:s: include

[SKYZYX](https://github.com/skyzyx/lambda-qr/wiki/Wi-Fi-Connection) lists fields for wifi:but it is just from [ZXING](https://github.com/zxing/zxing/wiki/Barcode-Contents). Assertions include: iOS support started in iOS 11 in 2017 and Android since 2010; order of fields (TSPH) does not matter; special chararacters to encode include quote, semicolon, comma, colon and should be escaped with \ (this is contrary to the WPA3) to match MeCard.


[ZXING](https://github.com/zxing/zxing/wiki/Barcode-Contents)

[DoCoMo](https://web.archive.org/web/20160304025131/https://www.nttdocomo.co.jp/english/service/developer/make/content/barcode/function/application/addressbook/index.html) MeCard format from DoCoMo. This is more a vague sketch of a description and not a detailed protocol.


## Links

The Wifi: URL scheme is defined by the [Wi-Fi.org](https://www.wi-fi.org/) in the [WPA-3 specification](https://www.wi-fi.org/download.php?file=/sites/default/files/private/WPA3_Specification_v3.0.pdf). However, they are merely regularizing an existing practice.

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

Example: **wifi:S:starpainter;P:deeznuts;;**
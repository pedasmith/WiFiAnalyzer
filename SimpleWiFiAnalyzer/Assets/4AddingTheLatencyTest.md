# Adding the Latency test

## Why now? Why this? How now?

I discovered, while looking for something totally different on the FCC.GOV website, that the American (USA) FCC (Federal Communications Commission) has created a speed and latency test for broadband and documented (roughtly) the protocol involved!

## Latency Timing

From the FCC Technical Description, page 5:

_Latency_: Measures the average round-trip time in milliseconds of up to 200UDP(User Datagram Protocol)data packets that are acknowledged as received individually within 2 seconds or are recorded as lost.  The packets are sent over a fixed 5-second time interval.No warm-up period is applied to latency.

Packets are sent for 5 seconds with a 2 second wait for packets, sending a maximum of 200 packets.
Algorithn is to send a packet and wait up to 2 seconds for the reply, sleeping 


## UWP Network hints


## Errata

**FCC Technical Description**

Page 9, Latency and Packet loss. The packets are described as being 160 bytes. According to the Samknows Github android code, the packets are actually 16 bytes. 60 bytes does work. Note that the reply packets are 16 bytes long regardless of the sent packet size.

Page 9: The technical description has each packet having as 8-byte sequence number and an 8-byte timestamp. The Github code instead has each packet as contains four four-byte (big endian) integers: a sequence number, a Unix seconds count, a milliseconds count, and a "magic" number

Page 9: The pseudo-code has the port being set to 5000. The Android code uses port 6000. AS of 2023-01-08, Port 5000 does not return any packets (seattle).

## Handy links.

[FCC Technical Description](https://www.fcc.gov/sites/default/files/2022_fcc_speed_test_app_technical_description.pdf)
[One of the servers](http://sp2-bdc-seattle-us.samknows.com/)
[SamKnows](https://samknows.com/)
[Github UDP search](https://github.com/SamKnows/skandroid-core/search?l=Java&q=udp)
[Github Samknows Android app](https://github.com/SamKnows/skandroid-core)


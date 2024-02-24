# Network Setup URL scheme

## Scheme and URL Format

Example: ``network:context:hotspot;action:start;S:myhotspot;P:mypassword;auth:WPA3;;``

The networksetup: URL is encoded like the **mecard** format. It starts with ``mecard:`` and then has a set of ``name:value;`` pairs, seperated with a semicolon, and overall ended with an additional semicolon. 

Each url includes a ``context`` (e.g., ``hotspot``) and an action (e.g., ``action:start``).

## context:hotspot

The ``hotspot`` context lets you start, stop, and report on a mobile hotspot. You can also use the ``start-session`` for newer version of Windows 11 to create a per-session mobile hotspot.

To start a hotspot (or a per-session hotspot) you must include an ``S`` and ``P`` values for the SSID and Password. The default auth type (``T``) is WPA2, although this may be change as WPA3 becomes more widespread. You can select different bands (2.4, 5, 6, and auto) which stand for 2.4 GHz, 5 Ghz and 6 Ghz. 

The priority is used only for the start-session action. The S and P fields are required; the rest are optional.

````
action: start start-session stop report
S:ssid
P:password
T:WPA2|WPA3+2|WPA3 D=WPA2
band:2.4|5|6|auto D=2.4
priority:normal|tethering D=normal
timeoutenabled:true|false D=do not change
````



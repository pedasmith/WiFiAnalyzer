using System;
using System.Collections.Generic;
using System.Text;

namespace SmartWiFiHelpers
{
    public static class IanaNetworkType
    {
        public static string Description(UInt32 ianaNetworkType)
        {
            switch (ianaNetworkType)
            {
                case 1: return "Some other type of network interface";
                case 6: return "Ethernet network interface";
                case 9: return "Token ring network interface";
                case 23: return "PPP network interface";
                case 24: return "Software loopback network interface";
                case 37: return "ATM network interface";
                case 71: return "Wi-Fi (IEEE 802.11)";
                case 131: return "Tunnel encapsulation network interface [VPN]";
                case 144: return "Firewire (IEEE 1394)";
            }
            return $"Other IANA type {ianaNetworkType}";
        }

    }
}

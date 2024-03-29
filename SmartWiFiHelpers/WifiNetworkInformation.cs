﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace SmartWiFiHelpers
{
    public class WiFiNetworkInformation
    {
        public string SSID { get; set; } // Is set with .OrUnnamed() so technically using it doesn't need the added .OrUnamed. I just put add it anyway.
        public string Bssid { get; set; }
        public string BandName { get; set; }
        public string ChannelName { get; set; }
        /// <summary>
        /// Bandwidth is in Megahertz
        /// </summary>
        public double Bandwidth { get; set; }
        /// <summary>
        /// Center frequency in Gigahertz (e.g., "2.4" for 2.4 GHz
        /// </summary>
        public double Frequency { get; set; }
        public int GetFrequenceInKilohertz() { return (int)Math.Round(Frequency * 1_000_000); }
        public double Rssi { get; set; }
        public double SignalBars { get; set; }
        public string PhyKind { get; set; }
        public TimeSpan Uptime { get; set; }
        public double BeaconInterval { get; set; }
        public string AuthenticationType { get; set; }
        public string EncryptionType { get; set; }
        public string IsWiFiDirect { get; set; }
        public string NetworkKind { get; set; }
        public string Notes { get; set; }
        public DateTimeOffset ScanTimeStamp { get; set; }

        // These can't be fields because they shouldn't be part of the grid.
        private WiFiAdapter Adapter;
        private WiFiAvailableNetwork AvailableNetwork;
        public WiFiAdapter GetAdapter() { return Adapter; }
        public void SetAdapter(WiFiAdapter adapter) { Adapter = adapter; }
        public WiFiAvailableNetwork GetAvailableNetwork() {  return AvailableNetwork; }
        public void SetAvailableNetwork(WiFiAvailableNetwork availableNetwork) { AvailableNetwork = availableNetwork; }

        public override string ToString()
        {
            return $"{SSID.OrUnnamed()} channel={ChannelName} band={BandName}";
        }

    }
}

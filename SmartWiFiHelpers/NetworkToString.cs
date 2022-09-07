﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace SmartWiFiHelpers
{
    static class NetworkToString
    {
        /// <summary>
        /// Amount to indent each time indent is indented
        /// </summary>
        static string Tab = "    ";
        public static string OrUnnamed(this string text, string defaultValue = "(no name)")
        {
            if (string.IsNullOrEmpty(text)) return defaultValue;
            return text;
        }

        public static string td(this string text)
        {
            var escaped = text; // TODO: actually escape!
            return $"<td>{escaped}</td>";
        }

        /// <summary>
        /// text is expected to be a string like <td>data</td><td>value</td>
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string tr(this string text)
        {
            return $"<tr>{text}</tr>\n";
        }
        /// <summary>
        /// trs is expected to be a series of tr lines
        /// </summary>
        /// <param name="trs"></param>
        /// <returns></returns>
        public static string html(this string trs)
        {
            return $"<html>\r\n<body>\r\n<table>{trs}</table>\r\n</body>\r\n</html>";
        }

        public static string ToString(string indent, AttributedNetworkUsage value)
        {
            if (value == null) return $"{indent}AttributedNetworkUsage does not exist\n";
            var retval = $"{indent}AttributedNetworkUsage {value.AttributionName}\n";
            indent += Tab;
            retval += $"{indent}BytesReceived={value.BytesReceived}\n";
            retval += $"{indent}BytesSent={value.BytesSent}\n";
            retval += $"{indent}AttributionId={value.AttributionId}\n";
            //TODO: Not including Thumbnail

            return retval;
        }

#if NEVER_EVER_DEFINED
        public static string ToCsvHeader_AttributedNetworkUsage()
        {
            return "AttributedNetworkUsageName,BytesReceived,BytesSent,AttributionId,";
        }
        public static string CsvData(AttributedNetworkUsage value)
        {
            var retval = $"{value.AttributionName},{value.BytesReceived},{value.BytesSent},{value.AttributionId},";
            return retval;
        }
#endif

        public static string ToString(string indent, ConnectionCost value)
        {
            if (value == null) return $"{indent}ConnectionCost does not exist\n";
            var retval = $"{indent}ConnectionCost\n";
            indent += Tab;
            retval += $"{indent}ApproachingDataLimit={value.ApproachingDataLimit}\n";
            retval += $"{indent}BackgroundDataUsageRestricted={value.BackgroundDataUsageRestricted}\n";
            retval += $"{indent}NetworkCostType={value.NetworkCostType}\n";
            retval += $"{indent}OverDataLimit={value.OverDataLimit}\n";
            retval += $"{indent}Roaming={value.Roaming}\n";
            return retval;
        }
        public static string ToString(string indent, ConnectionProfile value) //TODO: so much more
        {
            if (value == null) return $"{indent}ConnectionProfile does not exist\n";
            var retval = $"{indent}ConnectionProfile {value.ProfileName}\n";
            indent += Tab;
            // TODO: Get*() NetworkAdapter NetworkSecuritySettings
            retval += $"{indent}CanDelete={value.CanDelete}\n";
            retval += $"{indent}IsWlanConnectionProfile={value.IsWlanConnectionProfile}\n";
            retval += $"{indent}IsWwanConnectionProfile={value.IsWwanConnectionProfile}\n";
            retval += $"{indent}ServiceProviderGuid={value.ServiceProviderGuid}\n";

            // TODO: missing a bunch of Get*()
            /* Only supported in Phone
            var now = DateTime.UtcNow;
            var start = now.AddDays(-7.0); // one week;
            NetworkUsageStates states = new NetworkUsageStates();
            var anuList = await value.GetAttributedNetworkUsageAsync(start, now, states);
            foreach (var item in anuList)
            {
                retval += ToString(indent + Tab, item);
            }
            var ciList = await value.GetConnectivityIntervalsAsync(now, start, states);
            foreach (var item in ciList)
            {
                retval += ToString(indent + Tab, item);
            }
            */

            retval += $"{indent}NetworkConnectivityLevel={value.GetNetworkConnectivityLevel()}\n";
            retval += $"{indent}DomainConnectivityLevel={value.GetDomainConnectivityLevel()}\n";

            var names = value.GetNetworkNames();
            var namestr = "";
            foreach (var name in names)
            {
                namestr += name + " ";
            }
            retval += $"{indent}Names={namestr}\n";
            retval += $"{indent}SignalBars={value.GetSignalBars()}\n";

            if (value.NetworkAdapter != null)
            {
                retval += $"{indent}NetworkAdapter=not-null\n";
            }

            retval += ToString(indent, value.GetConnectionCost());
            retval += ToString(indent, value.WlanConnectionProfileDetails);

            return retval;
        }

        public static string ToString(string indent, ConnectivityInterval value)
        {
            if (value == null) return $"{indent}ConnectivityInterval does not exist\n";
            var retval = $"{indent}ConnectivityInterval Start={value.StartTime} Duration={value.ConnectionDuration}\n";
            return retval;
        }
        public static async Task<string> ToStringAsync(string indent, NetworkAdapter value)
        {
            if (value == null) return $"{indent}NetworkAdapter does not exist\n";
            var retval = $"{indent}NetworkAdapter\n";
            var profile = await value.GetConnectedProfileAsync();
            indent += Tab;
            retval += $"{indent}IanaInterfaceType={value.IanaInterfaceType} ({IanaNetworkType.Description(value.IanaInterfaceType)})\n";
            retval += $"{indent}InboundMaxBitsPerSecond={value.InboundMaxBitsPerSecond}\n";
            retval += $"{indent}OutboundMaxBitsPerSecond={value.OutboundMaxBitsPerSecond}\n";

            retval += ToString(indent, profile);
            retval += ToString(indent, value.NetworkItem);
            return retval;
        }

        public static string Decode(NetworkAuthenticationType value)
        {
            switch(value)
            {
                case NetworkAuthenticationType.None: return "None";
                case NetworkAuthenticationType.Unknown: return "Unknown";
                case NetworkAuthenticationType.Open80211: return "Open 802.11(Open80211)";
                case NetworkAuthenticationType.SharedKey80211: return "WEP Password(SharedKey80211)";
                case NetworkAuthenticationType.Wpa: return "WPA Enterprise (Wpa)";
                case NetworkAuthenticationType.WpaPsk: return "WPA Password (WpaPsk)";
                case NetworkAuthenticationType.WpaNone: return "WPA None(WpaNone)";
                case NetworkAuthenticationType.Rsna: return "Enterprise (Rsna)";
                case NetworkAuthenticationType.RsnaPsk: return "Password (RsnaPsk)";
                case NetworkAuthenticationType.Ihv: return "Custom (Ihv)";
                case NetworkAuthenticationType.Wpa3Sae: return "WPA3 Consumer (Wpa3Sae)";
                case NetworkAuthenticationType.Owe: return "Hotspot Opportunistic Wireless Encryption (Owe)";


                case NetworkAuthenticationType.Wpa3: return "WPA3 (Wpa3)"; //see MSDN for why this enum is 10 when the next is 10 also?
#if !SupportWin10
                //case NetworkAuthenticationType.Wpa3Enterprise192Bits: return "WPA3 Enterprise 192 (Wpa3Enterprise192Bits)";
                case NetworkAuthenticationType.Wpa3Enterprise: return "WPA3 Enterprise (Wpa3Enterprise)";
#else
                //case (NetworkAuthenticationType)10: return "WPA3 Enterprise 192 (Wpa3Enterprise192Bits)";
                case (NetworkAuthenticationType)13: return "WPA3 Enterprise (Wpa3Enterprise)";
#endif
            }
            return value.ToString();
        }
        // NetworkEncryptionType
        public static string Decode(NetworkEncryptionType value)
        {
            switch (value)
            {
                case NetworkEncryptionType.None: return "None";
                case NetworkEncryptionType.Unknown: return "Unknown";
                case NetworkEncryptionType.Wep: return "WEP";
                case NetworkEncryptionType.Wep40: return "WEP 40-bit (Wep40)";
                case NetworkEncryptionType.Wep104: return "WEP 104-bit (Wep104)";
                case NetworkEncryptionType.Tkip: return "TKIP";
                case NetworkEncryptionType.Ccmp: return "AES 802.11i (Ccmp)";
                case NetworkEncryptionType.WpaUseGroup: return "WPA (WpaUseGroup)";
                case NetworkEncryptionType.RsnUseGroup: return "802.11i RSN (RsnUseGroup)";
                case NetworkEncryptionType.Ihv: return "Custom (Ihv)";
#if !SupportWin10
                case NetworkEncryptionType.Gcmp: return "GCMP (Gcmp)";
                case NetworkEncryptionType.Gcmp256: return "GCMP 256-bit (Gcmp256)";

#else
                // The Windows 10 SDK doesn't have these values. When compiling on 
                // a Windows 10 box, we have to just provide the integers values
                // Values grabbed from https://docs.microsoft.com/en-us/uwp/api/Windows.Networking.Connectivity.NetworkEncryptionType?view=winrt-22621
                case (NetworkEncryptionType)10: return "GCMP (Gcmp)";
                case (NetworkEncryptionType)11: return "GCMP 256-bit (Gcmp256)";

#endif
            }
            return value.ToString();
        }
        public static string ToString(string indent, NetworkItem value)
        {
            if (value == null) return $"{indent}NetworkItem does not exist\n";
            var retval = $"{indent}NetworkItem NetworkId={value.NetworkId}\n";
            indent += Tab;
            var ntype = value.GetNetworkTypes();
            var nstr = "";
            if (ntype.HasFlag(NetworkTypes.Internet)) nstr += "Internet ";
            if (ntype.HasFlag(NetworkTypes.PrivateNetwork)) nstr += "Private ";
            if (ntype.HasFlag(NetworkTypes.None)) nstr += "None ";
            retval += $"{indent}NetworkTypes={nstr}\n";
            return retval;
        }
        public static string ToString(string indent, NetworkSecuritySettings value)
        {
            if (value == null) return $"{indent}NetworkSecuritySettings does not exist\n";
            var retval = $"{indent}NetworkSecuritySettings\n";
            indent += Tab;
            retval += $"{indent}NetworkAuthenticationType={value.NetworkAuthenticationType}\n";
            retval += $"{indent}NetworkEncryptionType={value.NetworkEncryptionType}\n";
            return retval;
        }
        public static void Fill(WiFiNetworkInformation data, NetworkSecuritySettings value)
        {
            data.AuthenticationType = Decode (value.NetworkAuthenticationType);
            data.EncryptionType = Decode (value.NetworkEncryptionType);
        }
        public static string ToCsvHeader_NetworkSecuritySettings()
        {
            return "AuthenticationType,EncryptionType,";
        }
        public static string ToHtmlHeader_NetworkSecuritySettings()
        {
            return "AuthenticationType".td()+"EncryptionType".td();
        }

        public static string ToCsvData(NetworkSecuritySettings value)
        {
            if (value == null) return $",,";
            return $"{value.NetworkAuthenticationType},{value.NetworkEncryptionType},";
        }
        public static string ToHtmlData(NetworkSecuritySettings value)
        {
            if (value == null) return $"".td()+"".td();
            return $"{value.NetworkAuthenticationType.ToString().td()}{value.NetworkEncryptionType.ToString().td()}";
        }

        public static async Task<string> ToStringAsync(string indent, WiFiAdapter value)
        {
            if (value == null) return $"{indent}WiFiAdapter does not exist\n";
            var retval = $"{indent}WiFiAdapter\n";
            var adap = await ToStringAsync(indent + Tab, value.NetworkAdapter);
            retval += adap;
            return retval;
        }

        public static string ToString(string indent, WiFiAvailableNetwork value)
        {
            if (value == null) return $"{indent}WiFiAvailableNetwork does not exist\n";
            var retval = $"{indent}WiFiAvailableNetwork Name={value.Ssid.OrUnnamed()} Bssid={value.Bssid}\n";
            indent += Tab;
            retval += $"{indent}BeaconInterval={value.BeaconInterval.TotalSeconds}\n";
            retval += $"{indent}ChannelCenterFrequencyInGigahertz={(double)value.ChannelCenterFrequencyInKilohertz/1000000.0}\n";
            retval += $"{indent}IsWiFiDirect={value.IsWiFiDirect}\n";
            retval += $"{indent}NetworkKind={value.NetworkKind}\n";
            retval += $"{indent}NetworkRssiInDecibelMilliwatts={value.NetworkRssiInDecibelMilliwatts}\n";
            retval += $"{indent}PhyKind={value.PhyKind}\n";
            retval += $"{indent}SignalBars={value.SignalBars}\n";
            retval += $"{indent}Uptime={value.Uptime}\n";

            retval += $"{ToString(indent, value.SecuritySettings)}\n";
            return retval;
        }
        public static void Fill(WiFiNetworkInformation dest, WiFiAvailableNetwork source, ScanMetadata smd)
        {
            dest.SSID = source.Ssid.OrUnnamed();
            dest.Bssid = source.Bssid;
            dest.BeaconInterval = source.BeaconInterval.TotalSeconds;
            dest.Frequency = (double)source.ChannelCenterFrequencyInKilohertz / 1_000_000.0; // Convert kilohertz to gigahertz
            dest.IsWiFiDirect = source.IsWiFiDirect ? "true" : "false";
            dest.NetworkKind = Decode(source.NetworkKind);
            dest.Rssi = source.NetworkRssiInDecibelMilliwatts;
            dest.PhyKind = Decode(source.PhyKind);
            dest.SignalBars = source.SignalBars;
            dest.Uptime = source.Uptime;
            dest.ScanTimeStamp = smd.ScanTime;

            Fill(dest, source.SecuritySettings);

            // Fill from the WiFiBandChannel
            var list = WiFiBandChannel.StaticWifiBandList;
            var index = WiFiBandChannel.Find(list, source.ChannelCenterFrequencyInKilohertz);
            if (index >= 0)
            {
                var wbc = list[index];
                dest.BandName = wbc.BandName;
                dest.ChannelName = wbc.ChannelName;
                dest.Bandwidth = wbc.BandwidthInKilohertzList[0] / 1000.0; // It's only a list because of the 900 MHz 802.11ah bands
                // Must convert khz to mhz to be more useful for people
            }
            else
            {
                ;
            }
            dest.SetAvailableNetwork(source);

        }
        public static string ToCsvHeader_WiFiAvailableNetwork()
        {
            return "WiFiSsid,Bssid,BeaconInterval,Frequency,IsWiFiDirect,NetworkKind,Rssi,PhyKind,SignalBars,Uptime,"
                + ToCsvHeader_NetworkSecuritySettings();
        }
        public static string ToHtmlHeader_WiFiAvailableNetwork()
        {
            return "WiFiSsid".td() + "Bssid".td() + "BeaconInterval".td() + "Frequency".td() + "IsWiFiDirect".td() + "NetworkKind".td() + "Rssi".td() + "PhyKind".td() + "SignalBars".td() + "Uptime".td()
                + ToHtmlHeader_NetworkSecuritySettings();
        }

        public static string ToCsvData(WiFiAvailableNetwork value)
        {
            if (value == null) return $",,,,,,,,,,,,";
            var ghz = (double)value.ChannelCenterFrequencyInKilohertz / 1000000.0;
            var retval = $"\"{value.Ssid}\",{value.Bssid},{value.BeaconInterval.TotalSeconds},{ghz},{value.IsWiFiDirect},{value.NetworkKind},";
            retval += $"{value.NetworkRssiInDecibelMilliwatts},{value.PhyKind},{value.SignalBars},{value.Uptime},";
            retval += ToCsvData(value.SecuritySettings);
            return retval;
        }
        public static string ToHtmlData(WiFiAvailableNetwork value)
        {
            if (value == null) return "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td();
            var ghz = (double)value.ChannelCenterFrequencyInKilohertz / 1000000.0;
            var retval = $"{value.Ssid.ToString().td()}{value.Bssid.ToString().td()}{value.BeaconInterval.TotalSeconds.ToString().td()}{ghz.ToString().td()}{value.IsWiFiDirect.ToString().td()}{value.NetworkKind.ToString().td()}";
            retval += $"{value.NetworkRssiInDecibelMilliwatts.ToString().td()}{value.PhyKind.ToString().td()}{value.SignalBars.ToString().td()}{value.Uptime.ToString().td()}";
            retval += ToHtmlData(value.SecuritySettings);
            return retval;
        }
        public static string ToString(string indent, WiFiNetworkInformation value)
        {
            var retval = "";
            retval += $"{indent}Name={value.SSID.OrUnnamed()}\n"; // The OrUnnamed isn't actually needed since it's filled in correctly
            indent += Tab;
            retval += $"{indent}BSSID={value.Bssid}\n";
            retval += $"{indent}Frequency={value.Frequency} GHz\n";
            retval += $"{indent}RSSI={value.Rssi}\n";
            retval += $"{indent}Strength={value.SignalBars}\n";
            retval += $"{indent}Uptime={value.Uptime.ToString("dd\\ \\d\\a\\y\\s\\ hh\\:mm")}\n";
            retval += $"{indent}Kind={value.PhyKind}\n";
            retval += $"{indent}Encryption={value.EncryptionType}\n";
            retval += $"{indent}Authentication={value.AuthenticationType}\n";
            retval += $"{indent}BeaconInterval={value.BeaconInterval} seconds\n";
            retval += $"{indent}WifiDirect={value.IsWiFiDirect}\n";
            retval += $"{indent}NetworkKind={value.NetworkKind}\n";
            retval += "\n\n\n";
            return retval;
        }


        public static string Decode(WiFiNetworkKind value)
        {
            switch (value)
            {
                case WiFiNetworkKind.Infrastructure: return "Wi-Fi"; // this is the normal case.
                case WiFiNetworkKind.Adhoc: return "Ad-hoc";
                case WiFiNetworkKind.Any: return "Wi-Fi or Ad-hoc";
            }
            return value.ToString(); // catches anything we miss
        }

        public static string ToString(string indent, WiFiNetworkReport value)
        {
            if (value == null) return $"{indent}WiFiNetworkReport does not exist\n";
            var retval = $"{indent}WiFiNetworkReport Count={value.AvailableNetworks.Count} TimeStamp={value.Timestamp}\n";
            indent += Tab;
            foreach (var item in value.AvailableNetworks)
            {
                retval += ToString(indent, item);
            }
            return retval;
        }

        public static string ToCsvHeader_WiFiNetworkReport()
        {
            return ToCsvHeader_WiFiAvailableNetwork();
        }
        public static string ToHtmlHeader_WiFiNetworkReport()
        {
            return ToHtmlHeader_WiFiAvailableNetwork();
        }
        public static string ToCsvData(WiFiNetworkReport value)
        {
            if (value == null) return $"NO_DATA_FOR_WIFI_NETWORK_REPORT\n";
            var retval = "";
            foreach (var item in value.AvailableNetworks)
            {
                retval += ToCsvData(item) + "\n";
            }
            return retval;
        }
        public static string ToHtmlData(WiFiNetworkReport value)
        {
            if (value == null) return $"NO_DATA_FOR_WIFI_NETWORK_REPORT".td();
            var retval = "";
            foreach (var item in value.AvailableNetworks)
            {
                retval += ToHtmlData(item).tr();
            }
            return retval;
        }

        public static void Fill(WiFiAdapter wifiAdapter, IList<WiFiNetworkInformation> list, WiFiNetworkReport value, ScanMetadata smd)
        {
            foreach (var item in value.AvailableNetworks)
            {
                var data = new WiFiNetworkInformation();
                Fill(data, item, smd);
                data.SetAdapter(wifiAdapter);
                list.Add(data);
            }
        }

        public static string Decode(WiFiPhyKind value)
        {
            switch (value)
            {
                case WiFiPhyKind.Fhss: return "Wi-Fi (802.11b) Frequency-hopping + spread spectrum (FHSS)";
                case WiFiPhyKind.Dsss: return "Wi-Fi (802.11b) Direct sequence + spread spectrum (DSSS)";
                case WiFiPhyKind.IRBaseband: return "Infrared";
                case WiFiPhyKind.Ofdm: return "Wi-Fi 2 (802.11a) Orthogonal frequency division multiplex (OFDM)";
                case WiFiPhyKind.Hrdsss: return "Wi-Fi (802.11b) High-rated DSSS";
                case WiFiPhyKind.Erp: return "WiFi 3 (802.11g) Extended Rate (ERP)";
                case WiFiPhyKind.HT: return "Wi-Fi 4 (802.11n) High Throughput (HT)";
                case WiFiPhyKind.Vht: return "Wi-Fi 5 (802.11ac) Very High Throughput (VHT)";
                case WiFiPhyKind.Dmg: return "WiGig (802.11ad) Directional multi-gigabit (DMG)";
                case WiFiPhyKind.HE: return "Wi-Fi 6 (802.11ax) High-Efficiency Wireless (HEW)";

            }
            return value.ToString();
        }


        public static string ToString(string indent, WlanConnectionProfileDetails value)
        {
            if (value == null) return "";
            var retval = $"{indent}WlanConnectionProfile SSID={value.GetConnectedSsid()}\n";
            return retval;
        }
    }
}

using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.ConversationalAgent;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace testWifiAbilities
{
    static class NetworkToString
    {
        /// <summary>
        /// Amount to indent each time indent is indented
        /// </summary>
        static string Tab = "    ";

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

        public static string ToCsvHeader_AttributedNetworkUsage()
        {
            return "AttributedNetworkUsageName,BytesReceived,BytesSent,AttributionId,";
        }
        public static string CsvData(AttributedNetworkUsage value)
        {
            var retval = $"{value.AttributionName},{value.BytesReceived},{value.BytesSent},{value.AttributionId},";
            return retval;
        }

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
            retval += $"{indent}DomainConnectivityLevel={value.GetDomainConnectivityLevel()}\n";
            retval += $"{indent}DomainConnectivityLevel={value.GetDomainConnectivityLevel()}\n";

            retval += $"{indent}NetworkConnectivityLevel={value.GetNetworkConnectivityLevel()}\n";
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
            retval += $"{indent}IanaInterfaceType={value.IanaInterfaceType}\n";
            retval += $"{indent}InboundMaxBitsPerSecond={value.InboundMaxBitsPerSecond}\n";
            retval += $"{indent}OutboundMaxBitsPerSecond={value.OutboundMaxBitsPerSecond}\n";

            retval += ToString(indent, profile);
            retval += ToString(indent, value.NetworkItem);
            return retval;
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
        public static void Fill(WifiNetworkInformation data, NetworkSecuritySettings value)
        {
            data.AuthenticationType = value.NetworkAuthenticationType.ToString();
            data.EncryptionType = value.NetworkEncryptionType.ToString();
        }
        public static string ToCsvHeader_NetworkSecuritySettings()
        {
            return "AuthenticationType,EncryptionType,";
        }

        public static string ToCsvData(NetworkSecuritySettings value)
        {
            if (value == null) return $",,";
            return $"{value.NetworkAuthenticationType},{value.NetworkEncryptionType},";
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
            var retval = $"{indent}WiFiAvailableNetwork Name={value.Ssid} Bssid={value.Bssid}\n";
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
        public static void Fill(WifiNetworkInformation data, WiFiAvailableNetwork value)
        {
            data.SSID = value.Ssid;
            data.Bssid = value.Bssid;
            data.BeaconInterval = value.BeaconInterval.TotalSeconds;
            data.Frequency = (double)value.ChannelCenterFrequencyInKilohertz / 1000000.0;
            data.IsWiFiDirect = value.IsWiFiDirect ? "true" : "false";
            data.NetworkKind = Decode(value.NetworkKind);
            data.Rssi = value.NetworkRssiInDecibelMilliwatts;
            data.PhyKind = Decode(value.PhyKind);
            data.SignalBars = value.SignalBars;
            data.Uptime = value.Uptime;

            Fill(data, value.SecuritySettings);
        }
        public static string ToCsvHeader_WiFiAvailableNetwork()
        {
            return "WiFiSsid,Bssid,BeaconInterval,Frequency,IsWiFiDirect,NetworkKind,Rssi,PhyKind,SignalBars,Uptime,"
                + ToCsvHeader_NetworkSecuritySettings();
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

        public static string ToString(string indent, WifiNetworkInformation value)
        {
            var retval = "";
            retval += $"{indent}Name={value.SSID}\n";
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

        public static void Fill(IList<WifiNetworkInformation> list, WiFiNetworkReport value)
        {
            foreach (var item in value.AvailableNetworks)
            {
                var data = new WifiNetworkInformation();
                Fill(data, item);
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

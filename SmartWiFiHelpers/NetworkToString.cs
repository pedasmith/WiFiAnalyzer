using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace SmartWiFiHelpers
{
    /// <summary>
    /// Lots of static methods that convert network classes into user-readable strings.
    /// </summary>
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
            // See http://sunriseprogrammer.blogspot.com/2022/09/clipboard-data-for-excel.html for why!

            var escaped = System.Net.WebUtility.HtmlEncode(text);
            return $"<td>{escaped}</td>";
        }
        public static string th(this string text)
        {
            var escaped = System.Net.WebUtility.HtmlEncode(text);
            return $"<th>{escaped}</th>";
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
            //NOTE: Not including Thumbnail

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
        public static string ToString(string indent, ConnectionProfile value) //NOTE: there are more items to get
        {
            if (value == null) return $"{indent}ConnectionProfile does not exist\n";
            var retval = $"{indent}ConnectionProfile {value.ProfileName}\n";
            indent += Tab;
            // NOTE: Get*() NetworkAdapter NetworkSecuritySettings
            retval += $"{indent}CanDelete={value.CanDelete}\n";
            retval += $"{indent}IsWlanConnectionProfile={value.IsWlanConnectionProfile}\n";
            retval += $"{indent}IsWwanConnectionProfile={value.IsWwanConnectionProfile}\n";
            retval += $"{indent}ServiceProviderGuid={value.ServiceProviderGuid}\n";

            // NOTE: missing a bunch of Get*()
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

        public static string ToString(string indent, LanIdentifier value)
        {
            if (value == null) return $"{indent}LanIdentifer does not exist\n";
            var infra = ToString("", value.InfrastructureId);
            var port = ToString("", value.PortId);
            var retval = $"{indent}LAN Identifer InfrastructureId=({infra}) Port=({port})";
            return retval;
        }
        public static string ToString(string indent, LanIdentifierData value)
        {
            if (value == null) return $"{indent}not set";
            string bytestr = "";
            foreach (var b in value.Value)
            {
                bytestr += $"{b:X2} ";
            }
            var retval = $"{indent}LldpType={value.Type} Value={bytestr}";
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
                default: return $"{value}(={(int)value})";
                case NetworkAuthenticationType.None: return "None";
                case NetworkAuthenticationType.Unknown: return "Unknown";
                case NetworkAuthenticationType.Open80211: return "Open 802.11 (Open80211)";
                case NetworkAuthenticationType.SharedKey80211: return "WEP Password (SharedKey80211)";
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
            // No need for this since I have a default value in the switch: return value.ToString();
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


        public static string ToString(string indent, NetworkOperatorTetheringAccessPointConfiguration value)
        {
            if (value == null) return $"{indent}NetworkOperatorTetheringAccessPointConfiguration does not exist\n";
            var retval = $"{indent}NetworkOperatorTetheringAccessPointConfiguration\n";
            indent += Tab;
            retval += $"{indent}Band={value.Band}\n";
            retval += $"{indent}Passphrase={value.Passphrase}\n";
            retval += $"{indent}Operational Ssid={value.Ssid}\n";
            var bandstr = "";
            var bandlist = new List<TetheringWiFiBand>() { TetheringWiFiBand.TwoPointFourGigahertz, TetheringWiFiBand.FiveGigahertz };
            foreach (var band in bandlist)
            {
                if (value.IsBandSupported(band)) bandstr += ToString(band) + " ";
            }
            retval += $"{indent}Supported Bands={bandstr}\n";
            return retval;
        }
        public static string ToString(string indent, NetworkOperatorTetheringClient value)
        {
            if (value == null) return $"{indent}NetworkOperatorTetheringClient does not exist\n";
            var retval = $"{indent}NetworkOperatorTetheringClient\n";
            indent += Tab;
            retval += $"{indent}MAC={value.MacAddress}\n";
            var hoststr = "";
            foreach (var host in value.HostNames)
            {
                hoststr += host.DisplayName + " ";
            }
            retval += $"{indent}Hosts={hoststr}\n";
            return retval;
        }

        public static string ToString (string indent, NetworkOperatorTetheringManager value)
        {
            if (value == null) return $"{indent}NetworkOperatorTetheringManager does not exist\n";
            var retval = $"{indent}NetworkOperatorTetheringManager\n";
            indent += Tab;
            retval += $"{indent}ClientCount={value.ClientCount}\n";
            retval += $"{indent}MaxClientCount={value.MaxClientCount}\n";
            retval += $"{indent}Operational State={value.TetheringOperationalState}\n";
            var profile = NetworkInformation.GetInternetConnectionProfile();
            var tcap = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(profile);
            retval += $"{indent}Tethering Capability={tcap}\n"; // using internet profile

            var apconfiguration = value.GetCurrentAccessPointConfiguration();
            retval += ToString(indent, apconfiguration);
            var clients = value.GetTetheringClients();
            foreach (var client in clients)
            {
                retval += ToString(indent, client);
            }
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
            return "AuthenticationType".th()+"EncryptionType".th();
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

        /// <summary>
        /// Converts value like "TwoPointFourGigahertz" to user-friendly "2.4 GHz"
        /// </summary>
        public static string ToString(TetheringWiFiBand value)
        {
            switch (value)
            {
                case TetheringWiFiBand.TwoPointFourGigahertz: return "2.4 GHz";
                case TetheringWiFiBand.FiveGigahertz: return "5 GHz";
                case TetheringWiFiBand.Auto: return "Any available";
            }
            return value.ToString(); // handles any other case that might come up.
        }


        public static async Task<string> ToStringAsync(string indent, WiFiAdapter value)
        {
            if (value == null) return $"{indent}WiFiAdapter does not exist\n";
            var retval = $"{indent}WiFiAdapter\n";
            var adap = await ToStringAsync(indent + Tab, value.NetworkAdapter);
            retval += adap;
            return retval;
        }

        public static string ToString(string indent, WiFiAvailableNetwork value, string probableBssid)
        {
            if (value == null) return $"{indent}WiFiAvailableNetwork does not exist\n";
            var isCurrentAP = "";
            if (value.Bssid == probableBssid)
            {
                isCurrentAP = "**";
            }
            var retval = $"{indent}{isCurrentAP}WiFiAvailableNetwork Name={value.Ssid.OrUnnamed()} Bssid={value.Bssid}\n";
            indent += Tab;
            retval += $"{indent}BeaconInterval={value.BeaconInterval.TotalSeconds}\n";
            retval += $"{indent}ChannelCenterFrequencyInGigahertz={(double)value.ChannelCenterFrequencyInKilohertz/1000000.0}\n";
            retval += $"{indent}IsWiFiDirect={value.IsWiFiDirect}\n";
            retval += $"{indent}NetworkKind={value.NetworkKind}\n";
            retval += $"{indent}NetworkRssiInDecibelMilliwatts={value.NetworkRssiInDecibelMilliwatts}\n";
            retval += $"{indent}PhyKind={Decode(value.PhyKind)}\n";
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
                dest.Notes = wbc.Notes;
            }
            else
            {
                ;
            }
            dest.SetAvailableNetwork(source);

        }
        private static string[] ColNames = new string[]
        {
            "SSID", "BSSID", "BandName", "ChannelName", "Bandwidth", "Frequency", 
            "RSSI", "SignalBars", "PhyKind", "Uptime", "BeaconInterval", 
            "AuthentictionType", "EncryptionType",  // From NetworkSecurity
            "IsWiFiDirect", "NetworkKind"
        };
        public static string ToCsvHeader_WiFiAvailableNetwork()
        {
            var retval = "";
            foreach (var name in ColNames)
            {
                if (retval != "") retval += ",";
                retval += name;
            }
            return retval;
            //return "WiFiSsid,Bssid,BeaconInterval,Frequency,IsWiFiDirect,NetworkKind,Rssi,PhyKind,SignalBars,Uptime,"
            //    + ToCsvHeader_NetworkSecuritySettings();
        }
        public static string ToHtmlHeader_WiFiAvailableNetwork()
        {
            var retval = "";
            foreach (var name in ColNames)
            {
                retval += name.th();
            }
            return retval;
            //return "WiFiSsid".th() + "Bssid".th() + "BeaconInterval".th() + "Frequency".th() + "IsWiFiDirect".th() + "NetworkKind".th() + "Rssi".th() + "PhyKind".th() + "SignalBars".th() + "Uptime".th()
            //    + ToHtmlHeader_NetworkSecuritySettings();
        }

        public static string ToCsvData(WiFiAvailableNetwork value)
        {
            if (value == null) return $",,,,,,,,,,,,";
            var ghz = (double)value.ChannelCenterFrequencyInKilohertz / 1000000.0;

            WiFiNetworkInformation wifini = new WiFiNetworkInformation();
            ScanMetadata smd = new ScanMetadata();
            Fill(wifini, value, smd);

            var retval = $"\"{value.Ssid}\",{value.Bssid},{wifini.BandName},{wifini.ChannelName},{wifini.Bandwidth},{ghz},";
            retval += $"{value.NetworkRssiInDecibelMilliwatts},{value.SignalBars},{Decode(value.PhyKind)},{value.Uptime},{value.BeaconInterval.TotalSeconds},";
            retval += ToCsvData(value.SecuritySettings);
            retval += $"{value.IsWiFiDirect},{value.NetworkKind}";

            //var retval = $"\"{value.Ssid}\",{value.Bssid},{value.BeaconInterval.TotalSeconds},{ghz},{value.IsWiFiDirect},{value.NetworkKind},";
            //retval += $"{value.NetworkRssiInDecibelMilliwatts},{Decode(value.PhyKind)},{value.SignalBars},{value.Uptime},";
            //retval += ToCsvData(value.SecuritySettings);
            return retval;
        }
        public static string ToHtmlData(WiFiAvailableNetwork value)
        {
            if (value == null) return "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td() + "".td();
            var ghz = (double)value.ChannelCenterFrequencyInKilohertz / 1000000.0;

            WiFiNetworkInformation wifini = new WiFiNetworkInformation();
            ScanMetadata smd = new ScanMetadata();
            Fill(wifini, value, smd);

            var retval = $"{value.Ssid.ToString().td()}{value.Bssid.ToString().td()}{wifini.BandName.td()}{wifini.ChannelName.td()}{wifini.Bandwidth.ToString().td()}{ghz.ToString().td()}";
            retval += $"{value.NetworkRssiInDecibelMilliwatts.ToString().td()}{value.SignalBars.ToString().td()}{Decode(value.PhyKind).td()}{value.Uptime.ToString().td()}{value.BeaconInterval.TotalSeconds.ToString().td()}";
            retval += ToHtmlData(value.SecuritySettings);
            retval += $"{value.IsWiFiDirect.ToString().td()}{value.NetworkKind.ToString().td()}";

            //var retval = $"{value.Ssid.ToString().td()}{value.Bssid.ToString().td()}{value.BeaconInterval.TotalSeconds.ToString().td()}{ghz.ToString().td()}{value.IsWiFiDirect.ToString().td()}{value.NetworkKind.ToString().td()}";
            //retval += $"{value.NetworkRssiInDecibelMilliwatts.ToString().td()}{Decode(value.PhyKind).td()}{value.SignalBars.ToString().td()}{value.Uptime.ToString().td()}";
            //retval += ToHtmlData(value.SecuritySettings);
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

        public static string ToString(string indent, WiFiNetworkReport value, string probableBssid)
        {
            if (value == null) return $"{indent}WiFiNetworkReport does not exist\n";
            var retval = $"{indent}WiFiNetworkReport Count={value.AvailableNetworks.Count} TimeStamp={value.Timestamp}\n";
            indent += Tab;
            foreach (var item in value.AvailableNetworks)
            {
                retval += ToString(indent, item, probableBssid);
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
        public static string ToCsvData(WiFiNetworkReport value, int rssiMinimum)
        {
            if (value == null) return $"NO_DATA_FOR_WIFI_NETWORK_REPORT\n";
            var retval = "";
            foreach (var item in value.AvailableNetworks)
            {
                if (item.NetworkRssiInDecibelMilliwatts >= rssiMinimum)
                {
                    retval += ToCsvData(item) + "\n";
                }
            }
            return retval;
        }
        public static string ToHtmlData(WiFiNetworkReport value, int rssiMinimum)
        {
            if (value == null) return $"NO_DATA_FOR_WIFI_NETWORK_REPORT".td();
            var retval = "";
            foreach (var item in value.AvailableNetworks)
            {
                if (item.NetworkRssiInDecibelMilliwatts >= rssiMinimum)
                {
                    retval += ToHtmlData(item).tr();
                }
            }
            return retval;
        }

        public static void Fill(WiFiAdapter wifiAdapterSource, IList<WiFiNetworkInformation> listDest, WiFiNetworkReport value, ScanMetadata smd, int rssiMinimum=-1000)
        {
            foreach (var item in value.AvailableNetworks)
            {
                if (item.NetworkRssiInDecibelMilliwatts > rssiMinimum)
                {
                    var data = new WiFiNetworkInformation();
                    Fill(data, item, smd);
                    data.SetAdapter(wifiAdapterSource);
                    listDest.Add(data);
                }
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
#if SUPPORT_LATEST_2023
                case WiFiPhyKind.Eht: return "Wi-Fi 7 (802.11be) Enhanced throughput (EHT)";
#else
                case (WiFiPhyKind)11: return "Wi-Fi 7 (802.11be) Enhanced throughput (EHT)";
#endif

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

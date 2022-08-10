using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testWifiAbilities
{
    public class WifiNetworkInformation
    {
        public string SSID { get; set; }
        public string Bssid { get; set; }
        public double BeaconInterval { get; set; }
        public double Frequency { get; set; }
        public string IsWiFiDirect { get; set; }
        public string NetworkKind { get; set; }
        public double Rssi { get; set; }
        public string PhyKind { get; set; }
        public double SignalBars { get; set; }
        public TimeSpan Uptime { get; set; }
        public string AuthenticationType { get; set; }
        public string EncryptionType { get; set; }
    }
}

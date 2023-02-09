using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace TestGetLanIdentifiers
{
    internal class Program
    {
        private static string CurrentProbableBssid = "";
        private static void Log (string str)
        {
            Console.WriteLine (str);
            System.Diagnostics.Debug.WriteLine (str);
        }
        private class NetworkToString
        {
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
        }

        private static void UpdateNetworkInfo()
        {
            try
            {
                var lanList = NetworkInformation.GetLanIdentifiers();
                Log($"Network Information: LAN Identifiers: ");
                CurrentProbableBssid = "";
                foreach (var lanItem in lanList)
                {
                    Log(NetworkToString.ToString("    ", lanItem));
                    if (lanItem.InfrastructureId?.Type == 4097)
                    {
                        // I've got a probably BSSID for the WLAN (Wi-Fi) 
                        // that I'm connected to!
                        var b = lanItem.InfrastructureId.Value.ToArray();
                        if (b.Length == 6)
                        {
                            CurrentProbableBssid = "";
                            for (int i = 0; i < b.Length; i++)
                            {
                                if (i > 0) CurrentProbableBssid += ":";
                                CurrentProbableBssid += $"{b[i]:X2}".ToLower();
                            }
                            ;
                        }
                    }
                }
                if (CurrentProbableBssid != "")
                {
                    Log($"    Current Probable BSSID={CurrentProbableBssid}");
                }
                Log("");
            }
            catch (Exception e)
            {
                Log($"Error while getting network LAN info {e.Message}");
            }
        }

        static void Main(string[] args)
        {
            UpdateNetworkInfo();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestLanBSSID
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            UILogSingleton = uiLog;
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Scan();
        }

        private static string CurrentProbableBssid = "";
        static TextBlock UILogSingleton = null;
        private static void Log(string str)
        {
            Console.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);
            if (UILogSingleton != null) UILogSingleton.Text += str + "\n";
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
                    Log($"    Current Probable BSSID={CurrentProbableBssid} (from GetLanIdentifiers)");
                }
                Log("");
            }
            catch (Exception e)
            {
                Log($"Error while getting network LAN info {e.Message}");
            }
        }

        static async Task ListWiFiScan()
        {
            int nmatch = 0;
            int nskip = 0;

            var adapterList = await WiFiAdapter.FindAllAdaptersAsync();
            if (adapterList.Count < 1)
            {
                Log($"Unable to scan wifi: no Wi-Fi adapters found");
                return;
            }
            foreach (var wifiAdapter in adapterList)
            {
                try
                {
                    await wifiAdapter.ScanAsync();
                }
                catch (Exception e)
                {
                    Log($"Scan error: {e.Message}");
                    return;
                }
                try
                {
                    foreach (var network in wifiAdapter.NetworkReport.AvailableNetworks)
                    {
                        if (network.NetworkRssiInDecibelMilliwatts <= -50) { nskip++; continue; } // Don't slam logging

                        var note = "";
                        if (network.Bssid == CurrentProbableBssid)
                        {
                            nmatch++;
                            note = " ** ";
                        }
                        Log($"{note}SSID={network.Ssid} BSSID={network.Bssid}{note}  (from WiFiAdapter.NetworkReport.AvailableNetworks)");
                    }
                }
                catch (Exception e)
                {
                    Log($"Scan networks error: {e.Message}");
                }
                Log($"Results: nmatch={nmatch} (should be 1) nskip={nskip}");

            }
        }

        private async Task Scan()
        {
            UpdateNetworkInfo();
            await ListWiFiScan();
        }
        private async void OnRescan(object sender, RoutedEventArgs e)
        {
            await Scan();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace testWifiAbilities
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await DoScanAsync();
        }

        private void Log(string text)
        {
            uiReport.Text += text + "\n";
        }

        private async void OnScanNow(object sender, RoutedEventArgs e)
        {
            await DoScanAsync();
        }

        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";

            var list = await WiFiAdapter.FindAllAdaptersAsync();
            var csv = NetworkToString.ToCsvHeaderWiFiNetworkReport() + "\n";
            foreach (var item in list)
            {
                Log(await NetworkToString.ToStringAsync("    ", item));
                await item.ScanAsync();
                var str = NetworkToString.ToString("    ", item.NetworkReport);
                csv += NetworkToString.ToCsvData(item.NetworkReport);
                Log(str);
                Log("");

            }

            Log ($"\nScan ended at {DateTime.Now}");
            Log("\n\n");
            Log(csv);
        }
    }
}

using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            uiGrid.ItemsSource = networkInfo;
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
        ObservableCollection<WifiNetworkInformation> networkInfo = new ObservableCollection<WifiNetworkInformation>();
        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";

            var list = await WiFiAdapter.FindAllAdaptersAsync();
            var csv = NetworkToString.ToCsvHeaderWiFiNetworkReport() + "\n";
            var dg = uiGrid;
            networkInfo.Clear(); 
            foreach (var item in list)
            {
                Log (await NetworkToString.ToStringAsync("", item));
                await item.ScanAsync();
                Log(NetworkToString.ToString("    ", item.NetworkReport));
                csv += NetworkToString.ToCsvData(item.NetworkReport);
                NetworkToString.Fill(networkInfo, item.NetworkReport);
            }

            Log ($"\nScan ended at {DateTime.Now}");
            Log("\n\n");
            uiCsv.Text = csv;
        }

        private void OnGridSort(object sender, DataGridColumnEventArgs e)
        {
            var dg = uiGrid;
            var list = networkInfo;
            var name = e.Column.Header.ToString();
            var prop = list[0].GetType().GetProperty(name);

            var direction = (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending) ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
            var sorted = direction == DataGridSortDirection.Ascending 
                ? networkInfo.OrderBy(item => prop.GetValue(item, null)).ToList() 
                : networkInfo.OrderByDescending(item => prop.GetValue(item, null)).ToList();


            for (int i=0; i< sorted.Count(); i++)
            {
                var oldIndex = networkInfo.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    networkInfo.Move(oldIndex, i);
                }
            }


            foreach (var col in dg.Columns)
            {
                if (col == e.Column) col.SortDirection = direction;
                else col.SortDirection = null;
            }
        }
    }
}

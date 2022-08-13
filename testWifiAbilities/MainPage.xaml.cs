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
            uiGrid.ItemsSource = NetworkInformationList;
            // TODO: no automatic scan on startup while I
            // get the radar display working.
            // await DoScanAsync();
        }

        private void Log(string text)
        {
            uiReport.Text += text + "\n";
        }

        private async void OnScanNow(object sender, RoutedEventArgs e)
        {
            await DoScanAsync();
        }
        ObservableCollection<WifiNetworkInformation> NetworkInformationList = new ObservableCollection<WifiNetworkInformation>();
        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";

            var list = await WiFiAdapter.FindAllAdaptersAsync();
            var csv = NetworkToString.ToCsvHeaderWiFiNetworkReport() + "\n";
            var dg = uiGrid;
            NetworkInformationList.Clear(); 
            foreach (var item in list)
            {
                Log (await NetworkToString.ToStringAsync("", item));
                item.AvailableNetworksChanged += Item_AvailableNetworksChanged;
                await item.ScanAsync();
                Log(NetworkToString.ToString("    ", item.NetworkReport));
                csv += NetworkToString.ToCsvData(item.NetworkReport);
                NetworkToString.Fill(NetworkInformationList, item.NetworkReport);
            }

            Log ($"\nScan ended at {DateTime.Now}");
            Log("\n\n");
            uiCsv.Text = csv;
        }

        private void Item_AvailableNetworksChanged(WiFiAdapter sender, object args)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkChange: {args}"); //TODO: remove
        }


        private void OnGridSort(object sender, DataGridColumnEventArgs e)
        {
            var dg = uiGrid;
            var list = NetworkInformationList;
            var name = e.Column.Header.ToString();
            var prop = list[0].GetType().GetProperty(name);

            var direction = (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending) ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
            var sorted = direction == DataGridSortDirection.Ascending 
                ? NetworkInformationList.OrderBy(item => prop.GetValue(item, null)).ToList() 
                : NetworkInformationList.OrderByDescending(item => prop.GetValue(item, null)).ToList();


            for (int i=0; i< sorted.Count(); i++)
            {
                var oldIndex = NetworkInformationList.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    NetworkInformationList.Move(oldIndex, i);
                }
            }


            foreach (var col in dg.Columns)
            {
                if (col == e.Column) col.SortDirection = direction;
                else col.SortDirection = null;
            }
        }

        private void OnGridTapped(object sender, TappedRoutedEventArgs e)
        {
            ;
        }
        private bool AmFiltering = false;
        List<WifiNetworkInformation> SavedInformation = null;

        private void OnGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var list = NetworkInformationList;
            var dg = uiGrid;
            var colName = dg.CurrentColumn.Header.ToString();
            var row = dg.SelectedItem as WifiNetworkInformation;
            var prop = row.GetType().GetProperty(colName);
            var filterTo = prop.GetValue(row);

            if (AmFiltering)
            {
                // Stop filtering.
                list.Clear();
                foreach (var item in SavedInformation)
                {
                    list.Add(item);
                }
                AmFiltering = false;
            }
            else
            {
                SavedInformation = new List<WifiNetworkInformation>();
                foreach (var item in list)
                {
                    SavedInformation.Add(item);
                }
                // And filter
                for (int rowIndex=list.Count-1; rowIndex>=0; rowIndex--)
                {
                    var potentialItem = prop.GetValue(list[rowIndex]);
                    if ((dynamic)potentialItem != (dynamic)filterTo)
                    {
                        list.RemoveAt(rowIndex);
                    }
                }
                if (list.Count == 0)
                {
                    list.Add(SavedInformation[0]); // always at least one.
                }
                AmFiltering = true;
            }
        }

        private void OnGridCurrentCellChanged(object sender, EventArgs e)
        {
            ;
        }
    }
}

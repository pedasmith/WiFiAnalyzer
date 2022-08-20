using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace testWifiAbilities
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IDisplayWifiNetworkInformation
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }
        //Location will have to wait for version 2
        //GeolocationAccessStatus GeoAccessStatus;
        //Geolocator Locator;
        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            //GeoAccessStatus = await Geolocator.RequestAccessAsync();
            //Locator = new Geolocator() { DesiredAccuracyInMeters = 4,  };
            //Locator.AllowFallbackToConsentlessPositions();

            uiGrid.ItemsSource = CurrentNetworkInformationList;
            uiRadar.DisplayWifiNetworkInformation = this;
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

        public void ScanFailed(string text)
        {
            Log(text); //TODO: do something bigger with this?
        }
        ObservableCollection<WifiNetworkInformation> CurrentNetworkInformationList = new ObservableCollection<WifiNetworkInformation>();
        String CurrentCsv = "";
        List<Reflector> CurrentReflectorList = new List<Reflector>();

        public class ScanMetadata
        {
            public DateTimeOffset ScanTime { get; set; } = DateTimeOffset.Now;
            public Geoposition Position { get; set; }
        }
        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";
            //var locatorTask = Locator.GetGeopositionAsync(new TimeSpan(0, 1, 0), new TimeSpan(0, 0, 5)); // allow 1-minue old data; timeout within 5 seconds

            uiRadar.Initialize();
            uiRadar.AddDummyReflectors();

            var dg = uiGrid;
            var smd = new ScanMetadata();


            var list = await WiFiAdapter.FindAllAdaptersAsync();
            CurrentCsv = NetworkToString.ToCsvHeader_WiFiNetworkReport() + "\n";
            CurrentNetworkInformationList.Clear(); 
            foreach (var item in list)
            {
                Log (await NetworkToString.ToStringAsync("", item));
                item.AvailableNetworksChanged += Item_AvailableNetworksChanged;
                try
                {
                    await item.ScanAsync();
                    //var scanTask =  item.ScanAsync();
                    //await Task.WhenAll(new Task[] { locatorTask.AsTask(), scanTask.AsTask() });
                }
                catch (Exception e)
                {
                    ScanFailed(e.Message); // TODO: do a message somehow?
                }
                Log(NetworkToString.ToString("    ", item.NetworkReport));
                //if (locatorTask.Status != AsyncStatus.Error) smd.Position = locatorTask.GetResults();
                //Log($"DBG: location status={locatorTask.Status} position={smd.Position}");

                CurrentCsv += NetworkToString.ToCsvData(item.NetworkReport);
                NetworkToString.Fill(CurrentNetworkInformationList, item.NetworkReport, smd);
            }
            //DoGridSort(uiGrid, NetworkInformationList, "SSID");
            Log ($"\nScan ended at {DateTime.Now}");
            Log("\n\n");
            uiCsv.Text = CurrentCsv;


            // Add the locations
            SetupCurrentReflectorList();
            uiRadar.SetReflectors(CurrentReflectorList); // Update the reflectors to represent the new truth about WiFi
            await uiRadar.StopAsync();
        }

        private void SetupCurrentReflectorList()
        {
            CurrentReflectorList = new List<Reflector>();
            foreach (var ninfo in CurrentNetworkInformationList)
            {
                var refl = new Reflector();
                refl.Icon = Reflector.Icon_AP;
                refl.NetworkInformation = ninfo;
                CurrentReflectorList.Add(refl);
            }
            CurrentReflectorList = CurrentReflectorList.OrderBy(value => value.NetworkInformation.Rssi).ToList();
        }

        private void Item_AvailableNetworksChanged(WiFiAdapter sender, object args)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkChange: {args}"); //TODO: remove
        }


        private void OnGridSort(object sender, DataGridColumnEventArgs e)
        {
            var dg = uiGrid;
            var list = CurrentNetworkInformationList;
            var name = e.Column.Header.ToString();
            DoGridSort(dg, list, name, e);
        }
        private void DoGridSort(DataGrid dg, IList<WifiNetworkInformation> list, string name, DataGridColumnEventArgs e)
        {
            var prop = list[0].GetType().GetProperty(name);

            var direction = (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending) 
                ? DataGridSortDirection.Ascending 
                : DataGridSortDirection.Descending;
            var sorted = direction == DataGridSortDirection.Ascending
                ? CurrentNetworkInformationList.OrderBy(item => prop.GetValue(item, null)).ToList()
                : CurrentNetworkInformationList.OrderByDescending(item => prop.GetValue(item, null)).ToList();


            for (int i = 0; i < sorted.Count(); i++)
            {
                var oldIndex = CurrentNetworkInformationList.IndexOf(sorted[i]);
                if (oldIndex != i)
                {
                    CurrentNetworkInformationList.Move(oldIndex, i);
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
            var list = CurrentNetworkInformationList;
            var dg = uiGrid;
            if (dg.CurrentColumn == null) return;

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

        public void Display(WifiNetworkInformation value)
        {
            var text = NetworkToString.ToString("", value);
            uiRadarDetailsText.Text = text + uiRadarDetailsText.Text;
            uiRadarDetails.Visibility = Visibility.Visible;
        }

        private void OnHideRadarDetails(object sender, TappedRoutedEventArgs e)
        {
            uiRadarDetails.Visibility = Visibility.Collapsed;
        }

        private void OnClearRadarDetails(object sender, TappedRoutedEventArgs e)
        {
            uiRadarDetailsText.Text = "";
        }
    }
}

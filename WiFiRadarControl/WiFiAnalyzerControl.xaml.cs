using Microsoft.Toolkit.Uwp.UI.Controls;
using SimpleWiFiAnalyzer;
using SmartWiFiHelpers;
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
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WiFiRadarControl
{

    public sealed partial class WiFiAnalyzerControl : UserControl, IDisplayWifiNetworkInformation
    {
        public WiFiAnalyzerControl()
        {
            this.InitializeComponent();
            this.Loaded += WiFiAnalyzerControl_Loaded;
        }

        private async void WiFiAnalyzerControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up the help system
            var pagename = "Help.md";
            uiHelpText.UriPrefix = "ms-appx:///Assets/HelpFiles/";
            uiHelpText.LinkClicked += UiHelpText_LinkClicked;

            const string StartPage = "Help.md";
            //pagename = this.DataContext as string;
            if (String.IsNullOrEmpty(pagename))
            {
                pagename = StartPage;
            }
            await HelpGotoAsync(pagename);
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
        HelpPageHistory HelpHistory = new HelpPageHistory();
        public static string HelpNavigatedTo = "";
        private void SetNavigatedTo(string place)
        {
            HelpHistory.NavigatedTo(place);
            HelpNavigatedTo = place;
        }

        #region HELP
        private async Task<bool> HelpGotoAsync(string filename)
        {
            if (filename.StartsWith("http://") || filename.StartsWith("https://"))
            {
                // Pop out to a browser window!
                try
                {
                    Uri uri = new Uri(filename);
                    var launched = await Windows.System.Launcher.LaunchUriAsync(uri);
                }
                catch (Exception)
                {
                    ; // do thing?
                }
                return true;
            }


            try
            {
                StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string fname = @"Assets\HelpFiles\" + filename;
                var f = await InstallationFolder.GetFileAsync(fname);
                var fcontents = File.ReadAllText(f.Path);
                uiHelpText.Text = fcontents;
                SetNavigatedTo(filename);
                return true;
            }
            catch (Exception)
            {
            }
            const string ErrorName = "Error.md";
            if (filename != ErrorName)
            {
                await HelpGotoAsync(ErrorName);
            }
            return false; // If I'm showing the error, return false.
        }
        private async void UiHelpText_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            var ok = await HelpGotoAsync(e.Link);
        }
        private async void OnHelpBack(object sender, RoutedEventArgs e)
        {
            var page = HelpHistory.PopLastPage();
            await HelpGotoAsync(page);
        }

        #endregion

        #region SCAN
        private async void OnScanNow(object sender, RoutedEventArgs e)
        {
            await DoScanAsync();
        }

        public void ScanFailed(string text)
        {
            Log(text); //TODO: do something bigger with this?
        }
        ObservableCollection<WiFiNetworkInformation> CurrentNetworkInformationList = new ObservableCollection<WiFiNetworkInformation>();
        String CurrentCsv = "";
        List<Reflector> CurrentReflectorList = new List<Reflector>();

        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";
            //var locatorTask = Locator.GetGeopositionAsync(new TimeSpan(0, 1, 0), new TimeSpan(0, 0, 5)); // allow 1-minue old data; timeout within 5 seconds

            uiRadar.Initialize();
            uiRadar.AddDummyReflectors();

            var dg = uiGrid;
            var smd = new ScanMetadata();


            var adapterList = await WiFiAdapter.FindAllAdaptersAsync();
            CurrentCsv = NetworkToString.ToCsvHeader_WiFiNetworkReport() + "\n";
            CurrentNetworkInformationList.Clear();
            foreach (var wifiAdapter in adapterList)
            {
                Log(await NetworkToString.ToStringAsync("", wifiAdapter));
                wifiAdapter.AvailableNetworksChanged += Item_AvailableNetworksChanged;
                try
                {
                    await wifiAdapter.ScanAsync();
                    //var scanTask =  item.ScanAsync();
                    //await Task.WhenAll(new Task[] { locatorTask.AsTask(), scanTask.AsTask() });
                }
                catch (Exception e)
                {
                    ScanFailed(e.Message); // TODO: do a message somehow?
                }
                Log(NetworkToString.ToString("    ", wifiAdapter.NetworkReport));
                //if (locatorTask.Status != AsyncStatus.Error) smd.Position = locatorTask.GetResults();
                //Log($"DBG: location status={locatorTask.Status} position={smd.Position}");

                CurrentCsv += NetworkToString.ToCsvData(wifiAdapter.NetworkReport);
                NetworkToString.Fill(wifiAdapter, CurrentNetworkInformationList, wifiAdapter.NetworkReport, smd);
            }
            Log($"\nScan ended at {DateTime.Now}");
            Log("\n\n");
            uiCsv.Text = CurrentCsv;

            // Add logging for the bands
            var orderList = new OrderedBandList(CurrentNetworkInformationList);
            Log("Frequency Chart:");
            foreach (var item in orderList.OrderedBands)
            {
                var str = $"    {item.Key}: ";
                foreach (var band in item.Value.InfoOverlapFrequency)
                {
                    str += $"{band.SSID} ";
                }
                foreach (var band in item.Value.InfoExactFrequency)
                {
                    str += $"**{band.SSID}** ";
                }
                Log(str);
            }
            Log("\n\n");

            // Set up the strength lists
            var obl = new OrderedBandList(CurrentNetworkInformationList);
            uiStrength.Children.Clear();
            WiFiStrengthControl.ResetColorIndex();
            foreach (var (freq,obi) in obl.OrderedBands)
            {
                var ctrl = new WiFiStrengthControl() { DisplayInfo = this, };
                ctrl.SetStrength(obi);
                uiStrength.Children.Add(ctrl);
            }

            // Add the locations
            CurrentReflectorList = CreateReflectorList(CurrentNetworkInformationList);
            uiRadar.SetReflectors(CurrentReflectorList); // Update the reflectors to represent the new truth about WiFi
            await uiRadar.StopAsync();
        }

        private static List<Reflector> CreateReflectorList(IList<WiFiNetworkInformation> list)
        {
            var retval = new List<Reflector>();
            foreach (var ninfo in list)
            {
                var reflector = new Reflector();
                reflector.Icon = Reflector.Icon_AP;
                reflector.NetworkInformation = ninfo;
                retval.Add(reflector);
            }
            retval = retval.OrderBy(value => value.NetworkInformation.Rssi).ToList();
            return retval;
        }

        private void Item_AvailableNetworksChanged(WiFiAdapter sender, object args)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkChange: {sender} args=<<{args}>>"); //TODO: remove
        }
        #endregion SCAN

        #region RADAR
        public void Display(WiFiNetworkInformation value)
        {
            var text = NetworkToString.ToString("", value);
            uiWiFiDetailsText.Text = text + uiWiFiDetailsText.Text;
            uiRadarDetails.Visibility = Visibility.Visible;
            CurrentAdapter = value.GetAdapter();
            CurrentAvailableNetwork = value.GetAvailableNetwork();
            bool canConnect = CurrentAdapter != null && CurrentAvailableNetwork != null;
            var visibility = canConnect ? Visibility.Visible : Visibility.Collapsed;
        }
        WiFiAdapter CurrentAdapter = null;
        WiFiAvailableNetwork CurrentAvailableNetwork = null;

        public void DisplayOneLine(WiFiNetworkInformation value)
        {
            uiWiFiOneLineInformation.Text = $"{value.SSID.OrUnnamed()} BSSID={value.Bssid} RSSI={value.Rssi}";
        }

        private void OnHideRadarDetails(object sender, TappedRoutedEventArgs e)
        {
            uiRadarDetails.Visibility = Visibility.Collapsed;
        }

        private void OnClearRadarDetails(object sender, TappedRoutedEventArgs e)
        {
            uiWiFiDetailsText.Text = "";
        }

        private async void OnConnectDetails(object sender, TappedRoutedEventArgs e)
        {
            if (CurrentAvailableNetwork == null || CurrentAdapter == null) return;
            try
            {
                var result = await CurrentAdapter.ConnectAsync(CurrentAvailableNetwork, WiFiReconnectionKind.Manual);
                var status = result.ConnectionStatus;
                Log($"CONNECT: status={status}");

            }
            catch (Exception ex)
            {
                Log($"CONNECT: Exception: {ex.Message}");
            }
        }
        private void OnDisconnectDetails(object sender, TappedRoutedEventArgs e)
        {
            if (CurrentAvailableNetwork == null || CurrentAdapter == null) return;
            try
            {
                CurrentAdapter.Disconnect();
            }
            catch (Exception ex)
            {
                Log($"DISCONNECT: Exception: {ex.Message}");
            }
        }

        #endregion


        #region TABLE
        private void OnGridSort(object sender, DataGridColumnEventArgs e)
        {
            var dg = uiGrid;
            var list = CurrentNetworkInformationList;
            var name = e.Column.Header.ToString();
            DoGridSort(dg, list, name, e);
        }
        private void DoGridSort(DataGrid dg, IList<WiFiNetworkInformation> list, string name, DataGridColumnEventArgs e)
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
        List<WiFiNetworkInformation> SavedInformation = null;

        private void OnGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var list = CurrentNetworkInformationList;
            var dg = uiGrid;
            if (dg.CurrentColumn == null) return;

            var colName = dg.CurrentColumn.Header.ToString();
            var row = dg.SelectedItem as WiFiNetworkInformation;
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
                SavedInformation = new List<WiFiNetworkInformation>();
                foreach (var item in list)
                {
                    SavedInformation.Add(item);
                }
                // And filter
                for (int rowIndex = list.Count - 1; rowIndex >= 0; rowIndex--)
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
        #endregion


    }
}

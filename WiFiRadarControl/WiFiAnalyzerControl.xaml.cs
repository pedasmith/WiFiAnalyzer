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
                Log(await NetworkToString.ToStringAsync("", item));
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
            Log($"\nScan ended at {DateTime.Now}");
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
            CurrentReflectorList = CurrentReflectorList.OrderByDescending(value => value.NetworkInformation.Rssi).ToList();
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

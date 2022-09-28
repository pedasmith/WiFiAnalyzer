using Microsoft.Toolkit.Uwp.UI.Controls;
using QRCoder;
using SimpleWiFiAnalyzer;
using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

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


        ObservableCollection<WiFiNetworkInformation> CurrentNetworkInformationList = new ObservableCollection<WiFiNetworkInformation>();
        String CurrentCsv = "";
        String CurrentHtml = "";
        List<Reflector> CurrentReflectorList = new List<Reflector>();

        private async Task DoScanAsync()
        {
            uiReport.Text = $"Scan started at {DateTime.Now}\n\n";
            uiScanProgressRing.IsIndeterminate = true;
            uiScanProgressRing.Visibility = Visibility.Visible;
            try
            {
                //var locatorTask = Locator.GetGeopositionAsync(new TimeSpan(0, 1, 0), new TimeSpan(0, 0, 5)); // allow 1-minue old data; timeout within 5 seconds

                uiRadar.Initialize();
                uiRadar.AddDummyReflectors();

                var dg = uiGrid;
                var smd = new ScanMetadata();

                GetNetworkInfo();

                var adapterList = await WiFiAdapter.FindAllAdaptersAsync();
                CurrentCsv = NetworkToString.ToCsvHeader_WiFiNetworkReport() + "\n";
                CurrentHtml = NetworkToString.ToHtmlHeader_WiFiNetworkReport().tr();
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
                        LogNetworkInfo($"Scan error: {e.Message}");
                    }
                    Log(NetworkToString.ToString("    ", wifiAdapter.NetworkReport));
                    //if (locatorTask.Status != AsyncStatus.Error) smd.Position = locatorTask.GetResults();
                    //Log($"DBG: location status={locatorTask.Status} position={smd.Position}");

                    CurrentCsv += NetworkToString.ToCsvData(wifiAdapter.NetworkReport);
                    CurrentHtml += NetworkToString.ToHtmlData(wifiAdapter.NetworkReport);
                    NetworkToString.Fill(wifiAdapter, CurrentNetworkInformationList, wifiAdapter.NetworkReport, smd);
                }
                CurrentHtml = CurrentHtml.html(); // surrounds <tr>..</tr>\n lines with html+body+table
                Log($"\nScan ended at {DateTime.Now}");
                Log("\n\n");

                // Set up the strength lists
                var obl = new OrderedBandList(CurrentNetworkInformationList);
                LogOrderedBandList(obl);
                uiStrength.Children.Clear();
                WiFiStrengthControl.ResetColorIndex();
                foreach (var (freq, obi) in obl.OrderedBands)
                {
                    var ctrl = new WiFiStrengthControl() { DisplayInfo = this, };
                    ctrl.SetStrength(obi);
                    uiStrength.Children.Add(ctrl);
                }

                // Add the locations
                CurrentReflectorList = CreateReflectorList(CurrentNetworkInformationList, CurrentSsid);
                uiRadar.SetReflectors(CurrentReflectorList); // Update the reflectors to represent the new truth about WiFi

            }
            catch (Exception e)
            {
                LogNetworkInfo($"Scan report error: {e.Message}");
            }
            uiScanProgressRing.IsIndeterminate = false;
            uiScanProgressRing.Visibility = Visibility.Collapsed;
            await uiRadar.StopAsync();
        }

        private void GetNetworkInfo()
        {
            CurrentSsid = null;
            try
            {
                var lanList = NetworkInformation.GetLanIdentifiers();
                foreach (var lanItem in lanList)
                {
                    Log(lanItem.InfrastructureId.ToString());
                }

                // https://docs.microsoft.com/en-us/uwp/api/Windows.Networking.Connectivity.ConnectionProfile?view=winrt-22621
                var cp = NetworkInformation.GetInternetConnectionProfile();
                if (cp != null)
                {
                    Log("Internet Connected Profile");
                    LogConnectionProfile(cp);
                    Log("\n\n");
                    LogNetworkInfo($"Connected on: {ConnectionProfileBestSSID(cp)}");
                    if (cp.WlanConnectionProfileDetails != null)
                    {
                        CurrentSsid = cp.WlanConnectionProfileDetails.GetConnectedSsid();
                    }
                    else
                    {
                        CurrentSsid = null;
                    }
                }
                else
                {
                    Log("No internet connection");
                    CurrentSsid = null;
                }
            }
            catch (Exception e)
            {
                Log($"Error while getting netowrk info {e.Message}");
            }
        }

        string CurrentSsid = null;

        /// <summary>
        /// Given a ConnectionProfile, return the best name for the user, preferably the WLAN SSID if it exists and isn't empty.
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        private string ConnectionProfileBestSSID(ConnectionProfile cp)
        {
            var retval = cp.ProfileName;
            if (cp.WlanConnectionProfileDetails != null)
            {
                var ssid = cp.WlanConnectionProfileDetails.GetConnectedSsid();
                if (!String.IsNullOrEmpty(ssid))
                {
                    retval = ssid;
                }
            }
            return retval;
        }

        private void LogConnectionProfile (ConnectionProfile cp)
        {
            Log($"Profile name={cp.ProfileName}");
            if (cp.IsWlanConnectionProfile)
            {
                var details = cp.WlanConnectionProfileDetails;
                Log($"    WiFi SSID={details?.GetConnectedSsid()}");
            }
            Log($"    Adapter={cp.NetworkAdapter.NetworkAdapterId}");
            Log($"    CanDelete={cp.CanDelete}");
            if (cp.ServiceProviderGuid.HasValue)
            {
                Log($"    Service Provider GUID={cp.ServiceProviderGuid}");
            }
            if (cp.IsWwanConnectionProfile)
            {
                var details = cp.WwanConnectionProfileDetails;
                Log($"    Cell AP={details?.AccessPointName}");
                Log($"    Registration State={details.GetNetworkRegistrationState()}");
                Log($"    Home Provider ID={details?.HomeProviderId}");
                Log($"    IPKind={details?.IPKind}");
            }
        }

        private void LogOrderedBandList(OrderedBandList orderList)
        {
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
        }

        private void LogNetworkInfo (string text)
        {
            uiNetworkInfo.Text = text;
        }

        private void LogConnectInfo(string text, bool clear = false)
        {
            if (clear) uiConnectLog.Text = "";
            uiConnectLog.Text = text + "\r" + uiConnectLog.Text;
        }

        private static List<Reflector> CreateReflectorList(IList<WiFiNetworkInformation> list, string matchingSsid)
        {
            var retval = new List<Reflector>();
            foreach (var ninfo in list)
            {
                var reflector = new Reflector();
                reflector.Icon = Reflector.Icon_AP;
                reflector.NetworkInformation = ninfo;
                reflector.SsidToMatch = matchingSsid;
                retval.Add(reflector);
            }
            retval = retval.OrderBy(value => value.NetworkInformation.Rssi).ToList();
            return retval;
        }

        private void Item_AvailableNetworksChanged(WiFiAdapter sender, object args)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkChange: {sender} args=<<{args}>>"); // NOTE: Just to track network changes
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

        private void OnCopyAsCsv(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(CurrentCsv);
            dp.Properties.Title = "Wi-Fi Scan data";
            Clipboard.SetContent(dp);
        }
        private void OnCopyForExcel(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(CurrentHtml);
            dp.Properties.Title = "Wi-Fi Scan data";
            Clipboard.SetContent(dp);
        }
        /// <summary>
        /// Never use. It makes an HTML thing that can't be pasted into notepad++ (!)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCopyForExcelHtml(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            var htmlFormat = HtmlFormatHelper.CreateHtmlFormat(CurrentHtml);
            dp.SetHtmlFormat(htmlFormat);
            dp.Properties.Title = "Wi-Fi Scan data";
            Clipboard.SetContent(dp);
        }

        #endregion


        #region CONNECT

        // Called from MainPage.cs NavigateToWiFiUrlConnect(WiFiUrl url)
        // From app.xaml.cs OnActivated(IActivatedEventArgs args) when started with wifi:S:starpainter;P:deeznuts;;
        public Task NavigateToWiFiConnectUrl(WiFiUrl url)
        {
            uiPivot.SelectedItem = uiConnectPivot;
            uiConnectPassword.Text = url.Password ?? "";
            uiConnectSsid.Text = url.Ssid ?? "";
            uiConnectUrl.Text = url.ToString();
            return ConnectFromUrlAsync(url);
        }

        private async void OnConnect(object sender, RoutedEventArgs e)
        {
            ; // TODO: wire this up
            var ssid = uiConnectSsid.Text;
            var pwd = uiConnectPassword.Text;
            WiFiUrl url = new WiFiUrl(ssid, pwd);
            uiConnectUrl.Text = url.ToString();
            await ConnectFromUrlAsync(url);
        }
        private async Task ConnectWriteQR(Image image, WiFiUrl url)
        {
            QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M;

            //Create raw qr code data
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url.ToString(), eccLevel);

            //Create byte/raw bitmap qr code
            BitmapByteQRCode qrCodeBmp = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImageBmp = qrCodeBmp.GetGraphic(20); // Note: these are colors from the original sample (but they are ugly): , new byte[] { 118, 126, 152 }, new byte[] { 144, 201, 111 });
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(qrCodeImageBmp);
                    await writer.StoreAsync();
                }
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);

                image.Source = bitmapImage;
            }
        }
        private async Task ConnectFromUrlAsync(WiFiUrl url)
        {
            url.WiFiType = "WPA";
            await ConnectWriteQR(uiConnectQR, url);
            var adapterList = await WiFiAdapter.FindAllAdaptersAsync();
            LogConnectInfo($"Finding Wi-Fi network for URL {url}", true);
            LogNetworkInfo($"Finding Wi-Fi network {url.Ssid}");
            if (adapterList.Count < 1)
            {
                LogConnectInfo($"Unable to connect to {url.Ssid}: no Wi-Fi adapters found");
                LogNetworkInfo($"Unable to connect to {url.Ssid}: no Wi-Fi adapters found");
                return; 
            }
            int nFoundAdapter = 0;
            int nConnectedFail = 0;
            int nConnectedSuccess = 0;
            WiFiConnectionStatus lastConnectionStatus = WiFiConnectionStatus.Success;
            foreach (var wifiAdapter in adapterList)
            {
                try
                {
                    await wifiAdapter.ScanAsync();
                }
                catch (Exception e)
                {
                    LogConnectInfo($"Scan error: {e.Message}");
                    LogNetworkInfo($"Scan error: {e.Message}");
                }
                foreach (var network in wifiAdapter.NetworkReport.AvailableNetworks)
                {
                    if (network.Ssid == url.Ssid)
                    {
                        nFoundAdapter++;
                        var pwd = new PasswordCredential() { Password = url.Password };
                        LogConnectInfo($"Found adapter on {url.Ssid}; connecting");
                        var result = await wifiAdapter.ConnectAsync(network, WiFiReconnectionKind.Automatic, pwd);
                        lastConnectionStatus = result.ConnectionStatus;
                        if (result.ConnectionStatus == WiFiConnectionStatus.Success)
                        {
                            nConnectedSuccess++;
                            LogConnectInfo($"Found adapter on {url.Ssid}; connect success");
                            break;// all done.
                        }
                        else
                        {
                            LogConnectInfo($"Found adapter on {url.Ssid}; connect fail reason={result.ConnectionStatus}");
                            nConnectedFail++;
                        }
                    }
                }
            }
            if (nConnectedSuccess > 0)
            {
                LogConnectInfo($"Connected to {url.Ssid}");
                LogNetworkInfo($"Connected to {url.Ssid}");
            }
            else if (nConnectedFail > 0)
            {
                LogConnectInfo($"Unable to connect to {url.Ssid} Reason={lastConnectionStatus}");
                LogNetworkInfo($"Unable to connect to {url.Ssid} Reason={lastConnectionStatus}");
            }
            else
            {
                LogConnectInfo($"No such network: {url.Ssid}");
                LogNetworkInfo($"No such network: {url.Ssid}");
            }
        }

        #endregion
        private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1)
            {
                var pivot = e.AddedItems[0] as PivotItem;
                var tag = pivot.Tag as string;
                if (tag == "MobileHotspot")
                {
                    uiMobileHotspot.TabTo();
                }
            }
            if (e.RemovedItems.Count == 1)
            {
                var pivot = e.RemovedItems[0] as PivotItem;
                var tag = pivot.Tag as string;
                if (tag == "MobileHotspot")
                {
                    uiMobileHotspot.TabAway();
                }
            }
        }
    }
}

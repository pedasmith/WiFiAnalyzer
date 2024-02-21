using MeCardParser;
using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static QRCoder.PayloadGenerator;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SmartWiFiControls
{
    public sealed partial class TetheringControl : UserControl
    {
        public TetheringControl()
        {
            this.InitializeComponent();
        }
        DispatcherTimer RefreshTimer = null;
        static int NLoaded = 0;
        const double RefreshTimerPeriodInSeconds = 5;
        public async Task TabToAsync()
        {
            NLoaded++;
            //TetheringLog($"✨Loaded={NLoaded}");

            if (RefreshTimer == null)
            {
                // Pause a little and then get data?
                int s = (int)Math.Floor(RefreshTimerPeriodInSeconds);
                int ms = (int)Math.Round(((double)s - RefreshTimerPeriodInSeconds) * 1000.0);
                RefreshTimer = new DispatcherTimer()
                {
                    Interval = new TimeSpan(0, 0, 0, s, ms),
                };
                RefreshTimer.Tick += RefreshTimer_Tick;
            }
            RefreshTimer.Start();
            if (!EnsureTetheringManager()) return;
            await ShowAsync(TetheringManager);
        }

        String[] AnimationTimerStringsCat = { "🐱", "😸", "😺", "😼", "😻", };
        String[] AnimationTimerStrings = { "🪐", "🪨", "🌌", "🌑", };

        long AnimationTimerCount = 0;
        private void RefreshTimer_Tick(object sender, object e)
        {
            if (!EnsureTetheringManager()) return;
            var task = ShowAsync(TetheringManager);

            uiAnimationTimer.Text = AnimationTimerStrings[AnimationTimerCount % AnimationTimerStrings.Length];
            AnimationTimerCount++;
        }

        public void TabAway()
        {
            NLoaded--;
            //TetheringLog($"✨Unloaded={NLoaded}");
            if (RefreshTimer == null) return;
            //RefreshTimer.Change(Timeout.Infinite, RefreshTimerPeriodInSeconds);
            RefreshTimer.Stop();
        }



        NetworkOperatorTetheringManager TetheringManager = null;
        private bool EnsureTetheringManager()
        {
            if (TetheringManager != null) return true;
            try
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                //var profileStr = NetworkToString.ToString("", profile);
                //TetheringLog(profileStr);
                if (profile != null) // NOTE: Not connected means null here
                {
                    TetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
                    //TetheringLog(NetworkToString.ToString("", TetheringManager));
                }
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: Unable to make tethering manager: {ex.Message}");
            }
            return TetheringManager != null;
        }

        private string GetAuthString(string defaultValue = "WPA2")
        {
            var retval = (uiTetheringAuth.SelectedItem as FrameworkElement)?.Tag as string ?? defaultValue;
            return retval;
        }

        private string GetBandString(string defaultValue = "2.4")
        {
            var retval = (uiTetheringBand.SelectedItem as FrameworkElement)?.Tag as string ?? defaultValue;
            return retval;
        }

        private string GetPriorityString(string defaultValue = "default")
        {
            var retval = (uiTetheringPriority.SelectedItem as FrameworkElement)?.Tag as string ?? defaultValue;
            return retval;
        }

        private NetworkOperatorTetheringAccessPointConfiguration CreateAPConfiguration()
        {
            var auth = WiFiEnumConverter.AuthFromString(GetAuthString());
            var band = WiFiEnumConverter.BandFromString(GetBandString());
            var configure = new NetworkOperatorTetheringAccessPointConfiguration()
            {
                Ssid = uiTetheringSsid.Text,
                Passphrase = uiTetheringPassphrase.Text,
                //Auth = auth,
                Band = band,
            };
            return configure;
        }

        private NetworkOperatorTetheringSessionAccessPointConfiguration CreateAPSessionConfiguration()
        {
            var band = WiFiEnumConverter.BandFromString(GetBandString());
            var auth = WiFiEnumConverter.AuthFromString(GetAuthString());
            var priority = WiFiEnumConverter.PriorityFromString(GetPriorityString());
            var configure = new NetworkOperatorTetheringSessionAccessPointConfiguration()
            {
                Ssid = uiTetheringSsid.Text,
                Passphrase = uiTetheringPassphrase.Text,
                Band = band,
                AuthenticationKind = auth,
                PerformancePriority = priority,
            };
            return configure;
        }
        private void TetheringLog(string text)
        {
            var start = uiTetheringLog.Text;
            const int maxlen = 100;
            if (start.Length > maxlen) start = "…" + start.Substring(start.Length - maxlen);
            if (start.Length > 0) start += "    ";
            var newtext = start + text;
            uiTetheringLog.Text = newtext;
        }
        private async void OnTetheringConfigureOnly(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;
            uiTetheringLog.Text = "";
            var configure = CreateAPConfiguration();
            await DoTetheringConfigureAsync(configure);
            await ShowAsync(TetheringManager);
        }

        // TODO: start as session
        private async void OnTetheringStartSession(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;
            uiTetheringLog.Text = "";
            var configure = CreateAPSessionConfiguration();
            await DoTetheringStartSessionAsync(configure);
            await ShowAsync(TetheringManager);
        }

        private void SelectAuth()
        {

        }

        /// <summary>
        /// Wraps TetheringManager.ConfigureAccessPointAsync with logging + try/catch
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public async Task<bool> DoTetheringConfigureAsync(NetworkOperatorTetheringAccessPointConfiguration configure)
        {
            var retval = true;
            TetheringLog("Starting Configure");
            try
            {
                await TetheringManager.ConfigureAccessPointAsync(configure);
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: Configure: {ex.Message}");
                retval = false;
            }
            TetheringLog("Complete");
            return retval;
        }

        /// <summary>
        /// Calls TetheringStartAsync and ShowAsync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnTetheringStart(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;

            uiTetheringLog.Text = "";
            await DoTetheringStartAsync();
            await ShowAsync(TetheringManager);
        }

        /// <summary>
        /// Wraps StartTetheringAsync with logging and try/catch
        /// </summary>
        /// <returns></returns>
        private async Task<bool> DoTetheringStartAsync()
        {
            if (!EnsureTetheringManager()) return false;

            var retval = false;
            TetheringLog("Starting mobile hotspot");
            try
            {
                var result = await TetheringManager.StartTetheringAsync();
                TetheringLog($"Tether: {result.Status} {result.AdditionalErrorMessage}");
                retval = result.Status == TetheringOperationStatus.Success;
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: {ex.Message}");
                retval = false;
            }
            TetheringLog("Complete");
            await ShowAsync(TetheringManager);
            return retval;
        }

        /// <summary>
        /// Wraps StartTetheringAsync with logging and try/catch 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private async Task<bool> DoTetheringStartSessionAsync(NetworkOperatorTetheringSessionAccessPointConfiguration config)
        {
            if (!EnsureTetheringManager()) return false;

            var retval = false;
            TetheringLog("Starting mobile hotspot");
            try
            {
                var result = await TetheringManager.StartTetheringAsync(config);
                TetheringLog($"Tether: {result.Status} {result.AdditionalErrorMessage}");
                retval = result.Status == TetheringOperationStatus.Success;
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: {ex.Message}");
                retval = false;
            }
            TetheringLog("Complete");
            await ShowAsync(TetheringManager);
            return retval;
        }


        private async void OnTetheringStop(object sender, RoutedEventArgs e)
        {
            await DoTetheringStop();
            await ShowAsync(TetheringManager);
        }

        private async Task DoTetheringStop()
        {
            if (TetheringManager == null) return;
            TetheringLog("Stopping");
            var step = "Stopping";
            try
            {
                var result = await TetheringManager.StopTetheringAsync();
                TetheringLog($"Tether: {result.Status} {result.AdditionalErrorMessage}");
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: {step}: {ex.Message}");
            }
            TetheringLog("Complete");
            await ShowAsync(TetheringManager);
        }
        private void OnTetheringListProfiles(object sender, RoutedEventArgs e)
        {
            TetheringLog("Profile List");
            var list = NetworkInformation.GetConnectionProfiles();
            foreach (var item in list)
            {
                var text = NetworkToString.ToString("", item);
                TetheringLog(text);
                TetheringLog("\n\n");
            }
            TetheringLog("Complete");
        }

        private void OnTetheringShowManager(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;
            var str = NetworkToString.ToString("", TetheringManager);
            TetheringLog(str);
        }
        private void OnTetheringClearScreen(object sender, RoutedEventArgs e)
        {
            uiTetheringLog.Text = "";
        }

        private async Task ShowAsync(NetworkOperatorTetheringManager manager = null)
        {
            if (manager == null)
            {
                manager = TetheringManager;
                if (manager == null)
                {
                    EnsureTetheringManager();
                    manager = TetheringManager;
                }
            }
            if (manager == null) return;
            uiStatus.Text = manager.TetheringOperationalState.ToString();
            uiCount.Text = manager.ClientCount.ToString();
            uiMaxCount.Text = manager.MaxClientCount.ToString();

            // Set the visibility of the buttons. If the state is transition or unknown, both buttons will be disabled.
            uiTetherStartButton.Visibility = manager.TetheringOperationalState == TetheringOperationalState.Off ? Visibility.Visible : Visibility.Collapsed;
            uiTetherStopButton.Visibility = manager.TetheringOperationalState == TetheringOperationalState.On ? Visibility.Visible : Visibility.Collapsed;

            NetworkOperatorTetheringAccessPointConfiguration apconfiguration = null;
            try
            {
                apconfiguration = manager.GetCurrentAccessPointConfiguration();
            }
            catch (Exception ex)
            {
                // NOTE: Gotcha! the GetCurrentAccessPointConfiguration can throw!
                apconfiguration = null;
                Log($"Error: unable to set up access point configuration; exception {ex.Message}");
            }
            if (apconfiguration != null)
            {
                uiSsid.Text = apconfiguration.Ssid;
                uiPassword.Text = apconfiguration.Passphrase;
                uiBand.Text = NetworkToString.ToString(apconfiguration.Band);

                var url = new WiFiUrl(apconfiguration);
                await ShowWiFiQRCodeAsync(url); // Note that the url is modified

                var bandstr = "";
                var bandlist = new List<TetheringWiFiBand>() { TetheringWiFiBand.TwoPointFourGigahertz, TetheringWiFiBand.FiveGigahertz };
                foreach (var band in bandlist)
                {
                    try
                    {
                        if (apconfiguration.IsBandSupported(band)) bandstr += NetworkToString.ToString(band) + " ";
                    }
                    catch (Exception)
                    {
                        ; // TODO: this failed at work -- why?
                    }
                }
                uiSupportedBand.Text = bandstr;
            }

            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile != null)
            {
                var tcap = NetworkOperatorTetheringManager.GetTetheringCapabilityFromConnectionProfile(profile);
                uiTetheringEnabled.Text = tcap.ToString(); // NOTE: weirdly, some connection profiles say that tething is enabled, but it's not actually allowed.
                uiConnectedToProfileName.Text = profile.ProfileName;
            }

            var clients = manager.GetTetheringClients();
            if (clients.Count == 0)
            {
                uiConnectedClientsTitle.Visibility = Visibility.Collapsed;
                uiConnectedClients.Visibility = Visibility.Collapsed;
                uiConnectedClients.Children.Clear();
            }
            else
            {
                uiConnectedClientsTitle.Visibility = Visibility.Visible;
                uiConnectedClients.Visibility = Visibility.Visible;
                uiConnectedClients.Children.Clear();
                try
                {
                    foreach (var client in clients)
                    {
                        var clientstr = "    ";
                        foreach (var host in client.HostNames)
                        {
                            clientstr += host.DisplayName + " ";
                        }
                        clientstr += $"MAC={client.MacAddress}";
                        var tb = new TextBlock()
                        {
                            Text = clientstr
                        };
                        uiConnectedClients.Children.Add(tb);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error: Exception: Show Clients: exception={ex.Message}");
                }
            }
        }

        private void Log(string str)
        {
            TetheringLog(str);
            Console.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);
        }

        public async Task DoMeCardActionAsync(MeCardRaw mecard)
        {
            var context = mecard.GetFieldValue("context", "unknown");
            var action = mecard.GetFieldValue("action", "unknown");
            // Mecard values:
            // action: start start-session stop report
            // S:ssid
            // P:password
            // T:WPA2 WPA3 WPA3+2
            // band: 2.4 5 6 auto
            // priority: normal tethering
            // 
            switch (context)
            {
                case "hotspot":
                    {
                        switch (action)
                        {
                            case "start":
                                {
                                    UpdateUXFromMecard(mecard);

                                    if (!EnsureTetheringManager()) return;
                                    uiTetheringLog.Text = "";
                                    var configure = CreateAPConfiguration();
                                    // Important note: if you configure an invalid SSID or Password (or whatever),
                                    // there's no error code that comes back. The configuration won't be accepted
                                    await DoTetheringConfigureAsync(configure);
                                    await DoTetheringStartAsync();

                                    // Update the UX
                                    await ShowAsync(TetheringManager);
                                }
                                break;
                            case "start-session":
                                {
                                    UpdateUXFromMecard(mecard);

                                    if (!EnsureTetheringManager()) return;
                                    uiTetheringLog.Text = "";
                                    var configureSession = CreateAPSessionConfiguration();
                                    // Important note: if you configure an invalid SSID or Password (or whatever),
                                    // there's no error code that comes back. The configuration won't be accepted
                                    await DoTetheringStartSessionAsync(configureSession);

                                    // Update the UX
                                    await ShowAsync(TetheringManager);
                                }
                                break;
                            case "stop":
                                if (!EnsureTetheringManager()) return;
                                uiTetheringLog.Text = "";

                                // Code from OnTetheringStop
                                TetheringLog("Stopping");
                                var step = "Stopping";
                                try
                                {
                                    var result = await TetheringManager.StopTetheringAsync();
                                    TetheringLog($"Tether: {result.Status} {result.AdditionalErrorMessage}");
                                }
                                catch (Exception ex)
                                {
                                    TetheringLog($"ERROR: {step}: {ex.Message}");
                                }
                                TetheringLog("Complete");
                                // Update the UX
                                await ShowAsync(TetheringManager);
                                break;
                        }
                    }
                    break;
                default:
                    Log($"Error: unknown context:{context};action:{action}");
                    break;
            }
        }

        private void UpdateUXFromMecard(MeCardRaw mecard)
        {
            uiTetheringSsid.Text = mecard.GetFieldValue("S", "notsetup");
            uiTetheringPassphrase.Text = mecard.GetFieldValue("P", "notsetup");
            var bandstr = mecard.GetFieldValue("band", "2.4");
            SelectBand(bandstr);
            var authstr = mecard.GetFieldValue("T", "WPA2");
            SelectAuth(authstr);
            var prioritystr = mecard.GetFieldValue("priority", "default");
            SelectPriority(prioritystr);
        }

        private void SelectAuth(string value)
        {
            var combo = uiTetheringAuth;
            foreach (var item in combo.Items)
            {
                var tagstr = (item as FrameworkElement)?.Tag as string ?? "";
                if (tagstr == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            Log($"Unknown auth (T) type {value}");
        }

        private void SelectBand(string value)
        {
            var combo = uiTetheringBand;
            foreach (var item in combo.Items)
            {
                var tagstr = (item as FrameworkElement)?.Tag as string ?? "";
                if (tagstr == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            Log($"Unknown band {value}");
        }

        private void SelectPriority(string value)
        {
            var combo = uiTetheringPriority;
            foreach (var item in combo.Items)
            {
                var tagstr = (item as FrameworkElement)?.Tag as string ?? "";
                if (tagstr == value)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            Log($"Unknown priority {value}");
        }


        /// <summary>
        /// Mostly obsolete; from when I added a SETUP as an "action" for a WIFI: URL. This is not something
        /// I want to support going forward. It's still in code, but should be removed.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task SetupFromWiFiSetupUrl(WiFiUrl url)
        {
            if (url == null || url.Action != "SETUP") return;
            uiTetheringSsid.Text = url.Ssid;
            uiTetheringPassphrase.Text = url.Password;

            var configure = CreateAPConfiguration();
            var configureOk = await DoTetheringConfigureAsync(configure);
            bool startOk = false;
            if (configureOk) startOk = await DoTetheringStartAsync();
            var ok = configureOk && startOk;

            if (!ok)
            {
                uiConnectQRPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                await ShowWiFiQRCodeAsync(url); // Note that the url is modified
            }
        }

        WiFiUrl CurrWiFiUrl = null;
        private async Task ShowWiFiQRCodeAsync(WiFiUrl url)
        {
            if (url == null || (url.Scheme != "WIFISETUP" && url.Scheme != "WIFI")) return;
            CurrWiFiUrl = url;
            uiConnectQRPanel.Visibility = Visibility.Visible;
            url.Scheme = "WIFI"; // switch over the wifi: url for the QR Code.
            url.WiFiAuthType = "WPA"; // we always make a WPA connection
            var newText = url.ToString();
            if (newText != uiConnectWifiUrl.Text) // Only update if it's changed.
            {
                uiConnectWifiUrl.Text = newText;
                ImageStream = await WiFiUrlToQRCode.ConnectWriteQR(uiConnectQR, url);
            }
        }

        private WiFiUrl WiFiUrlFromConnectUI()
        {
            return CurrWiFiUrl;
        }
        DataTransferManager Dtm = null;
        IRandomAccessStream ImageStream = null;

        private void OnConnectCopy(object sender, RoutedEventArgs e)
        {
            var wifiurl = WiFiUrlFromConnectUI();
            if (wifiurl == null) return;
            var imageStream = (uiConnectQRPanel.Visibility == Visibility.Visible) ? ImageStream : null; // can be null;
            CopyAndShare.Copy(wifiurl, imageStream);
        }


        private void OnConnectShare(object sender, RoutedEventArgs e)
        {
            var wifiurl = WiFiUrlFromConnectUI();
            if (wifiurl == null) return;
            if (Dtm == null)
            {
                Dtm = DataTransferManager.GetForCurrentView();
                Dtm.DataRequested += ConnectDtm_DataRequested;
            }
            DataTransferManager.ShowShareUI();
        }

        private void ConnectDtm_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var dataPackage = args.Request.Data;
            var wifiurl = WiFiUrlFromConnectUI();
            if (wifiurl == null) return;
            var imageStream = (uiConnectQRPanel.Visibility == Visibility.Visible) ? ImageStream : null; // can be null;
            CopyAndShare.FillDataPackage(dataPackage, wifiurl, imageStream);
        }
    }
}

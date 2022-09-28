using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
        public void TabTo()
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
            Show(TetheringManager);
        }

        String[] AnimationTimerStringsCat = { "🐱", "😸", "😺", "😼", "😻", };
        String[] AnimationTimerStrings = { "🪐", "🪨", "🌌", "🌑", };

        long AnimationTimerCount = 0;
        private void RefreshTimer_Tick(object sender, object e)
        {
            if (!EnsureTetheringManager()) return;
            Show(TetheringManager);

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
                TetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
                //TetheringLog(NetworkToString.ToString("", TetheringManager));
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: Unable to make tethering manager: {ex.Message}");
            }
            return TetheringManager != null;
        }
        private NetworkOperatorTetheringAccessPointConfiguration CreateAPConfiguration()
        {
            var configure = new NetworkOperatorTetheringAccessPointConfiguration()
            {
                Ssid = uiTetheringSsid.Text,
                Passphrase = uiTetheringPassphrase.Text,
                Band = TetheringWiFiBand.FiveGigahertz, //TODO: select band, too?
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
            if (!EnsureTetheringManager()) return ;
            uiTetheringLog.Text = "";
            var configure = CreateAPConfiguration();
            await DoTetheringConfigureAsync(configure);
            Show(TetheringManager);
        }

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

        private async void OnTetheringStart(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;

            uiTetheringLog.Text = "";
            await DoTetheringStartAsync();
            Show(TetheringManager);
        }

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
            Show(TetheringManager);
            return retval;
        }
        private async void OnTetheringStop(object sender, RoutedEventArgs e)
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
            Show(TetheringManager);
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
        private void Show(NetworkOperatorTetheringManager manager = null)
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

            var apconfiguration = manager.GetCurrentAccessPointConfiguration();
            if (apconfiguration != null)
            {
                uiSsid.Text = apconfiguration.Ssid;
                uiPassword.Text = apconfiguration.Passphrase;
                uiBand.Text = NetworkToString.ToString(apconfiguration.Band);
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
                uiTetheringEnabled.Text = tcap.ToString();
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
                foreach (var client in clients)
                {
                    var clientstr ="    ";
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
        }

        public async Task SetupFromWiFiSetupUrl(WiFiUrl url)
        {
            if (url == null || url.Scheme != "wifisetup") return;
            uiTetheringSsid.Text = url.Ssid;
            uiTetheringPassphrase.Text = url.Password;

            var configure = CreateAPConfiguration();
            var configureOk = await DoTetheringConfigureAsync(configure);
            bool startOk = false;
            if (configureOk) startOk = await DoTetheringStartAsync();
            var ok = configureOk && startOk;

            if (!ok)
            {
                uiConnectQR.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiConnectQR.Visibility = Visibility.Visible;
                url.Scheme = "wifi"; // switch over the wifi: url for the QR Code.
                url.WiFiType = "WPA"; // we always make a WPA connection
                await WiFiUrlToQRCode.ConnectWriteQR(uiConnectQR, url);
            }
        }
    }
}

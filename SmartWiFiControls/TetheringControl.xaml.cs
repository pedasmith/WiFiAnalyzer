using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
                Band = TetheringWiFiBand.FiveGigahertz,
            };
            return configure;
        }
        private void TetheringLog(string text)
        {
            uiTetheringLog.Text += text + "\n";
        }
        private async void OnTetheringConfigureOnly(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return ;
            uiTetheringLog.Text = "";
            TetheringLog("Starting Configure");
            var configure = CreateAPConfiguration();
            try
            {
                await TetheringManager.ConfigureAccessPointAsync(configure);
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: Configure: {ex.Message}");
            }
            TetheringLog("Complete");
        }

        private async void OnTetheringConfigureStart(object sender, RoutedEventArgs e)
        {
            if (!EnsureTetheringManager()) return;

            uiTetheringLog.Text = "";
            TetheringLog("Starting Configure");
            var configure = CreateAPConfiguration();
            var step = "Creating tethering";
            try
            {
                step = "Configuring";
                await TetheringManager.ConfigureAccessPointAsync(configure);

                step = "Starting";
                var result = await TetheringManager.StartTetheringAsync();
                TetheringLog($"Tether: {result.Status} {result.AdditionalErrorMessage}");
            }
            catch (Exception ex)
            {
                TetheringLog($"ERROR: {step}: {ex.Message}");
            }
            TetheringLog("Complete");
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
    }
}

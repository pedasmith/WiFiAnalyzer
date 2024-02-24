using MeCardParser;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NetworkSetup
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

        private void Log(string str)
        {
            uiLog.Text += str + "\n";
        }


        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            Log($"NOTE: Location request status:{accessStatus}");

            // Set up the help system
            var pagename = "NetworkSetupHelp.md";
            uiHelpText.UriPrefix = "ms-appx:///Assets/HelpFiles/";
            uiHelpText.LinkClicked += UiHelpText_LinkClicked;
            var version = SystemInformation.Instance.ApplicationVersion;
            uiHelpVersion.Text = $"Version {version.Major}.{version.Minor}";

            const string StartPage = "NetworkSetupHelp.md";
            //pagename = this.DataContext as string;
            if (String.IsNullOrEmpty(pagename))
            {
                pagename = StartPage;
            }
            await HelpGotoAsync(pagename);

        }

        private void OnPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }


        public async Task NavigateToMeCard(MeCardRaw mecard)
        {
            switch (mecard.SchemeCanonical)
            {
                case "NETWORKSETUP":
                    {
                        // Example: networksetup:context:hotspot;action:start;S:myhotspot;P:password;band:5;auth:WPA3;
                        // potential actions includes start stop report
                        var context = mecard.GetFieldValue("context", "unknown");
                        var action = mecard.GetFieldValue("action", "unknown");
                        await uiMobileHotspot.TabToAsync();
                        switch (context)
                        {
                            case "hotspot":
                                await uiMobileHotspot.DoMeCardActionAsync(mecard);
                                break;
                        }
                    }
                    break;
            }
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

        #region DisplayRequest_RequestActive
        DisplayRequest CurrDisplayRequest = null;
        private void OnCheckScreenOff(object sender, RoutedEventArgs e)
        {
            CurrDisplayRequest.RequestRelease();
        }
        private void OnCheckScreenOn(object sender, RoutedEventArgs e)
        {
            if (CurrDisplayRequest == null) {
                CurrDisplayRequest = new Windows.System.Display.DisplayRequest();
            }
            CurrDisplayRequest.RequestActive();
        }
        #endregion
    }
}

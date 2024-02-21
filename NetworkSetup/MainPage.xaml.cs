using MeCardParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    }
}

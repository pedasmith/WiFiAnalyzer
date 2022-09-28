using Microsoft.Toolkit.Uwp.UI.Controls;
using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.WiFi;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace SimpleWiFiAnalyzer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            int nerror = 0;
            nerror += WiFiBandChannel.TestFindOverlapping();
            nerror += WiFiUrl.Test();
            if (nerror != 0)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: NError is {nerror}; should be 0.");
            }
            this.Loaded += MainPage_Loaded;
        }

        public Task NavigateToWiFiUrlConnect(WiFiUrl url)
        {
            return uiWiFiAnalyzer.NavigateToWiFiConnectUrl(url);
            ;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
             // Nope, no need to do this: MathLogisticFunctions.Demonstrate();
        }
    }
}

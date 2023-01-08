using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SpeedTests
{
    public sealed partial class SpeedTestControl : UserControl
    {
        public SpeedTestControl()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Run the test automatically.
        /// </summary>
        public async Task DoTest()
        {
            var result = await SpeedTest.LatencyTestAsync(null); // null=use default server.
            uiLatencyStats.SetStatistics(result.SpeedStatistics);
            uiLog.Text += $"Server={result.Server} Port={result.Port}\nNSent={result.NSent} NRecv={result.NRecv} Error={result.Error ?? "(no error)"}";
        }

        FccSpeedTest2022 SpeedTest = new FccSpeedTest2022();

        private async void OnTest(object sender, RoutedEventArgs e)
        {
            await DoTest();
        }
    }
}

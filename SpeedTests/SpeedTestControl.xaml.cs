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
    public interface ISetStatistics
    {
        void SetStatistics(Statistics value);
    }
    public sealed partial class SpeedTestControl : UserControl
    {
        public SpeedTestControl()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Run the test automatically.
        /// </summary>
        public async Task DoLatencyTest()
        {
            var graph = new BoxWhiskerControl();
            uiLatencyGraphPanel.Items.Insert(0, graph);
            await Task.Delay(0);
            var uxlist = new List<ISetStatistics>() { graph, uiLatencyStats };

            var result = await SpeedTest.LatencyTestAsync(uxlist, null); // null=use default server.

            graph.SetStatistics(result.SpeedStatistics);
            graph.SetStatistics(result.SpeedStatistics);
            uiLatencyStats.SetStatistics(result.SpeedStatistics);
            uiLog.Text += $"Server={result.Server}:{result.Port}\nNSent={result.NSent} NRecv={result.NRecv} Error={result.Error ?? "(no error)"}\n\n";
        }

        FccSpeedTest2022 SpeedTest = new FccSpeedTest2022();

        private async void OnTest(object sender, RoutedEventArgs e)
        {
            await DoLatencyTest();
        }

        private void OnSelectChange(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            var graph = e.AddedItems[0] as BoxWhiskerControl;
            if (graph == null) return;
            var stats = graph.GetStatistics();
            uiLatencyStats.SetStatistics(stats);
            //MessageBox.Show("Parent");
        }
    }
}

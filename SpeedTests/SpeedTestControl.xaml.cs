using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
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
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The User Control stattype template is documented at https://go.microsoft.com/fwlink/?LinkId=234236


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
        /// 
        public async Task DoSpeedTests()
        {
            var list = GetSpeedTestList();
            foreach (var speedtype in list)
            {
                switch (speedtype)
                {
                    case SpeedTestType.Latency: await DoLatencyTest(); break;
                    case SpeedTestType.Download: await DoThroughputTest(); break;
                }
            }
        }


        enum SpeedTestType {  Latency, Download, }
        private List<SpeedTestType> GetSpeedTestList()
        {
            var retval = new List<SpeedTestType>();
            var str = (uiStatsType.SelectedItem as ComboBoxItem)?.Tag as String;
            var list = str.Split(new char[] { ' ' });
            foreach (var stattype in list)
            {
                switch (stattype)
                {
                    case "Latency": retval.Add(SpeedTestType.Latency); break;
                    case "Download": retval.Add(SpeedTestType.Download); break;
                    default:
                        Log($"ERROR: unknown statistics type {stattype}; expected Latency or Download");
                        break;
                }
            }
            return retval;
        }


        private async Task DoLatencyTest()
        {
            var graph = new BoxWhiskerControl();
            uiLatencyGraphPanel.Items.Insert(0, graph);
            await Task.Delay(0);
            var uxlist = new List<ISetStatistics>() { graph, uiLatencyStats };

            var result = await SpeedTest.LatencyTestAsync(uxlist, null); // null=use default server.

            graph.SetStatistics(result.SpeedStatistics);
            uiLatencyStats.SetStatistics(result.SpeedStatistics);
            uiLog.Text += $"Server={result.Server}:{result.Port}\nNSent={result.NSent} NRecv={result.NRecv} Error={result.Error ?? "(no error)"}\n\n";
        }

        YGraph CurrThroughputGraph;
        private async Task DoThroughputTest()
        {
            CurrThroughputGraph = new YGraph();
            uiLatencyGraphPanel.Items.Insert(0, CurrThroughputGraph);

            uiThroughput.Text = "Starting\n";
            var result = new FccSpeedTest2022.ThroughputTestResult();
            var task = SpeedTest.DownloadTest(result);
            while (!task.IsCompleted)
            {
                await Task.Delay(200);
                uiThroughput.Text += $"{result}\n";
                CurrThroughputGraph.AddValue(result.CurrSpeedInMbpsRounded);
            }
            uiThroughput.Text += "Done\n\n";
        }

        FccSpeedTest2022 SpeedTest = new FccSpeedTest2022();

        private void OnSelectChange(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            var graph = e.AddedItems[0] as BoxWhiskerControl;
            if (graph == null) return;
            var stats = graph.GetStatistics();
            uiLatencyStats.SetStatistics(stats);
            //MessageBox.Show("Parent");
        }

        private static void Log(string str)
        {
            Console.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
}

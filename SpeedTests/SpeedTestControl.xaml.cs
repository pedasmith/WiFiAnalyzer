using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
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
        void SetStatistics(Statistics value, bool displayA);
    }
    public sealed partial class SpeedTestControl : UserControl
    {
        public IGetSpeedTestOptions SpeedTestOptions = null;
        public WiFiRadarControl.IShowProgressRing ShowProgressRing = null;
        public SpeedTestControl()
        {
            this.InitializeComponent();
            this.Loaded += SpeedTestControl_Loaded;
        }

        private void SpeedTestControl_Loaded(object sender, RoutedEventArgs e)
        {
            //No longer here; it's on the WiFiAnalyzerControl and will be set on creation. SpeedTestOptions = uiSpeedTestOptionControl;
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
                    case SpeedTestType.Download: await DoDownloadTest(); break;
                    case SpeedTestType.Upload: await DoUploadTest(); break;
                }
            }
        }


        enum SpeedTestType {  Latency, Download, Upload, }
        private List<SpeedTestType> GetSpeedTestList()
        {
            var retval = new List<SpeedTestType>();
            var str = SpeedTestOptions.GetTestType();
            var list = str.Split(new char[] { ' ' });
            foreach (var stattype in list)
            {
                switch (stattype)
                {
                    case "Latency": retval.Add(SpeedTestType.Latency); break;
                    case "Download": retval.Add(SpeedTestType.Download); break;
                    case "Upload": retval.Add(SpeedTestType.Upload); break;
                    default:
                        Log($"ERROR: unknown statistics type {stattype}; expected Latency or Download or Upload");
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

            var server = SpeedTestOptions.GetServer();
            ShowProgressRing?.StartProgressIndeterminate();
            var result = await SpeedTest.LatencyTestAsync(uxlist, new HostName(server));
            ShowProgressRing?.StopProgressIndeterminate();
            // TODO: this keeps on updating the latency display while other
            // items are selected.
            //TODO: SPEED STAT can be null
            if (result.SpeedStatistics != null)
            {
                graph.SetStatistics(result.SpeedStatistics, true);
                uiLatencyStats.SetStatistics(result.SpeedStatistics, true);
                uiLog.Text += $"Server={result.Server}:{result.Port}\nNSent={result.NSent} NRecv={result.NRecv} Error={result.Error ?? "(no error)"}\n\n";
            }
            else
            {
                uiLog.Text += "TODO: no results";
            }
        }

        YGraph CurrThroughputGraph;
        private async Task DoDownloadTest()
        {
            var serverName = SpeedTestOptions.GetServer();

            CurrThroughputGraph = new YGraph();
            uiLatencyGraphPanel.Items.Insert(0, CurrThroughputGraph);

            var stats = new Statistics(new double[] { 0.0});
            CurrThroughputGraph.CurrStatistics = stats;
            var speed = new Statistics.AdditionalInfo("Throughput", "0.0");
            stats.PreAdditionalInfo.Add(speed);
            var nbytes = new Statistics.AdditionalInfo("N Bytes", "0.0");
            stats.PreAdditionalInfo.Add(nbytes);
            var time = new Statistics.AdditionalInfo("Time (s)", "0.0");
            stats.PreAdditionalInfo.Add(time);

            var server = new Statistics.AdditionalInfo("Server", serverName);
            stats.PostAdditionalInfo.Add(server);
            var at = new Statistics.AdditionalInfo("At", DateTime.Now.ToLongTimeString());
            stats.PostAdditionalInfo.Add(at);

            ShowProgressRing?.StartProgressIndeterminate();
            var result = new FccSpeedTest2022.ThroughputTestResult();
            var task = SpeedTest.DownloadTest(result, serverName);
            var startTime = DateTimeOffset.UtcNow;
            bool overTime = false;
            while (!task.IsCompleted && !overTime)
            {
                await Task.Delay(250);
                //uiThroughput.Text += $"{result}\n";
                CurrThroughputGraph.AddValue(result.SnapshotSpeedInMbpsRounded);
                speed.Value = result.SnapshotSpeedInMbpsRounded.ToString() + " Mbps";
                nbytes.Value = ((result.SnapshotTransferInBytes)/(1024*1024)).ToString();
                time.Value = result.SnapshotTimeAverageInSeconds.ToString("N1");

                if (uiLatencyGraphPanel.SelectedIndex < 1)
                {
                    uiLatencyStats.SetStatistics(stats, false); // not full stats
                }

                var timeInSeconds = DateTimeOffset.UtcNow.Subtract(startTime).TotalSeconds;
                if (timeInSeconds > 10.0) // the FCC download test only runs for 8 seconds
                {
                    overTime = true;
                }
            }
            CurrThroughputGraph.SetValue(result.SpeedInMbpsRounded);

            speed.Value = result.SpeedInMbpsRounded.ToString() + " Mbps";
            nbytes.Value = (result.NBytes / (1024 * 1024)).ToString();
            time.Value = result.TimeAverageInSeconds.ToString("N1");
            uiLatencyStats.SetStatistics(stats, false); // not full stats
            ShowProgressRing?.StopProgressIndeterminate();

        }
        private async Task DoUploadTest()
        {
            var serverName = SpeedTestOptions.GetServer();

            CurrThroughputGraph = new YGraph();
            uiLatencyGraphPanel.Items.Insert(0, CurrThroughputGraph);

            var stats = new Statistics(new double[] { 0.0 });
            CurrThroughputGraph.CurrStatistics = stats;
            var speed = new Statistics.AdditionalInfo("Throughput", "0.0");
            stats.PreAdditionalInfo.Add(speed);
            var nbytes = new Statistics.AdditionalInfo("N Bytes", "0.0");
            stats.PreAdditionalInfo.Add(nbytes);
            var time = new Statistics.AdditionalInfo("Time (s)", "0.0");
            stats.PreAdditionalInfo.Add(time);

            var server = new Statistics.AdditionalInfo("Server", serverName);
            stats.PostAdditionalInfo.Add(server);
            var at = new Statistics.AdditionalInfo("At", DateTime.Now.ToLongTimeString());
            stats.PostAdditionalInfo.Add(at);

            ShowProgressRing?.StartProgressIndeterminate();
            var result = new FccSpeedTest2022.ThroughputTestResult();
            var task = SpeedTest.UploadTest(result, serverName);
            var startTime = DateTimeOffset.UtcNow;
            bool overTime = false;
            while (!task.IsCompleted && !overTime)
            {
                await Task.Delay(250);
                //uiThroughput.Text += $"{result}\n";
                CurrThroughputGraph.AddValue(result.SnapshotSpeedInMbpsRounded);
                speed.Value = result.SnapshotSpeedInMbpsRounded.ToString() + " Mbps";
                nbytes.Value = (result.SnapshotTransferInBytes / (1024 * 1024)).ToString();
                time.Value = result.SnapshotTimeAverageInSeconds.ToString("N1");

                if (uiLatencyGraphPanel.SelectedIndex < 1)
                {
                    uiLatencyStats.SetStatistics(stats, false); // not full stats
                }

                var timeInSeconds = DateTimeOffset.UtcNow.Subtract(startTime).TotalSeconds;
                if (timeInSeconds > 10.0) // the FCC upload test only runs for 8 seconds
                {
                    overTime = true;
                }
            }
            CurrThroughputGraph.SetValue(result.SpeedInMbpsRounded);

            speed.Value = result.SpeedInMbpsRounded.ToString() + " Mbps";
            nbytes.Value = (result.NBytes / (1024 * 1024)).ToString();
            time.Value = result.TimeAverageInSeconds.ToString("N1");
            uiLatencyStats.SetStatistics(stats, false); // not full stats
            ShowProgressRing?.StopProgressIndeterminate();

        }

        FccSpeedTest2022 SpeedTest = new FccSpeedTest2022();

        private void OnSelectChange(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            Statistics stats = null;
            bool fullStats = true;
            if (e.AddedItems[0] is BoxWhiskerControl bwc)
            {
                stats = bwc.GetStatistics();
            }
            else if (e.AddedItems[0] is YGraph yg)
            {
                fullStats = false; // I just know this -- the YGraph is the download or upload data which doesn't incldue all of the stddev etc values.
                stats = yg.GetStatistics();
            }
            if (stats == null) return;
            uiLatencyStats.SetStatistics(stats, fullStats);
            //MessageBox.Show("Parent");
        }

        private static void Log(string str)
        {
            Console.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
}

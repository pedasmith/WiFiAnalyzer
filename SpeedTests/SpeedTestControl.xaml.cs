using ColorCode.Compilation.Languages;
using SmartWiFiHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WiFiRadarControl;
using Windows.ApplicationModel.DataTransfer;
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
using static System.Net.WebRequestMethods;

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
        public UsefulNetworkInformation CurrentUsefulNetworkInfo = null;
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
            CurrentUsefulNetworkInfo.Notes = SpeedTestOptions.GetNotes();
            SpeedTest.CurrentUsefulNetworkInfo = CurrentUsefulNetworkInfo; // Update to match latest value

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
            if (result.SpeedStatistics != null)
            {
                graph.SetStatistics(result.SpeedStatistics, true);
                uiLatencyStats.SetStatistics(result.SpeedStatistics, true);
                uiLog.Text += $"Server={result.Server}:{result.Port}\nNSent={result.NSent} NRecv={result.NRecv} Error={result.Error ?? "(no error)"}\n\n";
            }
            else
            {
                uiLog.Text += "Error: no results";
            }
        }


        YGraph CurrThroughputGraph;
        private async Task DoDownloadTest()
        {
            var serverName = SpeedTestOptions.GetServer();

            CurrThroughputGraph = new YGraph();
            CurrThroughputGraph.UpdateTitle("Download");
            uiLatencyGraphPanel.Items.Insert(0, CurrThroughputGraph);

            var stats = new Statistics(new double[] { 0.0});
            stats.TestType = "Download";
            CurrThroughputGraph.CurrStatistics = stats;
            var speed = new Statistics.AdditionalInfo("Throughput", "0.0");
            stats.PreAdditionalInfo.Add(speed);
            var nbytes = new Statistics.AdditionalInfo("Bytes", "0.0");
            stats.PreAdditionalInfo.Add(nbytes);
            var time = new Statistics.AdditionalInfo("Time (s)", "0.0");
            stats.PreAdditionalInfo.Add(time);

            FccSpeedTest2022.AddAdditionalStatsPost(stats, CurrentUsefulNetworkInfo, serverName, null);

            ShowProgressRing?.StartProgressIndeterminate();
            var result = new FccSpeedTest2022.ThroughputTestResult();
            var task = SpeedTest.DownloadTest(result, serverName);
            var startTime = DateTimeOffset.UtcNow;
            bool overTime = false;
            while (!task.IsCompleted && !overTime)
            {
                await Task.Delay(250);
                //uiLogging.Text += $"{result}\n";
                CurrThroughputGraph.AddValue(result.SnapshotSpeedInMbpsRounded);
                speed.Value = result.SnapshotSpeedInMbpsRounded.ToString() + " Mbps";
                nbytes.Value = AsMBytes((double)result.SnapshotTransferInBytes);
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
            nbytes.Value = AsMBytes(result.NBytes);
            time.Value = result.TimeAverageInSeconds.ToString("N1");
            uiLatencyStats.SetStatistics(stats, false); // not full stats
            ShowProgressRing?.StopProgressIndeterminate();

        }
        private async Task DoUploadTest()
        {
            var serverName = SpeedTestOptions.GetServer();

            CurrThroughputGraph = new YGraph();
            uiLatencyGraphPanel.Items.Insert(0, CurrThroughputGraph);
            CurrThroughputGraph.UpdateTitle("Upload");

            var stats = new Statistics(new double[] { 0.0 });
            stats.TestType = "Upload";
            CurrThroughputGraph.CurrStatistics = stats;
            var speed = new Statistics.AdditionalInfo("Throughput", "0.0");
            stats.PreAdditionalInfo.Add(speed);
            var nbytes = new Statistics.AdditionalInfo("Bytes", "0.0");
            stats.PreAdditionalInfo.Add(nbytes);
            var time = new Statistics.AdditionalInfo("Time (s)", "0.0");
            stats.PreAdditionalInfo.Add(time);

            FccSpeedTest2022.AddAdditionalStatsPost(stats, CurrentUsefulNetworkInfo, serverName, null);

            ShowProgressRing?.StartProgressIndeterminate();
            var result = new FccSpeedTest2022.ThroughputTestResult();
            var task = SpeedTest.UploadTest(result, serverName);
            var startTime = DateTimeOffset.UtcNow;
            bool overTime = false;
            while (!task.IsCompleted && !overTime)
            {
                await Task.Delay(250);
                //uiLogging.Text += $"{result}\n";
                CurrThroughputGraph.AddValue(result.SnapshotSpeedInMbpsRounded);
                speed.Value = result.SnapshotSpeedInMbpsRounded.ToString() + " Mbps";
                nbytes.Value = AsMBytes(result.SnapshotTransferInBytes);
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
            nbytes.Value = AsMBytes(result.NBytes);
            time.Value = result.TimeAverageInSeconds.ToString("N1");
            uiLatencyStats.SetStatistics(stats, false); // not full stats
            ShowProgressRing?.StopProgressIndeterminate();

        }

        private static string AsMBytes(double nbytes)
        {
            var retval = (nbytes / (1024 * 1024)).ToString("N2") + " MBytes";
            return retval;
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

        #region COPY
        private string GetAllAsExcel(string filter)
        {
            string html = "";
            var list = uiLatencyGraphPanel.Items;
            foreach (var item in list)
            {
                Statistics stats = null;
                if (item is BoxWhiskerControl bwc && filter.Contains("Latency"))
                {
                    stats = bwc.GetStatistics();
                }
                if (item is YGraph yg && filter.Contains("Throughput"))
                {
                    stats = yg.GetStatistics();
                }
                if (stats == null) continue;
                if (html == "")
                {
                    html = stats.GetDataAsExcelHeader();
                }
                html += stats.GetDataAsExcel();
            }
            return html;
        }

        private void OnCopyAsExcel(object sender, RoutedEventArgs e)
        {
            var filterList = (sender as Button).Tag as string;
            string html = "";
            foreach (var filter in filterList.Split(" "))
            {
                html += GetAllAsExcel(filter);
            }
            html = html.html(); // adds in the <csv><body><table> etc.

            var dp = new DataPackage();
            dp.SetText(html);
            dp.Properties.Title = "Speed Test data";
            Clipboard.SetContent(dp);
        }

        private string GetAllCsv(string filter)
        {
            string csv = "";
            var list = uiLatencyGraphPanel.Items;
            foreach (var item in list)
            {
                Statistics stats = null;
                if (item is BoxWhiskerControl bwc && filter.Contains("Latency"))
                {
                    stats = bwc.GetStatistics();
                }
                if (item is YGraph yg && filter.Contains("Throughput"))
                {
                    stats = yg.GetStatistics();
                }
                if (stats == null) continue;
                if (csv == "")
                {
                    csv = stats.GetDataCsvHeader();
                }
                csv += stats.GetDataCsv();
            }
            return csv;
        }

        private void OnCopyCsv(object sender, RoutedEventArgs e)
        {
            var filterList = (sender as Button).Tag as string;
            string csv = "";
            foreach (var filter in filterList.Split(" "))
            {
                csv += GetAllCsv(filter);
            }

            var dp = new DataPackage();
            dp.SetText(csv);
            dp.Properties.Title = "Speed Test data";
            Clipboard.SetContent(dp);
        }
        private void OnClearData (object sender, RoutedEventArgs e)
        {
            uiLatencyGraphPanel.Items.Clear();
        }

        #endregion
    }
}

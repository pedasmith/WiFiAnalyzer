using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Networking;

namespace SpeedTests
{
    partial class FccSpeedTest2022
    {
        public class LatencyTestResults
        {
            public LatencyTestResults(HostName server, String port)
            {
                Server = server;
                Port = port;
            }

            public DateTime StartTime { get; } = DateTime.Now;
            public HostName Server { get; set; } = null;
            public string Port { get; set; } = null;
            public int NSent { get; set; } = 0;
            public int NRecv { get; set; } = 0;
            public string Error { get; set; } = null;
            public double[] TimesInMillisecondsSorted { get; internal set; }
            public Statistics SpeedStatistics { get; internal set; }

            internal void Calculate(LatencyTestSingle[] singles)
            {
                NSent = 0;
                NRecv = 0;
                List<double> rawTimesInMilliseconds = new List<double>();
                for (int i = 0; i < singles.Length; i++)
                {
                    var s = singles[i];
                    if (s == null) continue;
                    NSent++;
                    if (s.HaveEndTime && !s.EndTimeIsTooLate)
                    {
                        NRecv++;
                        rawTimesInMilliseconds.Add(s.TimeInSeconds * 1000.0);
                    }
                }
                double PacketLossPercent = 0.00; // will be 0 to 100
                if (NRecv == 0)
                {
                    TimesInMillisecondsSorted = new double[1];
                    TimesInMillisecondsSorted[0] = 100_000.0; // very large
                    PacketLossPercent = 100.00;
                }
                else
                {
                    rawTimesInMilliseconds.Sort();
                    TimesInMillisecondsSorted = rawTimesInMilliseconds.ToArray();
                    PacketLossPercent = 100.00 * ((double)(NSent - NRecv) / (double)NSent);
                }

                var pdv = Rfc3393Calculations.Calculate_IPDV_Section_2_6_InMilliseconds(singles);

                SpeedStatistics = new Statistics(TimesInMillisecondsSorted);
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("Pkt Loss%", PacketLossPercent.ToString("N0") + "%"));
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("PDV To", pdv.PdvAverageToServer.ToString("N3")));
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("PDV From", pdv.PdvAverageFromServer.ToString("N3")));
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("Sent", NSent.ToString()));
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("Recv", NRecv.ToString()));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("Server", Server.DisplayName));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("Port", Port));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("At", StartTime.ToLongTimeString()));
            }
        }

        public class ThroughputTestResultSingleData
        {
            public double TimeInSeconds = 0.0;
            public long NBytes = 0;
            /// <summary>
            /// Basic calculation of Mbps (which what e.g., speedtest.net returns)
            /// </summary>
            public double Mbps
            {
                get
                {
                    if (TimeInSeconds == 0) return 0.0;
                    var retval = NBytes / TimeInSeconds; // bytes per second
                    retval = 8.0 * retval / (1024.0 * 1024.0);
                    return retval;
                }
            }

            /// <summary>
            /// FCC: B = Bytes transferred
            /// </summary>
            public double B
            {
                get
                {
                    return NBytes;
                }
            }
            /// <summary>
            /// FCC: T = Time (seconds) (between start time point and end time point)
            /// </summary>
            public double T
            {
                get
                {
                    return TimeInSeconds;
                }
            }
            /// <summary>
            /// FCC: S = SpeedInBytesPerSecond (Bytes per second)
            /// </summary>
            public double S
            {
                get
                {
                    if (T == 0.0) return 0.0;
                    return B / T;
                }
            }
        }

        public class ThroughputTestResultSingle
        {
            // Basic data

            /// <summary>
            /// URL that the download is from (e.g., http://sp2-bdc-seattle-us.samknows.com/1000MB.bin )
            /// </summary>
            public string TransferUri { get; set; }

            public ThroughputTestResultSingleData Data = new ThroughputTestResultSingleData();
            public ThroughputTestResultSingleData WarmupData = new ThroughputTestResultSingleData();

            // Variables set while the test is running
            public enum Phase { PhaseWarmup, PhaseTesting, PhaseComplete };
            public Phase CurrPhase = Phase.PhaseWarmup;
            public string Error = null;

            /// <summary>
            /// Basic calculation of Mbps (which what e.g., speedtest.net returns)
            /// </summary>
            public double Mbps {  get {  return Data.Mbps; } }

            /// <summary>
            /// FCC: B = Bytes transferred
            /// </summary>
            public double B {  get {  return Data.B; } }
            /// <summary>
            /// FCC: T = Time (seconds) (between start time point and end time point)
            /// </summary>
            public double T {  get {  return Data.T; } }
            /// <summary>
            /// FCC: S = SpeedInBytesPerSecond (Bytes per second)
            /// </summary>
            public double S {  get {  return Data.S; } }
        }

        public class ThroughputTestResult
        {
            public List<ThroughputTestResultSingle> SingleResults { get; } = new List<ThroughputTestResultSingle>();
            public string Error
            {
                get
                {
                    int nerror = 0;
                    string retval = null;
                    foreach (var item in SingleResults)
                    {
                        if (item.Error != null)
                        {
                            nerror += 1;
                            retval = item.Error;
                        }
                    }
                    if (nerror == 0) return null;
                    if (nerror == 1) return retval;
                    return $"N. Error={nerror} {retval}";
                }
            }

            /// <summary>
            /// FCC: SpeedInBytesPerSecond = S1 + S2 + S3 where S1, S2, S3 are the S results from SingleResults
            /// </summary>
            public double SpeedInBytesPerSecond
            {
                get
                {
                    var retval = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.S;
                    }
                    return retval;
                }
            }

            public int SpeedInMbps
            {
                get { return (int)Math.Floor(8.0 * SpeedInBytesPerSecond / (1024 * 1024)); }
            }



            /// <summary>
            /// FCC: SpeedInBytesPerSecond = S1 + S2 + S3 where S1, S2, S3 are the S results from SingleResults
            /// </summary>
            public double WarmupSpeedInBytesPerSecond
            {
                get
                {
                    var retval = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.WarmupData.S;
                    }
                    return retval;
                }
            }

            public int WarmupSpeedInMbps
            {
                get { return (int)Math.Floor(8.0 * WarmupSpeedInBytesPerSecond / (1024 * 1024)); }
            }

            public double CurrSpeedInBytesPerSecond
            {
                get
                {
                    if (SpeedInBytesPerSecond != 0.0) return SpeedInBytesPerSecond;
                    return WarmupSpeedInBytesPerSecond;
                }
            }

            /// <summary>
            /// A "nice" version
            /// </summary>
            public double CurrSpeedInMbpsRounded
            {
                get 
                { 
                    var mbps = 8.0 * CurrSpeedInBytesPerSecond / (1024 * 1024);
                    mbps = RoundOff(mbps);
                    return mbps;
                }
            }

            public static double RoundOff(double value)
            {
                var retval = (Math.Round(value * 10.0) / 10.0);
                return retval;
            }
            public override string ToString()
            {
                return $"Speed Mbps={SpeedInMbps} Bytes/Second={Math.Floor(SpeedInBytesPerSecond)} (Warmup {WarmupSpeedInMbps} and {Math.Floor(WarmupSpeedInBytesPerSecond)}) Error={Error}";
            }
        }
    }
}

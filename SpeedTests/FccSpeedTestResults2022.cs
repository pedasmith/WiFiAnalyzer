using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Convert Bytes/Second into Megabits / second (mega=1024*1024)
        /// </summary>
        /// <param name="speedBytesPerSecond"></param>
        /// <returns></returns>
        private static double CalculateMbps(double speedBytesPerSecond)
        {
            var retval = 8.0 * speedBytesPerSecond / (1024.0 * 1024.0);
            return retval;
        }

        private static double CalculateMbpsNice(double speedBytesPerSecond)
        {
            var retval = CalculateMbps(speedBytesPerSecond);
            retval = Math.Round(retval * 10.0) / 10.0;
            return retval;
        }
        public class ThroughputTestResultSingleData
        {
            public double TimeInSeconds { get; set; } = 0.0;
            public long NBytes = 0;
            /// <summary>
            /// Basic calculation of CalculateMbps (which what e.g., speedtest.net returns)
            /// </summary>
            public double Mbps
            {
                get
                {
                    var retval = CalculateMbps(S);
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
            /// URL that the download or upload is from (e.g., http://sp2-bdc-seattle-us.samknows.com/1000MB.bin )
            /// </summary>
            public string TransferUri { get; set; }

            public ThroughputTestResultSingleData Data = new ThroughputTestResultSingleData();
            public ThroughputTestResultSingleData WarmupData = new ThroughputTestResultSingleData();
            public ThroughputTestResultSingleData Snapshot = new ThroughputTestResultSingleData();

            // Variables set while the test is running
            public enum Phase { PhaseWarmup, PhaseTesting, PhaseComplete };
            public Phase CurrPhase = Phase.PhaseWarmup;
            public string Error = null;

            /// <summary>
            /// Basic calculation of CalculateMbps (which what e.g., speedtest.net returns)
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
                        retval += item.Data.S;
                    }
                    return retval;
                }
            }

            public double TimeAverageInSeconds
            {
                get
                {
                    double retval = 0;
                    double n = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.T;
                        n += 1.0;
                    }
                    return (retval / n);
                }
            }
            public double NBytes
            {
                get
                {
                    var retval = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.Data.NBytes;
                    }
                    return retval;
                }
            }
            public double SnapshotTimeAverageInSeconds
            {
                get
                {
                    double retval = 0;
                    double n = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.Snapshot.T;
                        n += 1.0;
                    }
                    return (retval / n);
                }
            }

            /// <summary>
            /// FCC: SpeedInBytesPerSecond = S1 + S2 + S3 where S1, S2, S3 are the S results from SingleResults
            /// </summary>
            public double SnapshotSpeedInBytesPerSecond
            {
                get
                {
                    var retval = 0.0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.Snapshot.S;
                    }
                    return retval;
                }
            }

            public long SnapshotTransferInBytes
            {
                get
                {
                    long retval = 0;
                    foreach (var item in SingleResults)
                    {
                        retval += item.Snapshot.NBytes;
                    }
                    return retval;
                }
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




            /// <summary>
            /// A "nice" version used only for the graph
            /// </summary>
            public double SpeedInMbpsRounded
            {
                get { return CalculateMbpsNice(SpeedInBytesPerSecond); }
            }
            /// <summary>
            /// A "nice" version used only for the graph
            /// </summary>
            public double SnapshotSpeedInMbpsRounded
            {
                get { return CalculateMbpsNice(SnapshotSpeedInBytesPerSecond); }
            }

            public override string ToString()
            {
                return $"Speed CalculateMbps={CalculateMbpsNice(SpeedInBytesPerSecond)} (Warmup {CalculateMbpsNice(WarmupSpeedInBytesPerSecond)}) Error={Error}";
            }
        }
    }
}

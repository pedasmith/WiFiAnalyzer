using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace SpeedTests
{
    public class FccSpeedTest2022
    {
        public List<string> Servers { get; } = new List<string>
        {
            "sp2-bdc-seattle-us.samknows.com"
        };

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
                for (int i=0; i<singles.Length; i++)
                {
                    var s = singles[i];
                    if (s == null) continue; // 
                    NSent++;
                    if (s.EndTime != null && s.TimeInSeconds >= 0 && s.TimeInSeconds <= 1_000_000)
                    {
                        NRecv++;
                        rawTimesInMilliseconds.Add(s.TimeInSeconds * 1000.0);
                    }
                }
                if (NRecv == 0)
                {
                    TimesInMillisecondsSorted = new double[1];
                    TimesInMillisecondsSorted[0] = 100_000.0; // very large
                }
                else
                {
                    rawTimesInMilliseconds.Sort();
                    TimesInMillisecondsSorted = rawTimesInMilliseconds.ToArray();
                }
                SpeedStatistics = new Statistics(TimesInMillisecondsSorted);
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("Sent", NSent.ToString()));
                SpeedStatistics.PreAdditionalInfo.Add(new Statistics.AdditionalInfo("Recv", NRecv.ToString()));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("Server", Server.DisplayName));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("Port", Port));
                SpeedStatistics.PostAdditionalInfo.Add(new Statistics.AdditionalInfo("At", StartTime.ToLongTimeString()));
            }
        }

        internal class LatencyTestSingle
        {
            public LatencyTestSingle(int id)
            {
                Id = id;
                SetStartTime();
            }
            public override string ToString()
            {
                return $"{Id}: time={TimeInSeconds}";
            }
            private void SetStartTime()
            {
                StartTime = DateTimeOffset.Now;
                Packet = CreateUdpPacket(Id, StartTime);
            }
            public void SetEndTime(long s, long ms)
            {
                EndTime = DateTimeOffset.Now;
                var stunix = DateTimeOffset.FromUnixTimeMilliseconds(s * 1000 + ms);
                ServerTime = stunix.ToOffset(EndTime.Offset);
            }
            public int Id { get; set; }
            public DateTimeOffset StartTime { get; internal set; }
            public DateTimeOffset EndTime { get; internal set; }
            public DateTimeOffset ServerTime { get; internal set; }
            public double TimeInSeconds { get { if (EndTime == null) return -1; return EndTime.Subtract(StartTime).TotalSeconds; } }
            public double ToServerInSeconds { get { if (ServerTime == null) return -1; return ServerTime.Subtract(StartTime).TotalSeconds; } }
            public double FromServerInSeconds { get { if (ServerTime == null || EndTime== null) return -1; return EndTime.Subtract(ServerTime).TotalSeconds; } }
            public byte[] Packet;
            private static byte[] CreateUdpPacket(int sequence, DateTimeOffset timestamp)
            {
                int CLIENTTOSERVERMAGIC = 0x00009000;
                long unixMilliseconds = timestamp.ToUnixTimeMilliseconds();
                int seconds = (int)((unixMilliseconds / 1000) & 0xFFFFFFFF);
                int milliseconds = (int)(unixMilliseconds % 1000);
                var packet = new byte[16];

                WriteInt32(packet,  0, sequence);
                WriteInt32(packet,  4, seconds);
                WriteInt32(packet,  8, milliseconds);
                WriteInt32(packet, 12, CLIENTTOSERVERMAGIC);
                return packet;
            }
            private static void WriteInt32(byte[] packet, int index, int value)
            {
                packet[index + 0] = (byte)((value >> 24) & 0xFF);
                packet[index + 1] = (byte)((value >> 16) & 0xFF);
                packet[index + 2] = (byte)((value >>  8) & 0xFF);
                packet[index + 3] = (byte)((value >>  0) & 0xFF);
            }
        }
        LatencyTestSingle[] IndividualTests = null;
        private LatencyTestResults LTR { get; set; } = null;
        /// <summary>
        /// UDP Latency tests; thanks to https://github.com/SamKnows/skandroid-core/blob/b65cc014a8d64da86bd4471d0d33f5c45e55aead/desktop/skConsoleSpeedTest/src/com/samknows/tests/LatencyTest.java
        /// </summary>

        /// <returns></returns>
        public async Task<LatencyTestResults> LatencyTestAsync(List<ISetStatistics> uxlist, HostName server, string port = "6000", double interPacketTimeInMilliseconds = 500, double delayTimoutInSeconds = 2.0, int nDatagrams = 200, double maxTimeInSeconds = 5.0)
        {
            nDatagrams = 20; // TODO: don't send too many yet!
            if (server == null)
            {
                server = new HostName(Servers[0]);
            }
            IndividualTests = new LatencyTestSingle[nDatagrams];

            var retval = new LatencyTestResults(server, port);
            LTR = retval;

            using (var socket = new DatagramSocket())
            {
                try
                {
                    socket.MessageReceived += Socket_MessageReceived;
                    //await socket.BindServiceNameAsync(port);
                    await socket.ConnectAsync(server, port);
                }
                catch (Exception ex)
                {
                    retval.Error = $"Unable to connect: server={server} port={port} error={ex.Message}";
                    return retval;
                }
                var outputStream = socket.OutputStream;
                var writer = new DataWriter(outputStream);
                uint nbytes = 0;
                for (int i = 0; i < nDatagrams; i++)
                {
                    retval.NSent++;
                    IndividualTests[i] = new LatencyTestSingle(i);
                    var packet = IndividualTests[i].Packet;
                    writer.WriteBytes(packet);
                    var result = await writer.StoreAsync();
                    nbytes += result;

                    if ((i %5) == 4) // every fifth one
                    {
                        retval.Calculate(IndividualTests);
                        foreach (var ux in uxlist)
                        {
                            ux.SetStatistics(retval.SpeedStatistics);
                        }
                        await Task.Delay(250);
                    }
                }
                int loopTimeInMillieconds = 199; // ms
                int timeLeftInMilliseconds = (int)(delayTimoutInSeconds * 1000.0);
                int nloop = 0;
                while (timeLeftInMilliseconds > 0 && LTR.NRecv < LTR.NSent)
                {
                    await Task.Delay(loopTimeInMillieconds);
                    timeLeftInMilliseconds -= loopTimeInMillieconds; // this won't be exact, but that's OK.
                    nloop++;

                    retval.Calculate(IndividualTests);
                    foreach (var ux in uxlist)
                    {
                        ux.SetStatistics(retval.SpeedStatistics);
                    }

                }

                // Final Calculation.
                if (nloop == 0)
                {
                    retval.Calculate(IndividualTests);
                    foreach (var ux in uxlist)
                    {
                        ux.SetStatistics(retval.SpeedStatistics);
                    }
                }
            }
            return retval;
        }

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            LTR.NRecv++;
            var dr = args.GetDataReader();
            int id = dr.ReadInt32();
            int s = dr.ReadInt32();
            int ms = dr.ReadInt32();
            IndividualTests[id].SetEndTime(s, ms);
        }
    }
}

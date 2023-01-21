﻿//#define TESTHOOK_PACKET_LATE

// See the TESTHOOK: spots for testing!
// See ERRATA: for issues with the FCC docs
// See the Asset "4AddingTheLatencyTest.md" for some background information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SpeedTests
{
    // http://sp2-bdc-seattle-us.samknows.com/
    // https://github.com/SamKnows/skandroid-core
    // https://github.com/SamKnows/skandroid-core/blob/b65cc014a8d64da86bd4471d0d33f5c45e55aead/libcore/src/com/samknows/tests/LatencyTest.java
    // https://github.com/SamKnows/skandroid-core/blob/b65cc014a8d64da86bd4471d0d33f5c45e55aead/desktop/skConsoleSpeedTest/src/com/samknows/tests/LatencyTest.java
    // https://github.com/SamKnows/skandroid-core/search?q=UdpDatagram
    // https://speedtest-api.samknows.com/targets?target_set=stackpath-us


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

        public class LatencyTestSingle
        {
            public LatencyTestSingle(int id)
            {
                Id = id;
                StartTime = DateTimeOffset.Now;
                Timer.Start();
               Packet = CreateUdpPacket(Id, StartTime);
            }
            public override string ToString()
            {
                return $"{Id}: time={TimeInSeconds}";
            }

            public static int NEndTime = 0;
            public void SetEndTime(long s, long ms)
            {
                HaveEndTime = true;
                //EndTime = now;
                Timer.Stop();
                var stunix = DateTimeOffset.FromUnixTimeMilliseconds(s * 1000 + ms);
                ServerTime = stunix.ToOffset(StartTime.Offset);

#if TESTHOOK_PACKET_LATE
                // TESTHOOK: Emulate a bad connection for the stats
                if (NEndTime % 2 == 0)
                {
                    EndTime = EndTime.AddSeconds(3.0);
                    EndTimeIsTooLate = true;
                }
#endif
                NEndTime++;

            }
            public int Id { get; set; }
            private Stopwatch Timer = new Stopwatch();
            private DateTimeOffset StartTime { get; set; }
            //public DateTimeOffset EndTime { get; internal set; }
            public bool EndTimeIsTooLate { get; set; } = false;
            public bool HaveEndTime { get; internal set; } = false;
            public DateTimeOffset ServerTime { get; internal set; }
            public double TimeInSeconds { get { return Timer.ElapsedMilliseconds / 1000.0; } }
            public double ToServerInSeconds { get { if (ServerTime == null) return -1; return ServerTime.Subtract(StartTime).TotalSeconds; } }
            public double FromServerInSeconds 
            { 
                get 
                { 
                    if (ServerTime == null) return -1; 
                    var retval = TimeInSeconds - ToServerInSeconds;
                    return retval; 
                } 
            }
            public byte[] Packet;
            private static byte[] CreateUdpPacket(int sequence, DateTimeOffset timestamp)
            {
                int CLIENTTOSERVERMAGIC = 0x00009000;
                long unixMilliseconds = timestamp.ToUnixTimeMilliseconds();
                int seconds = (int)((unixMilliseconds / 1000) & 0xFFFFFFFF);
                int milliseconds = (int)(unixMilliseconds % 1000);
                var packet = new byte[160];
                //ERRATA: FCC Technical description, page 9 says the packets are 160 bytes. The
                // Github code says 16 bytes.
                // 2023-01-08: 16 bytes works perfectly, as does 160 bytes. The return bytes are 16 bytes long.

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


        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            LTR.NRecv++;
            var dr = args.GetDataReader();
            int id = dr.ReadInt32();
            int s = dr.ReadInt32();
            int ms = dr.ReadInt32();
            int serverMagic = dr.ReadInt32();
            IndividualTests[id].SetEndTime(s, ms);
        }

        public async Task<LatencyTestResults> LatencyTestAsync(List<ISetStatistics> uxlist, HostName server, string port = "6000", 
            double interPacketTimeInMilliseconds = 500,
            double delayTimoutInSeconds = 2.0, 
            int maxDatagrams = 200, 
            double maxTimeInSeconds = 5.0)
        {
            // ERRATA: the default port here is "6000" (UWP calls these "services" which is the
            // RFC-compliant way of thinking about them. But the FCC Technical doc says the port 
            // should be 5000.
            // 2023-01-08: Port 6000 (Android code) works perfectly. Port 5000 does not work.

            if (server == null) // TODO: pick server correctly.
            {
                server = new HostName(Servers[0]);
            }
            IndividualTests = new LatencyTestSingle[maxDatagrams];

            var retval = new LatencyTestResults(server, port);
            LTR = retval;

            using (var socket = new DatagramSocket())
            {
                socket.Control.QualityOfService = SocketQualityOfService.LowLatency;
                socket.Control.InboundBufferSizeInBytes = 2 * 1024 * 1024; //huge!
                try
                {
                    socket.MessageReceived += Socket_MessageReceived;
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

                bool keepGoing = true;
                var overallEndTime = DateTimeOffset.UtcNow.AddSeconds(maxTimeInSeconds);
                var uiUpdateTime = DateTimeOffset.UtcNow.AddMilliseconds(250); // Update the UX every ZZZ milliseconds.
                while (keepGoing)
                {
                    int i = retval.NSent; // get an index number; it will be used a lot
                    retval.NSent++;
                    var currTest = new LatencyTestSingle(i);
                    IndividualTests[i] = currTest;
                    writer.WriteBytes(currTest.Packet);
                    var result = await writer.StoreAsync();
                    nbytes += result;

                    // Wait for the reply
                    var packetEndTime = DateTimeOffset.UtcNow.AddSeconds(delayTimoutInSeconds);
                    while (!currTest.HaveEndTime
                        && DateTimeOffset.UtcNow < packetEndTime)
                    {
                        await Task.Delay(5); // NOTE: seems like an OK value? The app seems responsive & uses very little CPU
                        if (DateTimeOffset.UtcNow > uiUpdateTime)
                        {
                            retval.Calculate(IndividualTests);
                            foreach (var ux in uxlist)
                            {
                                ux.SetStatistics(retval.SpeedStatistics);
                            }
                            uiUpdateTime = DateTimeOffset.UtcNow.AddMilliseconds(250);
                        }
                    }
                    if (!currTest.HaveEndTime)
                    {
                        currTest.EndTimeIsTooLate = true;
                    }

                    var pauseTimeInMilliseconds = interPacketTimeInMilliseconds;
                    if (currTest.HaveEndTime)
                    {
                        var packetTime = currTest.TimeInSeconds * 1000.0;
                        pauseTimeInMilliseconds -= packetTime;
                    }
                    else
                    {
                        // In reality, this is always going to be negative.
                        pauseTimeInMilliseconds = interPacketTimeInMilliseconds - (delayTimoutInSeconds * 1_000.0);
                    }
                    if (pauseTimeInMilliseconds > 0)
                    {
                        try
                        {
                            await Task.Delay((int)pauseTimeInMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            Log($"ERROR: Unexpected: the delay is < 0? {pauseTimeInMilliseconds} ex={ex.Message}");
                        }
                    }

                    // TODO: IP: slight difference in results: original code will not allow for
                    // packets that straggle in while we're waiting for a different packet.

                    // Check for end conditions.
                    if (DateTimeOffset.UtcNow > overallEndTime)
                    {
                        keepGoing= false;
                    }
                    if (retval.NSent >= maxDatagrams)
                    {
                        keepGoing = false;
                    }
                };
            } // Close the socket.

            // Final stat calculation and UX update
            retval.Calculate(IndividualTests);
            foreach (var ux in uxlist)
            {
                ux.SetStatistics(retval.SpeedStatistics);
            }

            return retval;
        }

        private static void Log(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
            Console.WriteLine(str); 
        }
    }
}

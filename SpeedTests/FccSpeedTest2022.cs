//#define TESTHOOK_PACKET_LATE  //TODO: this is no longer functional

// See the TESTHOOK: spots for testing!
// See ERRATA: for issues with the FCC docs
// See the Asset "4AddingTheLatencyTest.md" for some background information

using Microsoft.UI.Xaml.CustomAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using static QRCoder.PayloadGenerator;

namespace SpeedTests
{
    // http://sp2-bdc-seattle-us.samknows.com/
    // https://github.com/SamKnows/skandroid-core
    // https://github.com/SamKnows/skandroid-core/blob/b65cc014a8d64da86bd4471d0d33f5c45e55aead/libcore/src/com/samknows/tests/LatencyTest.java
    // https://github.com/SamKnows/skandroid-core/blob/b65cc014a8d64da86bd4471d0d33f5c45e55aead/desktop/skConsoleSpeedTest/src/com/samknows/tests/LatencyTest.java
    // https://github.com/SamKnows/skandroid-core/search?q=UdpDatagram
    // https://speedtest-api.samknows.com/targets?target_set=stackpath-us


    public partial class FccSpeedTest2022
    {
        public List<string> Servers { get; } = new List<string>
        {
            "sp2-bdc-seattle-us.samknows.com"
        };

        public String BinPath { get; } = "/1000MB.bin"; 
        // e.g., http://sp2-bdc-seattle-us.samknows.com/1000MB.bin




        public Task DownloadTest(ThroughputTestResult retval, string server)
        {
            // FCC says to use 3 threads. We use 3 tasks instead.
            retval.SingleResults.Add(new ThroughputTestResultSingle());
            retval.SingleResults.Add(new ThroughputTestResultSingle());
            retval.SingleResults.Add(new ThroughputTestResultSingle());

            // Set up the HttpClient so that it doesn't cache
            var bpf = new HttpBaseProtocolFilter();
            bpf.CacheControl.ReadBehavior = HttpCacheReadBehavior.NoCache;
            bpf.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;
            var hc = new HttpClient(bpf);

            // URI
            if (server == null) server = Servers[0]; 
            var uri = new Uri("http://" + server + BinPath);
            var failUri = new Uri("http://" + server + "/FAIL" + BinPath);

            Task[] tasks = new Task[] {
                DownloadTestSingle(hc, uri, retval.SingleResults[0]),
                DownloadTestSingle(hc, uri, retval.SingleResults[1]),
                DownloadTestSingle(hc, uri, retval.SingleResults[2]),
            };
            return Task.WhenAll(tasks);
        }


        public async Task DownloadTestSingle(HttpClient hc, Uri uri, ThroughputTestResultSingle results)
        {
            const double WarmupTimeInSeconds = 3.0; // "Each test cycle begins with a fixed 3-second warmup, ..."
            const int MaxDownloadInBytes = 1_000 * 1_024 * 1_024; // "Each speed test concludes either after 1,000 MB of payload"
            const double MaxDownloadTimeInSeconds = 5.0; // "... or after a maximum elapsed time period of 5 seconds"

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var task = hc.TrySendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead);

                var response = await task;
                if (response.ResponseMessage == null)
                {
                    // Can happen if we're offline
                    results.Error = $"Unable to connect; reason={response.ExtendedError?.Message ?? "(unable to connect)"}"; // TODO: better error message?
                    return;
                }
                if (response.ResponseMessage.StatusCode != HttpStatusCode.Ok)
                {
                    results.Error = $"Incorrect HTTP response. Expected 200; got {response.ResponseMessage.StatusCode}={response.ResponseMessage.ReasonPhrase}";
                    return;
                }

                var stream = await response.ResponseMessage.Content.ReadAsInputStreamAsync();

                const int BufferCapacity = 1024 * 1024; // 1 meg
                IBuffer buffer = new Windows.Storage.Streams.Buffer(BufferCapacity);

                bool keepGoing = true;
                int pauseTimeInMilliseconds = 5;


                results.CurrPhase = ThroughputTestResultSingle.Phase.PhaseWarmup;
                results.TransferUri = uri.ToString();

                var startTime = DateTimeOffset.UtcNow;
                DateTimeOffset downloadStartTime;
                DateTimeOffset downloadEndTime;
                var totalTimeInSeconds = WarmupTimeInSeconds + MaxDownloadTimeInSeconds;
                DateTimeOffset lastLog = DateTimeOffset.UtcNow;
                long lastLogNBytesSnapshot = 0;

                long totalNBytes = 0;
                results.Data.NBytes = 0;
                results.WarmupData.NBytes = 0;
                while (keepGoing)
                {
                    await Task.Delay(pauseTimeInMilliseconds);
                    Log("DBG: About to read");
                    // Here's the problem: the stream.ReadAsync doesn't fail just because the 
                    // underlying network goes away (e.g., you can shut off your Wi-Fi and the 
                    // stream just pauses. Presumably it will fail when the TCP/IP connection
                    // finally dies, but that might not be for a half hour -- timeouts for 
                    // networking code are a little insane.
                    // Hence the crafted timeout.
                    var timeSoFar = DateTimeOffset.UtcNow.Subtract(startTime).TotalSeconds;
                    var remainingTimeInMilliseconds = 1000.0 * Math.Max(1.0, totalTimeInSeconds - timeSoFar); // Wait at least one second
                    var timeoutTask = Task.Delay((int)remainingTimeInMilliseconds);
                    var readTask = stream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.Partial);
                    Task[] readTaskList = new Task[] { readTask.AsTask(), timeoutTask };
                    var taskResult = await Task.WhenAny(readTaskList);
                    Log("DBG: Read complete");
                    if (taskResult == timeoutTask)
                    {
                        // Ideally there would be some kind of cancel on the read,
                        // but there isn't.
                        Log($"DBG: Download: read timeout");
                        results.Error = $"Read timeout while testing";
                        keepGoing = false;
                    }
                    else
                    {
                        buffer = readTask.GetResults();
                    }
                    if (buffer.Length == 0)
                    {
                        results.Error = $"Read failed while testing";
                        keepGoing = false;
                    }
                    else if (buffer.Length < (buffer.Capacity - 1000))
                    {
                        if (pauseTimeInMilliseconds < 500 && buffer.Length < 250_000)
                        {
                            pauseTimeInMilliseconds += 5;
                        }
                    }
                    else if (buffer.Length >= buffer.Capacity)
                    {
                        if (pauseTimeInMilliseconds >= 5)
                        {
                            pauseTimeInMilliseconds -= 5;
                        }
                    }
                    totalNBytes += (int)buffer.Length; // needed for the snapshot

                    var now = DateTimeOffset.UtcNow;
                    // This is for the graph snapshot + logging
                    var logDeltaTime = now.Subtract(lastLog).TotalSeconds;
                    if (logDeltaTime > 0.25) // update every quarter second
                    {
                        Log($"DBG: Download: pause={pauseTimeInMilliseconds} length={buffer.Length}");

                        results.Snapshot.TimeInSeconds = logDeltaTime;
                        results.Snapshot.NBytes = totalNBytes - lastLogNBytesSnapshot;
                        lastLog = now;
                        lastLogNBytesSnapshot = totalNBytes;
                    }

                    switch (results.CurrPhase)
                    {
                        case ThroughputTestResultSingle.Phase.PhaseWarmup:
                            // Are we done with the warmup?
                            results.WarmupData.TimeInSeconds = now.Subtract(startTime).TotalSeconds;
                            results.WarmupData.NBytes += (int)buffer.Length;

                            if (results.WarmupData.TimeInSeconds >= WarmupTimeInSeconds)
                            {
                                // Ready to start the real testing CurrPhase
                                downloadStartTime = now;
                                results.CurrPhase = ThroughputTestResultSingle.Phase.PhaseTesting;
                            }
                            break;
                        case ThroughputTestResultSingle.Phase.PhaseTesting:
                            results.Data.TimeInSeconds = now.Subtract(downloadStartTime).TotalSeconds;
                            results.Data.NBytes += (int)buffer.Length;

                            if (results.Data.TimeInSeconds >= MaxDownloadTimeInSeconds)
                            {
                                results.CurrPhase = ThroughputTestResultSingle.Phase.PhaseComplete;
                                downloadEndTime = now;
                                Log($"DBG: Timeout after {results.Data.TimeInSeconds} seconds");
                            }
                            if (results.Data.NBytes > MaxDownloadInBytes)
                            {
                                results.CurrPhase = ThroughputTestResultSingle.Phase.PhaseComplete;
                                downloadEndTime = now;
                                Log($"DBG: Maxbytes after {results.Data.NBytes} bytes");
                            }
                            break;
                    }
                    if (results.CurrPhase == ThroughputTestResultSingle.Phase.PhaseComplete)
                    {
                        keepGoing = false;
                    }
                }

                Log($"DBG: S={results.S} CalculateMbps={results.Mbps}");
            }
            catch (Exception e)
            {
                Log($"ERROR: Exception {e.Message} for {uri}");
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
                                ux.SetStatistics(retval.SpeedStatistics, true);
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
                ux.SetStatistics(retval.SpeedStatistics, true);
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

using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedTests
{
    internal class FccSpeedTest2022
    {
        public List<string> Servers { get; } = new List<string>
        {
            "sp2-bdc-seattle-us.samknows.com"
        };

        public void LatencyTest(string server, string port=5000, double interPacketTimeInMilliseconds = 500, double delayTimoutInSeconds = 2.0, int NDatagrams = 200, double mxTimeInSeconds = 5.0)
        {
            var socket = new DatagramSocket();
            socket.Connect(server);

        }
    }
}

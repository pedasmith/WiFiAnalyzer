using System;
using System.Collections.Generic;
using System.Text;

namespace SmartWiFiHelpers
{
    class WiFiBandChannel
    {
        /// <summary>
        /// Input frequencies are all in MHz even through they are stored as KHz to match the underlying values
        /// </summary>
        /// <param name="channelCenterFrequencyInKilohertz"></param>
        /// <param name="bandwidth"></param>
        /// <param name="wifiVersion"></param>
        /// <param name="channelName"></param>
        /// <param name="bandName"></param>
        /// <param name="minOverlappingFrequency"></param>
        /// <param name="maxOverlappingFrequency"></param>
        public WiFiBandChannel(int channelCenterFrequency, int bandwidth, string wifiVersion, string channelName, string bandName, int minOverlappingFrequency, int maxOverlappingFrequency)
        {
            ChannelCenterFrequencyInKilohertz = channelCenterFrequency * 1000;
            BandwidthList.Add(bandwidth * 1000); // if more are needed, they can be added
            WiFiVersion = wifiVersion;
            ChannelName = channelName;
            BandName = bandName;

            MinOverlappingFrequency = minOverlappingFrequency * 1000;
            MaxOverlappingFrequency = maxOverlappingFrequency * 1000;
        }

        public int ChannelCenterFrequencyInKilohertz { get; internal set; } = 0;
        public List<int> BandwidthList { get; } = new List<int>();
        public string WiFiVersion { get; internal set; }
        public string ChannelName { get; internal set; }
        public string BandName { get; internal set; }
        public int MinOverlappingFrequency { get; internal set; }
        public int MaxOverlappingFrequency { get; internal set; }

        public static int Find(List<WiFiBandChannel> list, int frequencyInKilohertz)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ChannelCenterFrequencyInKilohertz == frequencyInKilohertz)
                {
                    return i;
                }
            }
            return -1;
        }

        public static List<WiFiBandChannel> CreateWiFiMapping()
        {
            var retval = new List<WiFiBandChannel>();
            const int BW24 = 22000; // in kilohertz
            const string GH24 = "2.4 GHz";
            const string BGNAX = "802.11b/g/n/ax";
            retval.Add(new WiFiBandChannel(2412, BW24, BGNAX,  "1", GH24, 2401, 2423));
            retval.Add(new WiFiBandChannel(2417, BW24, BGNAX,  "2", GH24, 2406, 2428));
            retval.Add(new WiFiBandChannel(2422, BW24, BGNAX,  "3", GH24, 2411, 2433));
            retval.Add(new WiFiBandChannel(2427, BW24, BGNAX,  "4", GH24, 2416, 2438));
            retval.Add(new WiFiBandChannel(2432, BW24, BGNAX,  "5", GH24, 2421, 2443));
            retval.Add(new WiFiBandChannel(2437, BW24, BGNAX,  "6", GH24, 2426, 2448));
            retval.Add(new WiFiBandChannel(2442, BW24, BGNAX,  "7", GH24, 2431, 2453));
            retval.Add(new WiFiBandChannel(2447, BW24, BGNAX,  "8", GH24, 2436, 2458));
            retval.Add(new WiFiBandChannel(2452, BW24, BGNAX,  "9", GH24, 2441, 2463));
            retval.Add(new WiFiBandChannel(2457, BW24, BGNAX, "10", GH24, 2446, 2468));
            retval.Add(new WiFiBandChannel(2462, BW24, BGNAX, "11", GH24, 2451, 2473));
            retval.Add(new WiFiBandChannel(2467, BW24, BGNAX, "12", GH24, 2456, 2478));
            retval.Add(new WiFiBandChannel(2472, BW24, BGNAX, "13", GH24, 2461, 2483));
            retval.Add(new WiFiBandChannel(2484, BW24, BGNAX, "14", GH24, 2473, 2495));

            const int BW10 = 10000; // in kilohertz
            const int BW20 = 20000; // in kilohertz
            const int BW40 = 40000; // in kilohertz
            const int BW80 = 80000; // in kilohertz
            const int BW160 = 160000; // in kilohertz
            const string GH5 = "5 GHz";
            const string AHJNACAX = "802.11a/h/j/n/ac/ax";
            retval.Add(new WiFiBandChannel(5035, BW10, AHJNACAX, "7", GH5, 5030, 5040));
            retval.Add(new WiFiBandChannel(5045, BW20, AHJNACAX, "8", GH5, 5030, 5050));
            retval.Add(new WiFiBandChannel(5045, BW10, AHJNACAX, "9", GH5, 5040, 5040));

            retval.Add(new WiFiBandChannel(5055, BW10, AHJNACAX, "11", GH5, 5050, 5060));
            retval.Add(new WiFiBandChannel(5060, BW20, AHJNACAX, "12", GH5, 5050, 5070));
            retval.Add(new WiFiBandChannel(5080, BW20, AHJNACAX, "16", GH5, 5070, 5090));

            retval.Add(new WiFiBandChannel(5160, BW20, AHJNACAX, "32", GH5, 5150, 5170));
            retval.Add(new WiFiBandChannel(5170, BW40, AHJNACAX, "34", GH5, 5150, 5190));
            retval.Add(new WiFiBandChannel(5180, BW20, AHJNACAX, "36", GH5, 5170, 5190));
            retval.Add(new WiFiBandChannel(5190, BW40, AHJNACAX, "38", GH5, 5170, 5210));
            retval.Add(new WiFiBandChannel(5200, BW20, AHJNACAX, "40", GH5, 5190, 5210));

            retval.Add(new WiFiBandChannel(5210, BW80, AHJNACAX, "42", GH5, 5170, 5250));

            retval.Add(new WiFiBandChannel(5220, BW20, AHJNACAX, "44", GH5, 5210, 5230));
            retval.Add(new WiFiBandChannel(5230, BW40, AHJNACAX, "46", GH5, 5210, 5250));
            retval.Add(new WiFiBandChannel(5240, BW20, AHJNACAX, "48", GH5, 5230, 5250));

            return retval;
        }
    }
}

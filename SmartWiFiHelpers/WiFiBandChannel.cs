using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.UI.WebUI;
using Windows.UI.Xaml.Controls;

namespace SmartWiFiHelpers
{
    class WiFiBandChannel
    {
        /// <summary>
        /// Input frequencies are all in MHz even through they are stored as KHz to match the underlying values
        /// </summary>
        /// <param name="channelCenterFrequencyInKilohertz"></param>
        /// <param name="bandwidthInKilohertz"></param>
        /// <param name="wifiVersion"></param>
        /// <param name="channelName"></param>
        /// <param name="bandName"></param>
        /// <param name="minOverlappingFrequencyInMegahertz"></param>
        /// <param name="maxOverlappingFrequencyInMegahertz"></param>
        public WiFiBandChannel(int channelCenterFrequencyInMegahertz, int bandwidthInKilohertz, string wifiVersion, string channelName, string bandName, int minOverlappingFrequencyInMegahertz, int maxOverlappingFrequencyInMegahertz)
        {
            ChannelCenterFrequencyInKilohertz = channelCenterFrequencyInMegahertz * 1000;
            BandwidthInKilohertzList.Add(bandwidthInKilohertz); // if more are needed, they can be added
            WiFiVersion = wifiVersion;
            ChannelName = channelName;
            BandName = bandName;

            MinOverlappingFrequencyInKilohertz = minOverlappingFrequencyInMegahertz*1000;
            MaxOverlappingFrequencyInKilohertz = maxOverlappingFrequencyInMegahertz*1000;
        }

        public int ChannelCenterFrequencyInKilohertz { get; internal set; } = 0;
        public double GetChannelCenterFrequencyInGigahertz() { return (double)ChannelCenterFrequencyInKilohertz / 1_000_000.0; }
        /// <summary>
        /// Bandwidth is in KHz.
        /// </summary>
        public List<int> BandwidthInKilohertzList { get; } = new List<int>();
        public string WiFiVersion { get; internal set; }
        public string ChannelName { get; internal set; }
        public string BandName { get; internal set; }
        public int MinOverlappingFrequencyInKilohertz { get; internal set; }
        public int MaxOverlappingFrequencyInKilohertz { get; internal set; }

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
        /// <summary>
        /// Returns a list of indexes into the list of frequencies that overlap the given WiFiBandChannel.
        /// This is used to make the "these freuencies are used" chart.
        /// </summary>
        public static List<int> FindOverlapping(List<WiFiBandChannel> list, WiFiBandChannel value)
        {
            var retval = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (value.MaxOverlappingFrequencyInKilohertz >= list[i].ChannelCenterFrequencyInKilohertz
                    && value.MinOverlappingFrequencyInKilohertz <= list[i].ChannelCenterFrequencyInKilohertz)
                {
                    retval.Add(i);
                }
            }
            return retval;
        }

        public static int TestFindOverlapping()
        {
            int nerror = 0;
            var wbcList = WiFiBandChannel.StaticWifiBandList;
            var freq = 2_412_000;
            var wbcIndex = WiFiBandChannel.Find(wbcList, freq);
            if (wbcIndex < 0)
            {
                Debug.WriteLine($"ERROR: WiFiBandChannel: FindOverlapping({freq}) was not found.");
                nerror++;
            }
            else
            {
                var expected = new List<string>(){ "1", "2", "3", };
                var actual = FindOverlapping(WiFiBandChannel.StaticWifiBandList, wbcList[wbcIndex]);
                if (expected.Count != actual.Count)
                {
                    Debug.WriteLine($"ERROR: WiFiBandChannel: FindOverlapping({freq}) return count expected={expected.Count} actual={actual.Count}");
                    nerror++;
                }
                else
                {
                    foreach (var itemIndex in actual)
                    {
                        var item = wbcList[itemIndex];
                        var expectedIndex = expected.IndexOf(item.ChannelName);
                        if (expectedIndex < 0)
                        {
                            Debug.WriteLine($"ERROR: WiFiBandChannel: FindOverlapping({freq}) returned {item.BandName} but it's not in the expected list");
                            nerror++;
                        }
                    }

                }
            }
            return nerror;
        }
        private static List<WiFiBandChannel> _StaticWifiBandList = CreateWiFiMapping();
        public static List<WiFiBandChannel> StaticWifiBandList {  get { return _StaticWifiBandList; } }

        // Data from https://en.wikipedia.org/wiki/List_of_WLAN_channels
        private static List<WiFiBandChannel> CreateWiFiMapping()
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

            const int BW10 = 10; // in megahertz
            const int BW20 = 20; // in megahertz
            const int BW40 = 40; // in megahertz
            const int BW80 = 80; // in megahertz
            const int BW160 = 160; // in megahertz

            retval.Add(Create5GhChannel(  "7", 5035, BW10));
            retval.Add(Create5GhChannel(  "8", 5040, BW20));
            retval.Add(Create5GhChannel(  "9", 5035, BW10));
            retval.Add(Create5GhChannel( "11", 5055, BW10));
            retval.Add(Create5GhChannel( "12", 5060, BW20));
            retval.Add(Create5GhChannel( "16", 5080, BW20));

            retval.Add(Create5GhChannel( "32", 5160, BW20));
            retval.Add(Create5GhChannel( "34", 5170, BW40));
            retval.Add(Create5GhChannel( "36", 5180, BW20));
            retval.Add(Create5GhChannel( "38", 5190, BW40));
            retval.Add(Create5GhChannel( "40", 5200, BW20));
            retval.Add(Create5GhChannel( "42", 5210, BW80));
            retval.Add(Create5GhChannel( "44", 5220, BW20));
            retval.Add(Create5GhChannel( "46", 5230, BW40));
            retval.Add(Create5GhChannel( "48", 5240, BW20));
            retval.Add(Create5GhChannel( "50", 5250, BW160));
            retval.Add(Create5GhChannel( "52", 5260, BW20));
            retval.Add(Create5GhChannel( "54", 5270, BW40));
            retval.Add(Create5GhChannel( "56", 5280, BW20));
            retval.Add(Create5GhChannel( "58", 5290, BW80));
            retval.Add(Create5GhChannel( "60", 5300, BW20));
            retval.Add(Create5GhChannel( "62", 5310, BW40));
            retval.Add(Create5GhChannel( "64", 5320, BW20));
            retval.Add(Create5GhChannel( "68", 5340, BW20));
            retval.Add(Create5GhChannel( "96", 5480, BW20));
            retval.Add(Create5GhChannel("100", 5500, BW20));
            retval.Add(Create5GhChannel("102", 5510, BW40));
            retval.Add(Create5GhChannel("104", 5520, BW20));
            retval.Add(Create5GhChannel("106", 5530, BW80));
            retval.Add(Create5GhChannel("108", 5540, BW20));
            retval.Add(Create5GhChannel("110", 5550, BW40));
            retval.Add(Create5GhChannel("112", 5560, BW20));
            retval.Add(Create5GhChannel("114", 5570, BW160));
            retval.Add(Create5GhChannel("116", 5580, BW20));
            retval.Add(Create5GhChannel("118", 5090, BW40));
            retval.Add(Create5GhChannel("120", 5600, BW20));
            retval.Add(Create5GhChannel("122", 5610, BW80));
            retval.Add(Create5GhChannel("124", 5620, BW20));
            retval.Add(Create5GhChannel("126", 5630, BW40));
            retval.Add(Create5GhChannel("128", 5640, BW20));
            retval.Add(Create5GhChannel("132", 5660, BW20));
            retval.Add(Create5GhChannel("134", 5670, BW40));
            retval.Add(Create5GhChannel("136", 5680, BW20));
            retval.Add(Create5GhChannel("138", 5690, BW80));
            retval.Add(Create5GhChannel("140", 5700, BW20));
            retval.Add(Create5GhChannel("142", 5710, BW40));
            retval.Add(Create5GhChannel("144", 5720, BW20));
            retval.Add(Create5GhChannel("149", 5745, BW20));
            retval.Add(Create5GhChannel("151", 5755, BW40));
            retval.Add(Create5GhChannel("153", 5765, BW20));
            retval.Add(Create5GhChannel("155", 5675, BW80));
            retval.Add(Create5GhChannel("157", 5785, BW20));
            retval.Add(Create5GhChannel("159", 5795, BW40));
            retval.Add(Create5GhChannel("161", 5805, BW20));
            retval.Add(Create5GhChannel("163", 5815, BW160));
            retval.Add(Create5GhChannel("165", 5825, BW20));
            retval.Add(Create5GhChannel("167", 5835, BW40));
            retval.Add(Create5GhChannel("169", 5845, BW20));
            retval.Add(Create5GhChannel("171", 5855, BW80));
            retval.Add(Create5GhChannel("173", 5865, BW20));
            retval.Add(Create5GhChannel("175", 5875, BW40));
            retval.Add(Create5GhChannel("177", 5885, BW20));
            retval.Add(Create5GhChannel("182", 5910, BW10));
            retval.Add(Create5GhChannel("183", 5915, BW20));
            retval.Add(Create5GhChannel("184", 5920, BW10));
            retval.Add(Create5GhChannel("187", 5935, BW10));
            retval.Add(Create5GhChannel("188", 5940, BW20));
            retval.Add(Create5GhChannel("189", 5945, BW10));
            retval.Add(Create5GhChannel("192", 5960, BW20));
            retval.Add(Create5GhChannel("196", 5980, BW20));


            return retval;
        }

        private static WiFiBandChannel Create5GhChannel(string channelName, int centerFrequencyInMegahertz, int bandwidthInMegahertz)
        {
            const string GH5 = "5 GHz";
            const string AHJNACAX = "802.11a/h/j/n/ac/ax";
            int minFrequencyInMegahertz = (centerFrequencyInMegahertz) - bandwidthInMegahertz / 2;
            int maxFrequencyInMegahertz = (centerFrequencyInMegahertz) + bandwidthInMegahertz / 2;
            return new WiFiBandChannel(centerFrequencyInMegahertz, bandwidthInMegahertz, AHJNACAX, channelName, GH5, minFrequencyInMegahertz, maxFrequencyInMegahertz);
        }
    }
}

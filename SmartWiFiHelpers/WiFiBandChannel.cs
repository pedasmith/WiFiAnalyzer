using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Media.Capture;
using Windows.UI.WebUI;
using Windows.UI.Xaml.Controls;

namespace SmartWiFiHelpers
{
    public class WiFiBandChannel
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

        public override string ToString()
        {
            return $"Channel={ChannelName} Freq={ChannelCenterFrequencyInKilohertz} Band={BandName} ";
        }

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
                // Don't include any equals -- in the 5GHz range, e.g. band 48 is 5240 MHz@20MHz bandwidth, but
                // it doesn't overlap with e.g. band 46 which is 5230
                //if (value.MaxOverlappingFrequencyInKilohertz > list[i].ChannelCenterFrequencyInKilohertz
                //    && value.MinOverlappingFrequencyInKilohertz < list[i].ChannelCenterFrequencyInKilohertz)
                //{
                //    retval.Add(i);
                //}
                bool minOverlap = FrequencyInRange(list[i], value.MinOverlappingFrequencyInKilohertz);
                bool centerOverlap = FrequencyInRange(list[i], value.ChannelCenterFrequencyInKilohertz);
                bool maxOverlap = FrequencyInRange(list[i], value.MaxOverlappingFrequencyInKilohertz);
                bool overlap = minOverlap || maxOverlap || centerOverlap; // because things that just barely touch don't really overlap. But everything should overlap itself.
                if (overlap)
                {
                    retval.Add(i);
                }
            }
            return retval;
        }

        private static bool FrequencyInRange(WiFiBandChannel potentialItem, int freq)
        {
            bool retval = (freq > potentialItem.MinOverlappingFrequencyInKilohertz
                && freq < potentialItem.MaxOverlappingFrequencyInKilohertz);
            return retval;
        }

        private static int TestFindOverlappingOne(int freq, List<string> expected)
        {
            int nerror = 0;
            var wbcList = WiFiBandChannel.StaticWifiBandList;
            var wbcIndex = WiFiBandChannel.Find(wbcList, freq);
            if (wbcIndex < 0)
            {
                Debug.WriteLine($"ERROR: WiFiBandChannel: FindOverlapping({freq}) was not found.");
                nerror++;
            }
            else
            {
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


        public static int TestFindOverlapping()
        {
            int nerror = 0;
            nerror += TestFindOverlappingOne(2_412_000, new List<string>() { "1", "2", "3", "4", "5" });
            nerror += TestFindOverlappingOne(5_240_000, new List<string>() { "48", "46", "42", "50" });
            return nerror;
        }
        private static List<WiFiBandChannel> _StaticWifiBandList = CreateWiFiMapping();
        public static List<WiFiBandChannel> StaticWifiBandList {  get { return _StaticWifiBandList; } }

        // Data from https://en.wikipedia.org/wiki/List_of_WLAN_channels
        private static List<WiFiBandChannel> CreateWiFiMapping()
        {
            var retval = new List<WiFiBandChannel>();
            const int BW24 = 20000; // in kilohertz
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

            const int BW10 = 10_000; // in kilohertz
            const int BW20 = 20_000; // in kilohertz
            const int BW40 = 40_000; // in kilohertz
            const int BW80 = 80_000; // in kilohertz
            const int BW160 = 160_000; // in kilohertz

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
            retval.Add(Create5GhChannel("155", 5775, BW80));
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

            retval.Add(Create6GHzChannel("1", 5955, BW20));
            retval.Add(Create6GHzChannel("3", 5965, BW40));
            retval.Add(Create6GHzChannel("5", 5975, BW20));
            retval.Add(Create6GHzChannel("7", 5985, BW80));
            retval.Add(Create6GHzChannel("9", 5995, BW20));
            retval.Add(Create6GHzChannel("11", 6005, BW40));
            retval.Add(Create6GHzChannel("13", 6015, BW20));
            retval.Add(Create6GHzChannel("15", 6025, BW160));
            retval.Add(Create6GHzChannel("17", 6035, BW20));
            retval.Add(Create6GHzChannel("19", 6045, BW40));
            retval.Add(Create6GHzChannel("21", 6055, BW20));
            retval.Add(Create6GHzChannel("23", 6065, BW80));
            retval.Add(Create6GHzChannel("25", 6075, BW20));
            retval.Add(Create6GHzChannel("27", 6085, BW40));
            retval.Add(Create6GHzChannel("29", 6095, BW20));
            //retval.Add(Create6GHzCHannel( "31", 6105, BW20));
            retval.Add(Create6GHzChannel("33", 6115, BW20));
            retval.Add(Create6GHzChannel("35", 6125, BW40));
            retval.Add(Create6GHzChannel("37", 6135, BW20));
            retval.Add(Create6GHzChannel("39", 6145, BW80));
            retval.Add(Create6GHzChannel("41", 6155, BW20));
            retval.Add(Create6GHzChannel("43", 6165, BW40));
            retval.Add(Create6GHzChannel("45", 6175, BW20));
            retval.Add(Create6GHzChannel("47", 6185, BW160));
            retval.Add(Create6GHzChannel("49", 6195, BW20));
            retval.Add(Create6GHzChannel("51", 6205, BW40));
            retval.Add(Create6GHzChannel("53", 6215, BW20));
            retval.Add(Create6GHzChannel("55", 6225, BW80));
            retval.Add(Create6GHzChannel("57", 6235, BW20));
            retval.Add(Create6GHzChannel("59", 6245, BW40));
            retval.Add(Create6GHzChannel("61", 6255, BW20));
            //retval.Add(Create6GHzCHannel( "63", 6265, BW20));
            retval.Add(Create6GHzChannel("65", 6275, BW20));
            retval.Add(Create6GHzChannel("67", 6285, BW40));
            retval.Add(Create6GHzChannel("69", 6295, BW20));
            retval.Add(Create6GHzChannel("71", 6305, BW80));
            retval.Add(Create6GHzChannel("73", 6315, BW20));
            retval.Add(Create6GHzChannel("75", 6325, BW40));
            retval.Add(Create6GHzChannel("77", 6335, BW20));
            retval.Add(Create6GHzChannel("79", 6345, BW160));
            retval.Add(Create6GHzChannel("81", 6355, BW20));
            retval.Add(Create6GHzChannel("83", 6365, BW40));
            retval.Add(Create6GHzChannel("85", 6375, BW20));
            retval.Add(Create6GHzChannel("87", 6385, BW80));
            retval.Add(Create6GHzChannel("89", 6395, BW20));
            retval.Add(Create6GHzChannel("91", 6405, BW40));
            retval.Add(Create6GHzChannel("93", 6415, BW20));
            //retval.Add(Create6GHzCHannel( "95", 6425, BW20));
            retval.Add(Create6GHzChannel("97", 6435, BW20));
            retval.Add(Create6GHzChannel("99", 6455, BW40));
            retval.Add(Create6GHzChannel("101", 6455, BW20));
            retval.Add(Create6GHzChannel("103", 6465, BW80));
            retval.Add(Create6GHzChannel("105", 6475, BW20));
            retval.Add(Create6GHzChannel("107", 6485, BW40));
            retval.Add(Create6GHzChannel("109", 6495, BW20));
            retval.Add(Create6GHzChannel("111", 6505, BW160));
            retval.Add(Create6GHzChannel("113", 6515, BW20));
            retval.Add(Create6GHzChannel("115", 6525, BW40));
            retval.Add(Create6GHzChannel("117", 6535, BW20));
            retval.Add(Create6GHzChannel("119", 6545, BW80));
            retval.Add(Create6GHzChannel("121", 6555, BW20));
            retval.Add(Create6GHzChannel("123", 6565, BW40));
            retval.Add(Create6GHzChannel("125", 6575, BW20));
            //retval.Add(Create6GHzCHannel("127", 6585, BW20));
            retval.Add(Create6GHzChannel("129", 6595, BW20));
            retval.Add(Create6GHzChannel("131", 6605, BW40));
            retval.Add(Create6GHzChannel("133", 6615, BW20));
            retval.Add(Create6GHzChannel("135", 6625, BW80));
            retval.Add(Create6GHzChannel("137", 6635, BW20));
            retval.Add(Create6GHzChannel("139", 6645, BW40));
            retval.Add(Create6GHzChannel("141", 6655, BW20));
            retval.Add(Create6GHzChannel("143", 6665, BW160));
            retval.Add(Create6GHzChannel("145", 6675, BW20));
            retval.Add(Create6GHzChannel("147", 6685, BW40));
            retval.Add(Create6GHzChannel("149", 6695, BW20));
            retval.Add(Create6GHzChannel("151", 6705, BW80));
            retval.Add(Create6GHzChannel("153", 6715, BW20));
            retval.Add(Create6GHzChannel("155", 6725, BW40));
            retval.Add(Create6GHzChannel("157", 6735, BW20));
            //retval.Add(Create6GHzCHannel("159", 6745, BW20));
            retval.Add(Create6GHzChannel("161", 6755, BW20));
            retval.Add(Create6GHzChannel("163", 6765, BW40));
            retval.Add(Create6GHzChannel("165", 6775, BW20));
            retval.Add(Create6GHzChannel("167", 6785, BW80));
            retval.Add(Create6GHzChannel("169", 6795, BW20));
            retval.Add(Create6GHzChannel("171", 6805, BW40));
            retval.Add(Create6GHzChannel("173", 6815, BW20));
            retval.Add(Create6GHzChannel("175", 6825, BW160));
            retval.Add(Create6GHzChannel("177", 6835, BW20));
            retval.Add(Create6GHzChannel("179", 6845, BW40));
            retval.Add(Create6GHzChannel("181", 6855, BW20));
            retval.Add(Create6GHzChannel("183", 6865, BW80));
            retval.Add(Create6GHzChannel("185", 6875, BW20));
            retval.Add(Create6GHzChannel("187", 6885, BW40));
            retval.Add(Create6GHzChannel("189", 6895, BW20));
            //retval.Add(Create6GHzCHannel("191", 6905, BW20));
            retval.Add(Create6GHzChannel("193", 6915, BW20));
            retval.Add(Create6GHzChannel("195", 6925, BW40));
            retval.Add(Create6GHzChannel("197", 6935, BW20));
            retval.Add(Create6GHzChannel("199", 6945, BW80));
            retval.Add(Create6GHzChannel("201", 6955, BW20));
            retval.Add(Create6GHzChannel("203", 6965, BW40));
            retval.Add(Create6GHzChannel("205", 6975, BW20));
            retval.Add(Create6GHzChannel("207", 6985, BW160));
            retval.Add(Create6GHzChannel("209", 6995, BW20));
            retval.Add(Create6GHzChannel("211", 7005, BW40));
            retval.Add(Create6GHzChannel("213", 7015, BW20));
            retval.Add(Create6GHzChannel("215", 7025, BW80));
            retval.Add(Create6GHzChannel("217", 7035, BW20));
            retval.Add(Create6GHzChannel("219", 7045, BW40));
            retval.Add(Create6GHzChannel("221", 7055, BW20));
            //retval.Add(Create6GHzCHannel("223", 7065, BW20));
            retval.Add(Create6GHzChannel("225", 7075, BW20));
            retval.Add(Create6GHzChannel("227", 7085, BW40));
            retval.Add(Create6GHzChannel("229", 7095, BW20));
            //retval.Add(Create6GHzCHannel("231", 7105, BW20));
            retval.Add(Create6GHzChannel("233", 7115, BW20));
            return retval;
        }

        private static WiFiBandChannel Create5GhChannel(string channelName, int centerFrequencyInMegahertz, int bandwidthInKilohertz)
        {
            const string GH5 = "5 GHz";
            const string AHJNACAX = "802.11a/h/j/n/ac/ax";
            int minFrequencyInMegahertz = (centerFrequencyInMegahertz) - (bandwidthInKilohertz / 1000) / 2;
            int maxFrequencyInMegahertz = (centerFrequencyInMegahertz) + (bandwidthInKilohertz / 1000) / 2;
            return new WiFiBandChannel(centerFrequencyInMegahertz, bandwidthInKilohertz, AHJNACAX, channelName, GH5, minFrequencyInMegahertz, maxFrequencyInMegahertz);
        }

        private static WiFiBandChannel Create6GHzChannel(string channelName, int centerFrequencyInMegahertz, int bandwidthInKilohertz)
        {
            const string GH6 = "6 GHz";
            const string AHJNACAX = "802.11ax";
            int minFrequencyInMegahertz = (centerFrequencyInMegahertz) - (bandwidthInKilohertz / 1000) / 2;
            int maxFrequencyInMegahertz = (centerFrequencyInMegahertz) + (bandwidthInKilohertz / 1000) / 2;
            return new WiFiBandChannel(centerFrequencyInMegahertz, bandwidthInKilohertz, AHJNACAX, channelName, GH6, minFrequencyInMegahertz, maxFrequencyInMegahertz);
        }
    }
}

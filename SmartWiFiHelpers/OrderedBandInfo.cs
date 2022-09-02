using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Bluetooth.Background;

namespace SmartWiFiHelpers
{
    /// <summary>
    /// Central class for a list of frequencies where each frequency is entered once, and there are lists of WifiNetworkInformations that
    /// are either centered on the frequency or are in range of the frequency (via their overlap)
    /// </summary>
    public class BandUsageInfo
    {
        public double FrequencyInGigahertz { get; set; }
        public WiFiBandChannel WBC { get; set; }
        public List<WiFiNetworkInformation> InfoExactFrequency = new List<WiFiNetworkInformation>();
        public List<WiFiNetworkInformation> InfoOverlapFrequency = new List<WiFiNetworkInformation>();
        public List<WiFiNetworkInformation> InfoAllFrequency = new List<WiFiNetworkInformation>();
        public void AddWifiNetworkInformation(WiFiNetworkInformation value)
        {
            var list = (value.Frequency == FrequencyInGigahertz) ? InfoExactFrequency : InfoOverlapFrequency;
            list.Add(value);
            InfoAllFrequency.Add(value);
        }
    }

    public class OrderedBandList
    {
        public SortedList<double, BandUsageInfo> OrderedBands = new SortedList<double, BandUsageInfo>();
        private BandUsageInfo GetOrCreate(double frequencyInGigahertz)
        {
            if (OrderedBands.ContainsKey(frequencyInGigahertz))
            {
                return OrderedBands[frequencyInGigahertz];
            }
            else
            {
                int frequencyInKilohertz = (int)Math.Round(frequencyInGigahertz * 1_000_000);
                var wbcList = WiFiBandChannel.StaticWifiBandList;
                var wbcIndex = WiFiBandChannel.Find(wbcList, frequencyInKilohertz);
                var wbc = wbcIndex >= 0 ? wbcList[wbcIndex] : null;
                var obi = new BandUsageInfo() { FrequencyInGigahertz = frequencyInGigahertz, WBC = wbc };
                OrderedBands.Add(frequencyInGigahertz, obi);
                return obi;
            }
        }
        public OrderedBandList(IList<WiFiNetworkInformation> list)
        {
            var wbcList = WiFiBandChannel.StaticWifiBandList;
            foreach (var item in list)
            {
                var wbcIndex = WiFiBandChannel.Find(wbcList, item.GetFrequenceInKilohertz());
                if (wbcIndex < 0) continue; // some items don't have a frequency.
                var overlappingList = WiFiBandChannel.FindOverlapping(wbcList, wbcList[wbcIndex]);
                foreach (var overlap in overlappingList)
                {
                    // Each of these
                    var wbc = wbcList[overlap];
                    var obi = GetOrCreate(wbc.GetChannelCenterFrequencyInGigahertz());
                    obi.AddWifiNetworkInformation(item);
                }
            }
        }
    }
}

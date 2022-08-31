﻿using System;
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
    public class OrderedBandInfo
    {
        public double FrequencyInGigahertz { get; set; }
        public List<WifiNetworkInformation> InfoExactFrequency = new List<WifiNetworkInformation>();
        public List<WifiNetworkInformation> InfoOverlapFrequency = new List<WifiNetworkInformation>();
        public void AddWifiNetworkInformation(WifiNetworkInformation value)
        {
            var list = (value.Frequency == FrequencyInGigahertz) ? InfoExactFrequency : InfoOverlapFrequency;
            list.Add(value);
        }
    }

    public class OrderedBandList
    {
        public SortedList<double, OrderedBandInfo> OrderedBands = new SortedList<double, OrderedBandInfo>();
        private OrderedBandInfo GetOrCreate(double frequency)
        {
            if (OrderedBands.ContainsKey(frequency))
            {
                return OrderedBands[frequency];
            }
            else
            {
                var obi = new OrderedBandInfo() { FrequencyInGigahertz = frequency };
                OrderedBands.Add(frequency, obi);
                return obi;
            }
        }
        public OrderedBandList(IList<WifiNetworkInformation> list)
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

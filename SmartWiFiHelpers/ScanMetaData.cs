using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Geolocation;

namespace SmartWiFiHelpers
{
    public class ScanMetadata
    {
        public DateTimeOffset ScanTime { get; set; } = DateTimeOffset.Now;
        public Geoposition Position { get; set; }
    }

}

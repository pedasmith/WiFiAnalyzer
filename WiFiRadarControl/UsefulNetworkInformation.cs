using System;
using System.Collections.Generic;
using System.Text;

namespace WiFiRadarControl
{
    /// <summary>
    /// A little bit of useful network information
    /// </summary>
    public class UsefulNetworkInformation
    {
        public UsefulNetworkInformation()
        {
            WlanSsid = "(no SSID)";
            WlanFrequencyInKilohertz = 0;
        }
        /// <summary>
        /// Often set in the UpdateConnectionInfo method
        /// </summary>
        public string WlanSsid { get; set; } = null;
        public string WlanSsidUser 
        { 
            get 
            { 
                var retval = String.IsNullOrWhiteSpace(WlanSsid) ? "(not set)" : WlanSsid;
                return retval;
            } 
        }
        /// <summary>
        /// Often set in DoRadarScanAsync based on CurrentProbableBssid set in UpdateNetworkInfo
        /// </summary>
        public int WlanFrequencyInKilohertz { get; set; }
        public string WlanFrequencyUser {  get
            {
                double f = ((double)WlanFrequencyInKilohertz) / 1_000_000.0;
                return f.ToString("N3"); // return e.g., 
            } 
        }
    }
}

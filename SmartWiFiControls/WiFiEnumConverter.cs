using System;
using System.Collections.Generic;
using System.Text;
using Windows.Networking.NetworkOperators;

namespace SmartWiFiControls
{
    public static class WiFiEnumConverter
    {
        public static TetheringWiFiAuthenticationKind AuthFromString(string value)
        {
            switch (value)
            {
                case "WPA2": return TetheringWiFiAuthenticationKind.Wpa2;
                case "WPA3+2": return TetheringWiFiAuthenticationKind.Wpa3TransitionMode;
                case "WPA3": return TetheringWiFiAuthenticationKind.Wpa3;
            }
            return TetheringWiFiAuthenticationKind.Wpa2;
        }


        public static TetheringWiFiBand BandFromString(string value)
        {
            switch (value)
            {
                case "2.4": return TetheringWiFiBand.TwoPointFourGigahertz;
                case "5": return TetheringWiFiBand.FiveGigahertz;
                case "6": return TetheringWiFiBand.SixGigahertz;
                case "Auto": return TetheringWiFiBand.Auto;
            }
            return TetheringWiFiBand.TwoPointFourGigahertz;
        }

        public static TetheringWiFiPerformancePriority PriorityFromString(string value)
        {
            switch (value)
            {
                case "default": return TetheringWiFiPerformancePriority.Default;
                case "tethering": return TetheringWiFiPerformancePriority.TetheringOverStation;
            }
            return TetheringWiFiPerformancePriority.TetheringOverStation;
        }
    }
}

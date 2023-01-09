using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.WiFiDirect;

namespace SpeedTests
{
    public static class Rfc3393Calculations
    {
        public class PdvResults
        {
            public double PdvAverageToServer { get; set; } = 0.0;
            public double PdvAverageFromServer { get; set; } = 0.0;
        }
        // https://www.rfc-editor.org/rfc/rfc2679
        // https://www.rfc-editor.org/rfc/rfc3393#page-16
        // See also: https://www.rfc-editor.org/rfc/rfc7679#page-19
        // TODO: there's a bunch of actual values to calculate which this doesn't, returning
        // instead of mean of the jitter ignoring dropped packets.
        public static PdvResults Calculate_IPDV_Section_2_6_InMilliseconds(FccSpeedTest2022.LatencyTestSingle[] values)
        {
            PdvResults retval = new PdvResults();
            int npairs = 0;
            for (int i=0; i<values.Length-1; i+= 2)
            {
                // Take pairs of values. Per section 2.6, Methodologies,
                // both packets have to be OK to calculate the jitter variance.
                // This code doesn't try to "chain" the values together; AFAICT that's 
                // what the RFC is saying in section 2.6
                var p1 = values[i];
                var p2 = values[i + 1];
                if (p1 == null || !p1.HaveEndTime || p1.EndTimeIsTooLate || p2 == null || !p2.HaveEndTime || p2.EndTimeIsTooLate)
                {
                    continue; // both have to be perfect.
                }
                var deltaT = 1000.0 * Math.Abs(p1.ToServerInSeconds - p2.ToServerInSeconds);
                retval.PdvAverageToServer += deltaT;
                deltaT = 1000.0 * Math.Abs(p1.FromServerInSeconds - p2.FromServerInSeconds);
                retval.PdvAverageFromServer += deltaT;
                npairs++;
            }
            retval.PdvAverageToServer = retval.PdvAverageToServer / (double)npairs;
            retval.PdvAverageFromServer = retval.PdvAverageFromServer / (double)npairs;
            return retval;
        }
    }
}

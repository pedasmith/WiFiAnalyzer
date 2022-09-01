using System;
using System.Collections.Generic;
using System.Text;

namespace SmartWiFiHelpers
{
    internal class MathLogisticFunctions
    {
        public double Min, Max;
        public double Range { get { return Max - Min; } }
        public double Lower, Upper;
        public double Scale { get { return Upper - Lower; } }

        public MathLogisticFunctions(double min, double max, double lower, double upper)
        {
            Min = min;
            Max = max;
            Lower = lower;
            Upper = upper;
        }

        public static MathLogisticFunctions CreateAmbientNoiseBarSize()
        {
            return new MathLogisticFunctions(1, 5, -75, -45);
        }
        public double Calculate(double value)
        {
            var center = value - ((Upper + Lower) / 2.0);
            var scale = (center * 4.0 / Scale);
            var fnc = 1.0 / (1.0 + Math.Exp(-scale));
            var retval = (fnc * Range) + Min;
            return retval;
        }

        public static void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }



        public static void Demonstrate()
        {
            Log("Demonstrate the Logistic functions");
            Log("===========================");

            var lf = new MathLogisticFunctions(1, 5, -75, -45); // Actual Wi-Fi range is -80 to -40. Fiddle so that they make nice outputs.
            for (double d = -90.0; d < -20.0; d += 10.0)
            {
                var l = lf.Calculate(d);
                Log($"    {d}={l}");
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedTests
{
    public static class MathMinMax
    {
        public static (double min, double max) MinMax(double[] values, double defaultMin = double.MaxValue, double defaultMax = double.MinValue)
        {
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;
            if (values.Length == 0) return (defaultMin, defaultMax);
            for (int i = 0; i < values.Length; i++)
            {
                minVal = Math.Min(minVal, values[i]);
                maxVal = Math.Max(maxVal, values[i]);
            }
            return (minVal, maxVal);
        }
        public static (double min, double max) MinMax(List<double> values, double defaultMin = double.MaxValue, double defaultMax = double.MinValue)
        {
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;
            if (values.Count == 0) return (defaultMin, defaultMax);
            for (int i = 0; i < values.Count; i++)
            {
                minVal = Math.Min(minVal, values[i]);
                maxVal = Math.Max(maxVal, values[i]);
            }
            return (minVal, maxVal);
        }

        /// <summary>
        /// Given a raw value (e.g., '5.3') and an array of 'nice' value (e.g., [1, 2, 5, 7.5, 10, 15, 20, 25, 50]),
        /// return the first 'nice' value which is >= larger rawValue
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="niceValues"></param>
        /// <returns></returns>
        public static double NiceValue(double rawValue, double[] niceValues)
        {
            foreach (double value in niceValues) { 
                if (value >= rawValue) return value;
            }
            return rawValue;
        }

        /// <summary>
        /// Given a raw value between valueMin and valueMax, rescale it to 
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="valueMin"></param>
        /// <param name="valueMax"></param>
        /// <returns></returns>
        public static double Rescale (double rawValue, double valueMin, double valueMax, double scaleMin, double scaleMax)
        {
            var ratio = (rawValue - valueMin) / (valueMax - valueMin);
            var scale = (ratio * (scaleMax - scaleMin)) + scaleMin;
            return scale;
        }
    }
}

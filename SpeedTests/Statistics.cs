﻿using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedTests
{
    public class Statistics
    {
        public override string ToString()
        {
            return $"N={N} Average={Average} Median={Median} Range={Range}";
        }
        public string TestType { get; set; }
        public double Average { get; internal set; }
        public double IqrLow { get; internal set; }
        public double IqrHigh { get; internal set; }
        public double Max { get; internal set; }
        public double Median { get; internal set; }
        public double Min { get; internal set; }
        public int N { get; internal set; }
        public double Range { get; internal set; }
        public double StdDev {  get; internal set; }
        public double Total { get; internal set; }  
        public double TotalSquared { get; internal set; }

        public class AdditionalInfo
        {
            public AdditionalInfo(string name, string value)
            {
                Name = name;
                Value = value ?? "";
            }

            public string Name { get; set; }
            public string Value { get; set; }
        }
        public List<AdditionalInfo> PreAdditionalInfo { get; } = new List<AdditionalInfo>();
        public List<AdditionalInfo> PostAdditionalInfo { get; } = new List<AdditionalInfo>();

        public string GetDataAsExcel()
        {
            string retval = TestType.td();
            foreach (var item in PreAdditionalInfo)
            {
                retval += item.Value.Replace(" Mbps", "").Replace(" MBytes", "").td();
            }
            var format = "N3";

            if (TestType != "Download" && TestType != "Upload")
            {
                retval += this.Max.ToString(format).td();
                retval += this.IqrHigh.ToString(format).td();
                retval += this.Median.ToString(format).td();
                retval += this.Average.ToString(format).td();
                retval += this.IqrLow.ToString(format).td();
                retval += this.Min.ToString(format).td();
                retval += this.Range.ToString(format).td();
                retval += this.StdDev.ToString(format).td();
            }


            foreach (var item in PostAdditionalInfo)
            {
                retval += item.Value.td();
            }
            retval = retval.tr();
            return retval;
        }
        public string GetDataAsExcelHeader()
        {
            string retval = "TestType".th();
            foreach (var item in PreAdditionalInfo)
            {
                retval += item.Name.th();
            }

            if (TestType != "Download" && TestType != "Upload") //NOTE: icky code caused by over-using the Statistics
            {
                retval += "Maximum".th() + "IqrHigh".th() + "Median".th() + "Mean".th() + "IqrLow".th() + "Minimum".th() + "Range".th() + "StdDev".th();
            }
            foreach (var item in PostAdditionalInfo)
            {
                retval += item.Name.th();
            }
            retval = retval.tr();
            return retval;
        }

        public string GetDataCsv()
        {
            string retval = TestType;
            foreach (var item in PreAdditionalInfo)
            {
                retval += "," + item.Value.Replace(" Mbps", "").Replace(" MBytes", "").Replace("%","");
            }
            var format = "N3";

            if (TestType != "Download" && TestType != "Upload")
            {
                retval += "," + this.Max.ToString(format);
                retval += "," + this.IqrHigh.ToString(format);
                retval += "," + this.Median.ToString(format);
                retval += "," + this.Average.ToString(format);
                retval += "," + this.IqrLow.ToString(format);
                retval += "," + this.Min.ToString(format);
                retval += "," + this.Range.ToString(format);
                retval += "," + this.StdDev.ToString(format);
            }


            foreach (var item in PostAdditionalInfo)
            {
                retval += "," + item.Value;
            }
            retval = retval + "\n";
            return retval;
        }
        public string GetDataCsvHeader()
        {
            string retval = "TestType";
            foreach (var item in PreAdditionalInfo)
            {
                retval += ",\"" + item.Name + "\"";
            }

            if (TestType != "Download" && TestType != "Upload") //NOTE: icky code caused by over-using the Statistics
            {
                retval += ",Maximum,IqrHigh,Median,Mean,IqrLow,Minimum,Range,StdDev";
            }
            foreach (var item in PostAdditionalInfo)
            {
                retval += ","+item.Name;
            }
            retval += "\n";
            return retval;
        }

        public Statistics(double[] values)
        {
            Total = 0.0;
            TotalSquared= 0.0;
            for (int i=0; i<values.Length; i++)
            {
                Total += values[i];
                TotalSquared += (values[i] * values[i]);
            }
            N = values.Length;
            if (N == 0)
            {
                Min = 0.0;
                Max = 0.0;
                Average = 0.0;
            }
            else
            {
                Min = values[0];
                Max = values[N - 1];
                Average = Total / N;
            }
            if (N == 0)
            {
                Median = 0.0;
            }
            else if (N % 2 == 0)
            {
                var midpoint = (int)Math.Floor((N-1) / 2.0); // example: N==4, indexes are 0,1,2,3 and I want midpoint to be 1
                var before = values[midpoint];
                var after = values[midpoint + 1];
                Median = (before + after)/2.0;
            }
            else
            {
                var midpoint = (int)Math.Floor(N / 2.0);
                Median = values[midpoint];
            }

            if (N < 2)
            {
                Range = 0.0;
                IqrLow = 0.0;
                IqrHigh = 0.0;
                StdDev = 0.0;
            }
            else
            {
                Range = values[N - 1] - values[0];
                IqrLow = AtRatio(values, 0.25);
                IqrHigh = AtRatio(values, 0.75);
                var variance = (TotalSquared - ((Total * Total) / N)) / (N - 1);
                StdDev = Math.Sqrt(variance);
            }
        }
        private static double At(double[] values, double index)
        {
            int lo = (int)Math.Floor(index);
            int hi = (int)Math.Ceiling(index);
            double ratio = index - (double)lo; // 0 when index=lo, .1 near low .9 near hi...
            double lovalue = values[lo];
            double hivalue = values[hi];
            var retval = (lovalue * (1.0-ratio)) + (hivalue * (ratio));
            return retval;
        }

        /// <summary>
        /// Give 0.0, return index [0]; 1.0 return index [Length-1]; otherwise somewhere between
        /// </summary>
        private static double AtRatio(double[] values, double ratio)
        {
            var index = ratio * ((double)values.Length - 1.0);
            return At(values, index);
        }
        public static int Test()
        {
            int nerrors = 0;
            nerrors += TestAt();
            nerrors += TestAtRatio();
            nerrors += TestStatistics();
            return nerrors;
        }
        private static int TestAt()
        {
            int nerrors = 0;
            double[] a1 = { 11.0, };
            double[] a2 = { 11.0, 22.0, };
            double[] a4 = { 11.0, 22.0, 33.0, 44.0, };
            double[] a5 = { 11.0, 22.0, 33.0, 44.0, 55.0};
            nerrors += TestAtOne(a1, 0.0, 11.0);
            nerrors += TestAtOne(a2, 0.0, 11.0);
            nerrors += TestAtOne(a2, 0.1, 12.1);
            nerrors += TestAtOne(a2, 0.2, 13.2);
            nerrors += TestAtOne(a2, 0.3, 14.3);
            nerrors += TestAtOne(a2, 0.4, 15.4);
            nerrors += TestAtOne(a2, 0.5, 16.5);
            nerrors += TestAtOne(a2, 0.6, 17.6);
            nerrors += TestAtOne(a2, 0.7, 18.7);
            nerrors += TestAtOne(a2, 0.8, 19.8);
            nerrors += TestAtOne(a2, 0.9, 20.9);
            nerrors += TestAtOne(a2, 1.0, 22.0);

            nerrors += TestAtOne(a4, 1.5, 27.5);
            nerrors += TestAtOne(a5, 2, 33.0);

            return nerrors;
        }

        private static int TestAtOne(double[] values, double index, double expected)
        {
            int nerrors = 0;
            double actual = At(values, index);
            var delta = Math.Abs(expected - actual);
            var close = delta < 0.001; // there are no feasable errors that this won't catch.
            if (!close)
            {
                nerrors += 1;
                Log($"Statistics:AtRatio length={values.Length} index={index} actualMean={actual} expectedMean={expected}");
            }
            return nerrors;
        }


        private static int TestAtRatio()
        {
            int nerrors = 0;
            double[] a1 = { 11.0, };
            double[] a2 = { 11.0, 22.0, };
            double[] a4 = { 11.0, 22.0, 33.0, 44.0, };
            double[] a5 = { 11.0, 22.0, 33.0, 44.0, 55.0 };
            nerrors += TestAtRatioOne(a1, 0.0, 11.0);
            nerrors += TestAtRatioOne(a1, 0.5, 11.0);
            nerrors += TestAtRatioOne(a1, 1.0, 11.0);

            nerrors += TestAtRatioOne(a2, 0.0, 11.0);
            nerrors += TestAtRatioOne(a2, 0.5, 16.5);
            nerrors += TestAtRatioOne(a2, 1.0, 22.0);

            nerrors += TestAtRatioOne(a5, 0.0, 11.0);
            nerrors += TestAtRatioOne(a5, 0.25, 22.0);
            nerrors += TestAtRatioOne(a5, 0.375, 27.5);
            nerrors += TestAtRatioOne(a5, 0.5, 33.0);
            nerrors += TestAtRatioOne(a5, 1.0, 55.0);

            return nerrors;
        }

        private static int TestAtRatioOne(double[] values, double index, double expected)
        {
            int nerrors = 0;
            double actual = AtRatio(values, index);
            var delta = Math.Abs(expected - actual);
            var close = delta < 0.001; // there are no feasable errors that this won't catch.
            if (!close)
            {
                nerrors += 1;
                Log($"Statistics:AtRatio length={values.Length} index={index} actualMean={actual} expectedMean={expected}");
            }
            return nerrors;
        }

        private static int TestStatistics()
        {
            int nerrors = 0;
            double[] sample1 = { 4, 5, 6, 7, 8 }; // Calculating Better Decision, p 6-1
            double[] sample2 = { 15.0, 15.2, 15.6, 15.7, 15.8, 15.9, 15.9, 16.1}; // Student Calculator Math, 1976+1980, p 7-15
            nerrors += TestMeanOne(sample1, 6.00); // sample1 only had the population stddev, not the sample stddev
            nerrors += TestStdDevOne(sample2, 0.3741657);

            return nerrors;
        }
        private static int TestMeanOne(double[] values, double expectedMean)
        {
            int nerrors = 0;
            var stats = new Statistics(values);
            double actualMean = stats.Median;
            var delta = Math.Abs(expectedMean - actualMean);
            var close = delta < 0.001; // there are no feasable errors that this won't catch.
            if (!close)
            {
                nerrors += 1;
                Log($"Statistics:Median length={values.Length} actualMean={actualMean} expectedMean={expectedMean}");
            }
            return nerrors;
        }
        private static int TestStdDevOne(double[] values, double expectedStdDev)
        {
            int nerrors = 0;
            var stats = new Statistics(values);
            double actualStdDev = stats.StdDev;
            var delta = Math.Abs(expectedStdDev - actualStdDev);
            var close = delta < 0.001; // there are no feasable errors that this won't catch.
            if (!close)
            {
                nerrors += 1;
                Log($"Statistics:StdDev length={values.Length} actualMean={actualStdDev} expectedMean={expectedStdDev}");
            }
            return nerrors;
        }

        private static void Log(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
            Console.WriteLine(str);
        }
    }
}

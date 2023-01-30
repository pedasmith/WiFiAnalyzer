using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SpeedTests
{
    public sealed partial class BoxWhiskerControl : UserControl, ISetStatistics
    {
        private static double Map(double value, double domainMin, double domainMax, double rangeMin, double rangeMax)
        {
            var ratio = (value - domainMin) / (domainMax - domainMin);
            var retval = (ratio * (rangeMax - rangeMin)) + rangeMin;
            return retval;
        }

        private double[] YMaxDefault = new double[]
        {
            .25, .5, .75,
            1, 2, 5, 10, 25, 50, 75,
            100, 200, 500, 1_000, 2_500, 5_000, 7_500,
            10_00, 20_00, 50_00, 100_000, 250_000, 500_000, 750_000,
        };


        public BoxWhiskerControl()
        {
            this.InitializeComponent();
        }
        private Statistics CurrStatistics = null;
        public Statistics GetStatistics()
        {
            return CurrStatistics;
        }
        public void SetStatistics(Statistics stats, bool isFull) //isFull is ignored for this
        {
            // NEW: TODO: when not connected, stats is null
            if (stats == null)
            {
                return;
            }
            var format = "N3";
            CurrStatistics = stats;

            double canvasHeight = uiCanvas.Height; // Not ActualHeight because it might not have been layed out yet.
            double canvasMin = canvasHeight;
            double canvasMax = 0.0;
            double domainMax = GetYMax(stats.Max, YMaxDefault, stats.Max * 1.1);// A bit bigger than the max value
            statRangeMax.Y1 = Map(stats.Max, 0.0, domainMax, canvasMin, canvasMax);
            statRangeMax.Y2 = Map(stats.IqrHigh, 0.0, domainMax, canvasMin, canvasMax);
            statRangeMin.Y1 = Map(stats.IqrLow, 0.0, domainMax, canvasMin, canvasMax);
            statRangeMin.Y2 = Map(stats.Min, 0.0, domainMax, canvasMin, canvasMax);
            statMax.Y1 = Map(stats.Max, 0.0, domainMax, canvasMin, canvasMax);
            statMax.Y2 = Map(stats.Max, 0.0, domainMax, canvasMin, canvasMax);
            statMin.Y1 = Map(stats.Min, 0.0, domainMax, canvasMin, canvasMax);
            statMin.Y2 = Map(stats.Min, 0.0, domainMax, canvasMin, canvasMax);
            statMedian.Y1 = Map(stats.Median, 0.0, domainMax, canvasMin, canvasMax);
            statMedian.Y2 = Map(stats.Median, 0.0, domainMax, canvasMin, canvasMax);
            statIqr.Height = Map(stats.IqrHigh - stats.IqrLow, 0.0, domainMax, 0, canvasHeight); // Height is relative to height of the canvas, not the flipped-around Y values
            double addedHeight = statIqr.Height > 3.0 ? 0.0 : (3.0 - statIqr.Height);

            // Otherwise the "box" for the IQR range is not visible, which looks terrible.
            var newTop = Map(stats.IqrHigh, 0.0, domainMax, canvasMin, canvasMax);
            if (addedHeight > 0.0)
            {
                statIqr.Height += addedHeight;
                newTop -= (addedHeight / 2.0);
            }
            Canvas.SetTop(statIqr, newTop);

            uistatYAxisMin.Text = "0.00";
            uistatYAxisMax.Text = domainMax.ToString(format);
        }

        private static double GetYMax(double maxy, double[] template, double defaultValue)
        {
            for (int i=0; i<template.Length; i++)
            {
                if (template[i] >= maxy) return template[i];
            }
            return defaultValue;
        }

        private static void Log(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
            Console.WriteLine(str);
        }


        public static int Test()
        {
            int nerror = 0;
            nerror += TestMap();
            return nerror;
        }

        private static int TestMap()
        {
            int nerror = 0;
            nerror += TestMapOne(100.0, 100.0, 110.0, 200.0, 240.0, 200.0);
            nerror += TestMapOne(105.0, 100.0, 110.0, 200.0, 240.0, 220.0);
            nerror += TestMapOne(110.0, 100.0, 110.0, 200.0, 240.0, 240.0);

            //Make sure it works upside-down, too
            nerror += TestMapOne(0.0, 0.0, 0.5, 200.0, 0.0, 200.0);
            nerror += TestMapOne(0.2, 0.0, 0.5, 200.0, 0.0, 120.0);
            nerror += TestMapOne(0.5, 0.0, 0.5, 200.0, 0.0, 0.0);

            nerror += TestMapOne(0.2, 0.0, 0.5, 200.0, 0.0, 120.0);
            return nerror;
        }

        private static int TestMapOne(double value, double domainMin, double domainMax, double rangeMin, double rangeMax, double expected)
        {
            int nerror = 0;
            var actual = Map(value, domainMin, domainMax, rangeMin, rangeMax);
            var delta = Math.Abs(actual - expected);
            if (delta > 0.001)
            {
                nerror += 1;
                Log($"Error: Map: ({value}, {domainMin}, {domainMax}, {rangeMin}, {rangeMax}) actual={actual} expected={expected}");
            }
            return nerror;
        }
    }
}

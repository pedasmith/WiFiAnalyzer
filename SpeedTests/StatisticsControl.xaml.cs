using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class StatisticsControl : UserControl, ISetStatistics
    {
        public StatisticsControl()
        {
            this.InitializeComponent();
        }
        public void SetStatistics(Statistics stats)
        {
            string format = "N3";
            uistatMin.Text = stats.Min.ToString(format);
            uistatIqrLow.Text = stats.IqrLow.ToString(format);
            uistatMedian.Text = stats.Median.ToString(format);
            uistatMean.Text = stats.Average.ToString(format);
            uistatIqrHigh.Text = stats.IqrHigh.ToString(format);
            uistatMax.Text = stats.Max.ToString(format);
            uistatRange.Text = stats.Range.ToString(format);
            uistatStdDev.Text = stats.StdDev.ToString(format);

            var txt = "";
            foreach (var item in stats.PreAdditionalInfo)
            {
                var tab = item.Name.Length <= 7 ? "\t\t" : "\t";
                txt += $"{item.Name}{tab}{item.Value}\n";
            }
            uiPreInfo.Text = txt;

            txt = "";
            foreach (var item in stats.PostAdditionalInfo)
            {
                var tab = item.Name.Length < 8 ? "\t\t" : "\t";
                txt += $"{item.Name}{tab}{item.Value}\n";
            }
            uiPostInfo.Text = txt;
        }
    }
}

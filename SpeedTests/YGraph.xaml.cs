using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SpeedTests
{
    public sealed partial class YGraph : UserControl
    {
        public string ValueSuffix = " MByte/Second";
        public YGraph()
        {
            this.InitializeComponent();
            this.Loaded += YGraph_Loaded;
        }
        public Statistics CurrStatistics = null;
        public Statistics GetStatistics()
        {
            return CurrStatistics;
        }
        private void YGraph_Loaded(object sender, RoutedEventArgs e)
        {
            //var task = DemoUsage();
        }

        private async Task DemoUsage()
        {
            var list1 = new List<double>() { 17, 18, 19 };
            Update(list1);
            await Task.Delay(1_000);
            list1.Add(17); Update(list1); await Task.Delay(1_000);
            list1.Add(23); Update(list1); await Task.Delay(1_000);
            list1.Add(21); Update(list1); await Task.Delay(1_000);
            list1.Add(10); Update(list1); await Task.Delay(1_000);
            list1.Add(17); Update(list1); await Task.Delay(1_000);
        }


        List<double> Values = new List<double>();
        List<Line> Lines = new List<Line>();
        public void Update(List<double> values)
        {
            Values.Clear();
            foreach (var item in values)
            {
                Values.Add(item);
            }
            Redraw();
        }

        public void AddValue(double value)
        {
            Values.Add(value);
            Redraw();
        }

        private void Redraw()
        {
            EnsureEnoughLines(Values.Count);
            SpaceLines();
            var ymax = VerticalLines(Values.ToArray()); //TODO: inefficiently swapping from list to double :-(
            uistatYAxisMax.Text = ymax.ToString() + ValueSuffix;
            uistatYCurrValue.Text = Values[Values.Count-1].ToString();
        }

        public void SetValue(double value)
        {
            uistatYCurrValue.Text = value.ToString();
        }


        private void EnsureEnoughLines(int nValues)
        {
            var nToAdd = (nValues + 1) - Lines.Count;
            for (int i=0; i<nToAdd; i++)
            {
                var newLine = new Line() { StrokeThickness = 1, };
                Lines.Add(newLine);
                uiCanvas.Children.Add(newLine);
            }

            // First and last lines are invisible.
            if (Lines.Count >= 1)
            {
                Lines[0].Opacity = 0;
                Lines[Lines.Count - 1].Opacity = 0;
            }
            for (int i=1; i<Lines.Count-1; i++)
            {
                Lines[i].Opacity = 1.0;
            }
        }

        /// <summary>
        /// Set the X1 and X2 value for each line
        /// </summary>
        private void SpaceLines()
        {
            if (Lines.Count == 0) return;
            var delta = uiCanvas.Width / Lines.Count;
            for (int i=0; i<Lines.Count; i++)
            {
                var x = (double)i  * delta;
                var line = Lines[i];
                line.X1 = x;
                line.X2 = x + delta;
            }
        }

        /// <summary>
        /// Set the Y1 and Y2 values of the lines; return new nice max value
        /// </summary>
        private double VerticalLines(double[] values)
        {
            double graphMin = uiCanvas.Height - 5;
            double graphMax = 5; // not quite at the top
            double graphH = Math.Abs(graphMin - graphMax);

            var (valueMin, valueMax) = MathMinMax.MinMax(values);
            valueMin = 0.0; // I know this is always the best value
            valueMax = MathMinMax.NiceValue(valueMax, 
                new double[] { 1.0, 2.0, 5.0, 7.5, 10.0, 15.0, 20.0, 25.0, 30.0, 40.0, 50.0, 60.0, 75.0, 100.0, 200.0, 500.0, 1000.0  });
            for (int i=0; i<values.Length; i++)
            {
                double y = MathMinMax.Rescale(values[i], valueMin, valueMax, graphMin, graphMax);
                Lines[i].Y2 = y;
                Lines[i + 1].Y1 = y;
            }
            return valueMax;
        }
    }
}

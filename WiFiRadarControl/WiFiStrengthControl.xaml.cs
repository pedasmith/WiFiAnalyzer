using SmartWiFiHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WiFiRadarControl
{
    public sealed partial class WiFiStrengthControl : UserControl
    {
        public IDisplayWifiNetworkInformation DisplayInfo = null;

        public WiFiStrengthControl()
        {
            this.InitializeComponent();
        }

        List<Brush> RectBrushes = new List<Brush>()
        {
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.Green),
            new SolidColorBrush(Colors.Blue),
            new SolidColorBrush(Colors.Cyan),
            new SolidColorBrush(Colors.Magenta),

            new SolidColorBrush(Colors.DarkRed),
            new SolidColorBrush(Colors.DarkGreen),
            new SolidColorBrush(Colors.DarkBlue),
            new SolidColorBrush(Colors.DarkCyan),
            new SolidColorBrush(Colors.DarkMagenta),
        };
        Brush OutlineExact = new SolidColorBrush(Colors.Black);
        Brush OutlineOverlap = new SolidColorBrush(Colors.White);
        Thickness RectMarginExact = new Thickness(3, 3, 3, 3);
        Thickness RectMarginOverlap = new Thickness(3, 3+5, 3, 3+5);

        static Dictionary<string, Brush> BrushDictionary = new Dictionary<string, Brush>();

        Brush GetBrush(int colorIndex, WifiNetworkInformation wifiNetworkInformation)
        {
            if (wifiNetworkInformation.SSID == "MSFTCONNECT")
            {
                ; // handy hook for the debugger.
            }
            var key = wifiNetworkInformation.Bssid;
            if (BrushDictionary.ContainsKey (key))
            {
                return BrushDictionary[key];
            }
            var brush = RectBrushes[colorIndex % RectBrushes.Count];
            BrushDictionary.Add(key, brush);
            return brush;
        }
        static int ColorIndex = 0;
        public static void ResetColorIndex() { ColorIndex = 0; }

        public void SetStrength(OrderedBandInfo list)
        {
            uiText.Text = list.FrequencyInGigahertz.ToString();
            const double HeightOverlap = 10.0;
            const double HeightExact = 20.0;
            var lf = MathLogisticFunctions.CreateAmbientNoiseBarSize();

            uiChannelName.Text = list.WBC?.ChannelName;

            foreach (var wifiNetworkInfo in list.InfoOverlapFrequency)
            {
                var rect = CreateRect(lf, wifiNetworkInfo);
                rect.Height = HeightOverlap;
                rect.Stroke = OutlineOverlap;
                rect.Margin = RectMarginOverlap;
                uiStrength.Children.Add(rect);
                ColorIndex++;
            }
            foreach (var wifiNetworkInfo in list.InfoExactFrequency)
            {
                var rect = CreateRect(lf, wifiNetworkInfo);
                rect.Height = HeightExact;
                rect.Stroke = OutlineExact;
                rect.Margin = RectMarginExact;
                uiStrength.Children.Add(rect);

                ColorIndex++;
            }
            var weight = list.InfoExactFrequency.Count > 0 ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal;
            uiChannelName.FontWeight = weight;
        }

        private Rectangle CreateRect(MathLogisticFunctions lf, WifiNetworkInformation wifiNetworkInfo)
        {
            const double multiplier = 10.0;
            var width = lf.Calculate(wifiNetworkInfo.Rssi) * multiplier;
            var brush = GetBrush(ColorIndex, wifiNetworkInfo);
            var rect = new Rectangle() { Width = width, Fill = brush, Tag = wifiNetworkInfo };
            rect.Tapped += Strength_Tapped;
            rect.DoubleTapped += Strength_DoubleTapped;
            ToolTipService.SetToolTip(rect, new ToolTip() { Content = $"SSID={wifiNetworkInfo.SSID}" });
            return rect;
        }
        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
        private void Strength_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var wni = (sender as FrameworkElement).Tag as WifiNetworkInformation;
            DisplayInfo?.DisplayOneLine(wni);
        }
        private void Strength_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var wni = (sender as FrameworkElement).Tag as WifiNetworkInformation;
            DisplayInfo?.Display(wni);
        }
    }
}

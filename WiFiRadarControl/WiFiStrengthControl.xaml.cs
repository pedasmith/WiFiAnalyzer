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
        private static Color rgb(byte r, byte g, byte b) // rgb(124, 119, 102)
        {
            return new Color() { R = r, G = g, B = b, A=255 };
        }

        List<Brush> RectBrushes = new List<Brush>()
        {
            new SolidColorBrush(rgb(254, 176, 153)),
            new SolidColorBrush(rgb(255, 164, 137)),
            new SolidColorBrush(rgb(255, 157, 128)),
            new SolidColorBrush(rgb(255, 151, 121)),
            new SolidColorBrush(rgb(255, 145, 112)),
            new SolidColorBrush(rgb(255, 126, 87)),
            new SolidColorBrush(rgb(255, 114, 71)),
            new SolidColorBrush(rgb(255, 101, 55)),
            new SolidColorBrush(rgb(255, 87, 38)),
            new SolidColorBrush(rgb(255, 75, 22)),
            new SolidColorBrush(rgb(255, 63, 6)),
            new SolidColorBrush(rgb(228, 53, 0)),
            new SolidColorBrush(rgb(204, 48, 0)),
            new SolidColorBrush(rgb(186, 43, 0)),
            new SolidColorBrush(rgb(171, 39, 0)),
            new SolidColorBrush(rgb(147, 34, 0)),
            new SolidColorBrush(rgb(122, 28, 0)),
            new SolidColorBrush(rgb(97, 22, 0)),
            new SolidColorBrush(rgb(88, 20, 0)),
            new SolidColorBrush(rgb(57, 13, 0)),
            new SolidColorBrush(rgb(32, 7, 0)),
        };
        Brush OutlineExact = new SolidColorBrush(Colors.Black);
        Brush OutlineOverlap = new SolidColorBrush(Colors.White);
        Thickness RectMarginExact = new Thickness(3, 3, 3, 3);
        Thickness RectMarginOverlap = new Thickness(3, 3+5, 3, 3+5);

        static Dictionary<string, Brush> BrushDictionary = new Dictionary<string, Brush>();

        Brush GetBrush(int colorIndex, WiFiNetworkInformation wifiNetworkInformation)
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

        public void SetStrength(BandUsageInfo list)
        {
            uiFrequency.Text = list.FrequencyInGigahertz.ToString();
            uiBandwidth.Text = (list.WBC.BandwidthInKilohertzList[0] / 1_000).ToString();
            const double HeightOverlap = 10.0;
            const double HeightExact = 20.0;
            var lf = MathLogisticFunctions.CreateAmbientNoiseBarSize();

            uiChannelName.Text = list.WBC?.ChannelName;

            var orderedList = list.InfoOverlapFrequency.OrderBy(comparer => comparer.Rssi);   
            foreach (var wifiNetworkInfo in orderedList)
            {
                var rect = CreateRect(lf, wifiNetworkInfo);
                CustomizeRect(rect, HeightOverlap, OutlineOverlap, RectMarginOverlap);
                uiStrength.Children.Add(rect);
                ColorIndex++;
            }
            orderedList = list.InfoExactFrequency.OrderBy(comparer => comparer.Rssi);
            foreach (var wifiNetworkInfo in orderedList)
            {
                var rect = CreateRect(lf, wifiNetworkInfo);
                CustomizeRect(rect, HeightExact, OutlineExact, RectMarginExact);
                uiStrength.Children.Add(rect);

                ColorIndex++;
            }
            var weight = list.InfoExactFrequency.Count > 0 ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal;
            uiChannelName.FontWeight = weight;
        }
        private void CustomizeRect(Rectangle rect, double height, Brush outline, Thickness margin)
        {
            rect.Height = height;
            rect.Stroke = outline;
            rect.Margin = margin;
        }

        private Rectangle CreateRect(MathLogisticFunctions lf, WiFiNetworkInformation wifiNetworkInfo)
        {
            const double multiplier = 10.0;
            var width = lf.Calculate(wifiNetworkInfo.Rssi) * multiplier;
            var brush = GetBrush(ColorIndex, wifiNetworkInfo);
            var rect = new Rectangle() { Width = width, Fill = brush, Tag = wifiNetworkInfo };
            rect.Tapped += Strength_Tapped;
            rect.DoubleTapped += Strength_DoubleTapped;
            ToolTipService.SetToolTip(rect, new ToolTip() { Content = $"{wifiNetworkInfo.SSID.OrUnnamed()}" });
            return rect;
        }
        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
        private void Strength_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var wni = (sender as FrameworkElement).Tag as WiFiNetworkInformation;
            DisplayInfo?.DisplayOneLine(wni);
        }
        private void Strength_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var wni = (sender as FrameworkElement).Tag as WiFiNetworkInformation;
            DisplayInfo?.Display(wni);
        }
    }
}

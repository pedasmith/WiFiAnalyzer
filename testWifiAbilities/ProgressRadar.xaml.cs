using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.Networking.BackgroundTransfer;
using Windows.UI;
using Windows.UI.WebUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace testWifiAbilities
{
    public interface IDisplayWifiNetworkInformation
    {
        void Display(WifiNetworkInformation value);
    }
    /// <summary>
    /// The APs that have been discovered (or "dummy" APs)
    /// </summary>
    public class Reflector
    {
        public Reflector()
        {
        }
        public Point Center;
        public string Icon { get; set; } = Icon_Dummy;
        public const string Icon_Dummy = ""; // RADIOBTNON
        public const string Icon_AP = ""; // MICROSOFT SYBOL APPBAR GLYPH NETWORKTOWER
        public const string Icon_ConnectedAP = ""; // WIFIHOTSPOT  ""; // MICROSOFT SYMBOL APPBAR GLYPH INTERNETSHARING

        public string Name { get { return NetworkInformation.SSID; } }
        public WifiNetworkInformation NetworkInformation { get; set; }

        public List<FrameworkElement> ToBeRemoved = new List<FrameworkElement>();
        public const int PreferredAPPerRing = 7;
        public const int PreferredUnknownRingCount = 1;

    }

    /// <summary>
    /// Primary class for the Radar
    /// Call
    /// </summary>
    public sealed partial class ProgressRadar : UserControl
    {
        // UX constants
        double RingSpeed = 75.0;
        const int NRING = 5;

        public IDisplayWifiNetworkInformation DisplayWifiNetworkInformation = null;

        Timer AnimationTimer;
        List<Reflector> Reflectors = new List<Reflector>();
        List<Ring> Rings = new List<Ring>();
        List<Reflection> Reflections = new List<Reflection>();

        Brush ReticuleBrush = new SolidColorBrush(Colors.LightBlue);

        public ProgressRadar()
        {
            this.InitializeComponent();
            this.Loaded += ProgressRadar_Loaded;
        }
        // State variables
        bool reflectorsInit = false;
        bool amStopping = false;
        bool shouldAnimate = false;


        /// <summary>
        /// Initialize the radar and start it
        /// </summary>
        public void Initialize()
        {
            amStopping = false;
            foreach (var reflection in Reflections)
            {
                uiCanvas.Children.Remove(reflection.Arc);
            }
            foreach (var reflector in Reflectors)
            {
                foreach (var fe in reflector.ToBeRemoved)
                {
                    uiCanvas.Children.Remove(fe);
                }
            }
            foreach (var fe in ReticuleElements)
            {
                uiCanvas.Children.Remove(fe);
            }
            foreach (var ring in Rings)
            {
                uiCanvas.Children.Remove(ring.Circle);
            }


            Reflectors.Clear();
            Rings.Clear();
            Reflections.Clear();
            ReticuleElements.Clear();
            reflectorsInit = false; // Can't do this until we're initializing.
            shouldAnimate = true;
        }

        public void SetReflectors(List<Reflector> reflectors)
        {
            // Remove the old (including the UX)
            foreach (var reflector in Reflectors)
            {
                foreach (var fe in reflector.ToBeRemoved)
                {
                    uiCanvas.Children.Remove(fe);
                }    
            }
            Reflectors.Clear();

            // Add the new
            foreach (var reflector in reflectors)
            {
                Reflectors.Add(reflector);
            }
            reflectorsInit = false; // next animation will get the UX set up
        }

        /// <summary>
        /// Tells the radar to stop, waiting for all the rings to go away (up to 10 seconds)
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            var start = DateTime.UtcNow;
            amStopping = true;
            bool allStopped = false;
            bool goneTooLong = false;
            while (!allStopped && !goneTooLong)
            {
                allStopped = RingsAllStoped();
                if (!allStopped)
                {
                    await Task.Delay(100); // stopping in a tenth of a second should be good enough.
                }
                var delta = DateTime.UtcNow.Subtract(start).TotalSeconds;
                if (delta >= 10.0)
                {
                    goneTooLong = true;
                }
            }
            shouldAnimate = false;
        }

        private bool RingsAllStoped()
        {
            bool checkAllStopped = true;
            foreach (var ring in Rings)
            {
                if (!ring.CycleComplete) checkAllStopped = false;
            }
            return checkAllStopped;
        }


        DateTime LastUpdateTime;
        private void ProgressRadar_Loaded(object sender, RoutedEventArgs e)
        {
            LastUpdateTime = DateTime.UtcNow;

            AnimationTimer = new Timer(async (obj) =>  {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    UpdateRadar();
                });
            }, null, 0, 20); // every 100 milliseconds
        }




        private static double Distance(Ring ring, Reflector reflector)
        {
            var dx = ring.Center.X - reflector.Center.X;
            var dy = ring.Center.Y - reflector.Center.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            return dist;
        }

        double NDrawnRings = 0;
        double DrawnRingRadius = 0.0;

        private void InitializeReflectorLocations()
        {
            var center = new Point(uiCanvas.ActualWidth / 2.0, uiCanvas.ActualHeight / 2.0); // Canvas doesn't have a size until it's displayed once.
            const int NReflectorsPerRing = Reflector.PreferredAPPerRing;
            double deltaAngle = Math.PI * 2.0 / NReflectorsPerRing;

            var maxRings = Math.Ceiling ((double)Reflectors.Count / (double)NReflectorsPerRing);
            var distanceDelta = Math.Min (center.X, center.Y) / ((double)maxRings + 0.2); // don't get too close to the edge

            NDrawnRings = maxRings;
            DrawnRingRadius = distanceDelta;

            var distance = distanceDelta;
            var offsetAngle = -Math.PI / 2; // at the top
            for (int i=0, ringIndex=0; i<Reflectors.Count; i++)
            {
                var reflector = Reflectors[i];  
                double angle = ringIndex * deltaAngle + offsetAngle;
                reflector.Center = new Point(distance * Math.Cos(angle) + center.X, distance * Math.Sin(angle) + center.Y);

                ringIndex++;
                if (ringIndex >= NReflectorsPerRing)
                {
                    ringIndex = 0;
                    offsetAngle -= deltaAngle / maxRings; // actually a constant depending on the number of reflectors.
                    distance += distanceDelta;
                }
            }
        }

        FontFamily IconFontFamily = new FontFamily("Segoe UI,Segoe MDL2 Assets");
        const int TextZIndex = 5;
        private void InitializeReflectorText(Reflector reflector)
        {
            var bdr = new Border()
            {
                Background = new SolidColorBrush(Colors.White),
                Padding = new Thickness(5),
                Margin = new Thickness(5),
                IsTapEnabled = true,
                Tag = reflector,
            };
            bdr.Tapped += Bdr_Tapped;
            var sp = new StackPanel();

            var tb = new TextBlock()
            {
                Text = reflector.Icon,
                FontSize = 15,
                FontFamily = IconFontFamily,
                Foreground = new SolidColorBrush(Colors.Black),
                TextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
            };
            sp.Children.Add(tb);

            if (reflector.NetworkInformation != null)
            {
                tb = new TextBlock()
                {
                    Text = reflector.NetworkInformation.SSID,
                    FontSize = 15,
                    FontFamily = IconFontFamily,
                    Foreground = new SolidColorBrush(Colors.Black),
                    TextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                sp.Children.Add(tb);
                /*
                uiCanvas.Children.Add(tb);
                Canvas.SetLeft(tb, reflector.Center.X - 10);
                Canvas.SetTop(tb, reflector.Center.Y - 10);
                uiCanvas.Children.Add(tb);
                tb.Measure(new Size(300, 300));
                Canvas.SetLeft(tb, reflector.Center.X - 10 - tb.DesiredSize.Width / 2.0);
                Canvas.SetTop(tb, reflector.Center.Y - 10 + 20);
                */

            }

            bdr.Child = sp;
            uiCanvas.Children.Add(bdr);
            bdr.Measure(new Size(300, 300));
            Canvas.SetLeft(bdr, reflector.Center.X - bdr.DesiredSize.Width / 2.0);
            Canvas.SetTop(bdr, reflector.Center.Y - 15);
            Canvas.SetZIndex(bdr, TextZIndex);

            reflector.ToBeRemoved.Add(bdr);
        }

        DoubleCollection ReticuleStrokeDashArray = new DoubleCollection() { 2, 6 };

        /// <summary>
        /// Call this after calling the InitializeReflectorLocations (it uses the NDrawnRings etc)
        /// </summary>
        /// <param name="uiCanvas"></param>
        private void DrawReticule(Canvas uiCanvas)
        {
            double DotRadius = 10;

            double CX = (uiCanvas.ActualWidth / 2.0);
            double CY = (uiCanvas.ActualHeight / 2.0);

            var dot = new Ellipse()
            {
                Stroke = ReticuleBrush,
                Fill = ReticuleBrush,
                Width = DotRadius*2.0,
                Height = DotRadius*2.0,
            };
            uiCanvas.Children.Add(dot);
            Canvas.SetLeft(dot, CX - DotRadius);
            Canvas.SetTop(dot, CY - DotRadius);
            ReticuleElements.Add(dot);

            // RADAR rings
            for (int i=0; i<NDrawnRings; i++)
            {
                var radius = (i + 1) * DrawnRingRadius;
                var ring = new Ellipse()
                {
                    Stroke = ReticuleBrush,
                    Width = radius*2.0,
                    Height = radius*2.0,
                    StrokeDashArray = new DoubleCollection() { 2, 6 },
                    StrokeThickness = 2,
                };
                uiCanvas.Children.Add(ring);
                Canvas.SetLeft(ring, CX - radius);
                Canvas.SetTop(ring, CY - radius);
                ReticuleElements.Add(ring);
            }

            const int NReflectorsPerRing = Reflector.PreferredAPPerRing;
            double length = NDrawnRings * DrawnRingRadius * 1.2; // Spills out past the last ring
            double angleDelta = Math.PI * 2.0 / NReflectorsPerRing;
            var offsetAngle = -Math.PI / 2; // at the top

            for (double angle = 0.0; angle <= Math.PI * 2; angle += angleDelta)
            {
                var line = new Line()
                {
                    Stroke = ReticuleBrush,
                    StrokeDashArray = new DoubleCollection() { 2, 6 },
                    StrokeThickness = 2,
                    X1 = CX, 
                    Y1 = CY,
                    X2 = CX + length * Math.Cos(angle + offsetAngle),
                    Y2 = CY + length * Math.Sin(angle + offsetAngle),
                };
                uiCanvas.Children.Add(line);
                ReticuleElements.Add(line);
            }
        }
        List<FrameworkElement> ReticuleElements = new List<FrameworkElement>();

        private void Bdr_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var reflector = (sender as Border)?.Tag as Reflector;
            var ni = reflector?.NetworkInformation;
            if (ni == null) return;
            DisplayWifiNetworkInformation?.Display(ni);
            Log($"Click: SSID={ni.SSID} bssid={ni.Bssid} RSSI={ni.Rssi}");
        }

        public void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine(text); //TODO: remove
        }

        public void AddDummyReflectors()
        {
            for (int i = 0; i < Reflector.PreferredAPPerRing * Reflector.PreferredUnknownRingCount; i++)
            {
                Reflectors.Add(new Reflector());
            }
        }


        private void UpdateRadar()
        {
            if (!shouldAnimate) return;

            var now = DateTime.UtcNow;
            var delta = now.Subtract(LastUpdateTime).TotalSeconds;
            LastUpdateTime = now;

            var center = new Point(uiCanvas.ActualWidth / 2.0, uiCanvas.ActualHeight / 2.0); // Canvas doesn't have a size until it's displayed once.
            var maxRadius = Math.Max(uiCanvas.ActualWidth, uiCanvas.ActualHeight) / 2.0;

            // Time to add a new ring?
            bool addRing = false;
            addRing = Rings.Count == 0;
            var ringAddRadius = maxRadius / (double)NRING;
            if (Rings.Count > 0 && Rings.Count < NRING && Rings[Rings.Count - 1].Radius > ringAddRadius) addRing = true;
            if (amStopping) addRing = false;

            if (!reflectorsInit) // Add in dummy reflectors
            {
                reflectorsInit = true;
                InitializeReflectorLocations();
                foreach (var reflector in Reflectors)
                {
                    InitializeReflectorText(reflector);
                }
                DrawReticule(uiCanvas);
            }

            var rnd = new Random(); // TODO: make this static.

            if (addRing)
            {
                Rings.Add(new Ring(uiCanvas, center, null, RingSpeed, 2.0, maxRadius)); // null=use the default brush;
            }

            foreach (var ring in Rings)
            {
                ring.Update(delta, amStopping);
            }

            // Add new reflections as needed.
            if (!amStopping)
            {
                foreach (var ring in Rings)
                {
                    foreach (var reflector in Reflectors)
                    {
                        var d = Distance(ring, reflector);
                        if (d >= ring.OldRadius && d < ring.Radius)
                        {
                            var reflection = new Reflection(uiCanvas, reflector.Center, ring.Center, new SolidColorBrush(Colors.DarkGreen), RingSpeed, 2.0, d);
                            Reflections.Add(reflection);
                        }
                    }
                }
            }
            foreach (var reflection in Reflections)
            {
                reflection.Update(delta);
            }

            // Get rid of old reflections when they are done.
            for (int i=Reflections.Count-1; i>=0; i--)
            {
                if (Reflections[i].CycleComplete)
                {
                    uiCanvas.Children.Remove(Reflections[i].Arc);
                    Reflections.RemoveAt(i);
                }
            }
        }
    }
}

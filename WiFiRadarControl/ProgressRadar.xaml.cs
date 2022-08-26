using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using SmartWiFiHelpers;
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WiFiRadarControl
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
            RemoveReflectionsElements();
            RemoveReflectorsElements();
            RemoveReticuleElements();
            RemoveRingsElements();


            Reflectors.Clear();
            Rings.Clear();
            reflectorsInit = false; // Can't do this until we're initializing.
            shouldAnimate = true;
        }

        private void RemoveReflectionsElements()
        {
            foreach (var reflection in Reflections)
            {
                uiCanvas.Children.Remove(reflection.Arc);
            }
        }

        private void RemoveReflectorsElements()
        {
            foreach (var reflector in Reflectors)
            {
                foreach (var fe in reflector.ToBeRemoved)
                {
                    uiCanvas.Children.Remove(fe);
                }
                reflector.ToBeRemoved.Clear();
            }
        }

        private void RemoveReticuleElements()
        {
            foreach (var fe in ReticuleElements)
            {
                uiCanvas.Children.Remove(fe);
            }
            ReticuleElements.Clear();
        }

        private void RemoveRingsElements()
        {
            foreach (var ring in Rings)
            {
                uiCanvas.Children.Remove(ring.Circle);
                ring.Circle = null;
            }
        }

        public void SetReflectors(List<Reflector> reflectors)
        {
            RemoveReflectorsElements();
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
        public void StopInstantly()
        {
            amStopping = true;
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


        public class RingLayoutData
        {
            public double NDrawnRings = 0.0;
            public double DrawnRingRadius = 0.0;
            public double InnerRadius = 0.0;
            public double OuterRadius = 0.0;
            public int NItemsInOuterRing = 0;
        }
        RingLayoutData RingLayout = new RingLayoutData();




        FontFamily IconFontFamily = new FontFamily("Segoe UI,Segoe MDL2 Assets");
        const int TextZIndex = 5;
        private void DrawReflectorText(Reflector reflector)
        {
            var bdr = new Border()
            {
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
            };
            var bdrIcon = new Border()
            {
                IsTapEnabled = true,
                Tag = reflector,
            };
            //bdrIcon.Tapped += Bdr_Tapped;
            bdrIcon.Child = tb;
            sp.Children.Add(bdrIcon);

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
                var bdrName = new Border()
                {
                    Background = new SolidColorBrush(Colors.White),
                    IsTapEnabled = true,
                    Tag = reflector,
                };
                //bdrName.Tapped += Bdr_Tapped;
                bdrName.Child = tb;
                sp.Children.Add(bdrName);
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
        private void DrawReticule(Canvas uiCanvas, RingLayoutData layoutData)
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
            for (int i=0; i< layoutData.NDrawnRings; i++)
            {
                var radius = (double)i * layoutData.DrawnRingRadius + layoutData.InnerRadius;
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

            //const int NReflectorsPerRing = Reflector.PreferredAPPerRing;
            double length = layoutData.OuterRadius;
            double angleDelta = Math.PI * 2.0 / layoutData.NItemsInOuterRing;
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

        Random rnd = new Random(); 

        private void AddRingsIfNeeded()
        {
            var center = new Point(uiCanvas.ActualWidth / 2.0, uiCanvas.ActualHeight / 2.0); // Canvas doesn't have a size until it's displayed once.
            var maxRadius = Math.Max(uiCanvas.ActualWidth, uiCanvas.ActualHeight) / 2.0;

            bool addRing;
            addRing = Rings.Count == 0;
            var ringAddRadius = maxRadius / (double)NRING;
            if (Rings.Count > 0 && Rings.Count < NRING && Rings[Rings.Count - 1].Radius > ringAddRadius) addRing = true;
            if (amStopping) addRing = false;

            if (addRing)
            {
                Rings.Add(new Ring(uiCanvas, center, null, RingSpeed, 2.0, maxRadius)); // null=use the default brush;
            }

        }

        private void DrawReflectorsIfNeeded(bool forceRedraw = false)
        {
            if (!reflectorsInit || forceRedraw)
            {
                reflectorsInit = true;
                RemoveReflectorsElements();
                RadarHelpers.InitializeReflectorLocations(uiCanvas, RingLayout, Reflectors);
                foreach (var reflector in Reflectors)
                {
                    DrawReflectorText(reflector);
                }
                RemoveReticuleElements();
                DrawReticule(uiCanvas, RingLayout);
            }
        }

        private void AddReflectionsAsNeeded()
        {
            if (!amStopping)
            {
                foreach (var ring in Rings)
                {
                    foreach (var reflector in Reflectors)
                    {
                        var d = Distance(ring, reflector);
                        if (d >= ring.OldRadius && d < ring.Radius)
                        {
                            var reflection = new Reflection(uiCanvas, reflector, ring, new SolidColorBrush(Colors.DarkGreen), RingSpeed, 2.0, d);
                            Reflections.Add(reflection);
                        }
                    }
                }
            }
        }

        private void UpdateRadar()
        {
            DrawReflectorsIfNeeded(); // Always do this.
            if (!shouldAnimate) return;
            if (uiFreeze.IsChecked.Value) return; // handy for debugging

            var now = DateTime.UtcNow;
            var delta = now.Subtract(LastUpdateTime).TotalSeconds;
            LastUpdateTime = now;


            // Time to add a new ring?
            AddRingsIfNeeded();
            foreach (var ring in Rings)
            {
                ring.Update(delta, amStopping);
            }

            AddReflectionsAsNeeded();
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


        int NDemo = 20;
        private async void OnRedraw(object sender, RoutedEventArgs e)
        {
            NDemo = Int32.Parse(uiNDemo.Text);
            await DoRedraw(NDemo);
        }
        private async void OnAddAndRedraw(object sender, RoutedEventArgs e)
        {
            var delta = Int32.Parse((sender as Button).Tag as string);
            NDemo += delta;
            uiNDemo.Text = NDemo.ToString();
            await DoRedraw(NDemo);
        }
        private async Task DoRedraw(int nwifi)
        {
            var list = new List<Reflector>();
            for (int i=1; i<=nwifi; i++)
            {
                var name = $"MyWifi_{i:D3}";
                var wifi = new WifiNetworkInformation()
                {
                    SSID = name,
                    Bssid = i.ToString(),
                };
                
                var reflector = new Reflector()
                {
                    Icon = Reflector.Icon_AP,
                    NetworkInformation = wifi,
                };
                list.Add(reflector);
            }
            StopInstantly();
            Initialize();
            SetReflectors(list);
            await StopAsync();
        }

        private void OnRedrawRadar(object sender, RoutedEventArgs e)
        {
            var center = new Point(uiCanvas.ActualWidth / 2.0, uiCanvas.ActualHeight / 2.0); // Canvas doesn't have a size until it's displayed once.
            //DumpAllPositions($"Start: center  x={Math.Round(center.X)} y={Math.Round(center.Y)}");


            // Correct order: reflectors then reflections. Rings can be in any order.
            DrawReflectorsIfNeeded(true); // force a redraw
            // Must do rings before relections(because the reflections point to the rings)
            foreach (var ring in Rings)
            {
                ring.Reposition(center);
            }

            foreach (var reflection in Reflections)
            {
                reflection.ResetPointTo();
                reflection.SetPosition();
                reflection.Update(0.0);
            }
            //DumpAllPositions($"End:");
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Log($"DBG: SizeChange height={Math.Round(uiCanvas.ActualHeight)} width={Math.Round(uiCanvas.ActualWidth)}");
            OnRedrawRadar(null, null);
        }

        private void DumpAllPositions(string note)
        {
            Log($"All Positions: {note}");
            Log($"===========================");

            for (int i = 0; i < Reflectors.Count; i++)
            {
                var value = Reflectors[i];
                Log($"    Reflector {i}: x={Math.Round(value.Center.X)} y={Math.Round(value.Center.Y)}");
            }

            for (int i = 0; i < Rings.Count; i++)
            {
                var value = Rings[i];
                Log($"    Ring {i}: x={Math.Round(value.Center.X)} y={Math.Round(value.Center.Y)}");
            }

            for (int i = 0; i < Reflections.Count; i++)
            {
                var value = Reflections[i];
                Log($"    Reflection {i}: radius={Math.Round(value.Radius)} x={Math.Round(value.CenterObject.Center.X)} y={Math.Round(value.CenterObject.Center.Y)}   pointTo.x={Math.Round(value.PointToObject.Center.X)} pointTo.y={Math.Round(value.PointToObject.Center.Y)}");
            }
        }
    }
}

using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public class Reflector
    {
        public Reflector(Point center)
        {
            Center = center;
        }
        public Point Center;
    }

    public class Ring
    {
        public Ellipse Circle;
        public Point Center;
        public double Radius;
        public double OldRadius;
        public Brush Stroke;
        public double Speed;
        public double MinSize;
        public double MaxSize;
        public double Thickness = 7.0;
        public const double FinalThicknessMultiplier = 10.0;

        public Ring(Canvas parent, Point center, Brush stroke, double speed, double minSize, double maxSize)
        {
            Circle = new Ellipse()
            {
                Stroke = stroke,
                StrokeThickness = Thickness,
            };
            Center = center;
            Radius = minSize;
            Stroke = stroke;
            Speed = speed;
            MinSize = minSize;
            MaxSize = maxSize;

            parent.Children.Add(Circle);
            Update(0.0);
        }

        public void Update(double delta)
        {
            OldRadius = Radius;
            Radius += delta * Speed;
            if (Radius > MaxSize)
            {
                Radius = MinSize;
                OldRadius = Radius;
            }
            else if (Radius < MinSize)
            {
                Radius = MinSize;
                OldRadius = Radius;
            }
            Circle.Width = Radius*2.0;
            Circle.Height = Radius*2.0;
            Canvas.SetLeft(Circle, Center.X - Radius);
            Canvas.SetTop(Circle, Center.Y - Radius);

            var pct = ((Radius - MinSize) / (MaxSize - MinSize));
            Circle.Opacity = (1.0 - pct);
            Circle.StrokeThickness = Thickness + FinalThicknessMultiplier * (Thickness * pct); // will go from Thickness to 2x
        }
    }
    public class Reflection
    {
        public bool CycleComplete = false;
        public Path Arc;
        public ArcSegment ArcSegmentInternal;
        PathFigure PathFigureInternal;
        public Point Center;
        public double Radius;
        public Brush Stroke;
        public double Speed;
        public double MinSize;
        public double MaxSize;
        public double Thickness = 7.0;
        public const double FinalThicknessMultiplier = 10.0;

        public double AngleDegrees = -45.0; // TODO: need to calculate this
        const double AngleWidth = .45;

        private Point CreatePoint(double angleRadians, double radius)
        {
            return new Point(Math.Cos(angleRadians) * radius, Math.Sin(angleRadians) * radius);
        }
        public Reflection(Canvas parent, Point center, Point pointTo, Brush stroke, double speed, double minSize, double maxSize)
        {
            Radius = minSize;
            AngleDegrees = Math.Atan2(pointTo.Y - center.Y, pointTo.X - center.X) * 180.0 / Math.PI;
            var angleRadians = AngleDegrees * Math.PI / 180.0;
            Center = center;
            Stroke = stroke;
            Speed = speed;
            MinSize = minSize;
            MaxSize = maxSize;

            ArcSegmentInternal = new ArcSegment()
            {
                IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise,
                RotationAngle = AngleDegrees,
                Size = new Size(minSize*2.0, minSize*2.0),
                //Point = CreatePoint(angleRadians + AngleWidth, Radius),
            };
            PathFigureInternal = new PathFigure()
            {
                //StartPoint = CreatePoint(angleRadians - AngleWidth, Radius),
            };
            PathFigureInternal.Segments.Add(ArcSegmentInternal);
            var pg = new PathGeometry();
            pg.Figures.Add(PathFigureInternal);
            Arc = new Path()
            {
                Stroke = stroke,
                StrokeThickness = 2.0, //  Thickness,
                Data = pg,
            };



            parent.Children.Add(Arc);
            Canvas.SetLeft(Arc, Center.X - Radius);
            Canvas.SetTop(Arc, Center.Y - Radius);
            Update(0.0);
        }

        public void Update(double delta)
        {
            Radius += delta * Speed;
            if (Radius > MaxSize)
            {
                Radius = MinSize;
                CycleComplete = true;
            }
            else if (Radius < MinSize)
            {
                Radius = MinSize;
            }

            var angleRadians = AngleDegrees * Math.PI / 180.0;

            ArcSegmentInternal.Size = new Size(Radius, Radius); // size is x and y radius
            ArcSegmentInternal.Point = CreatePoint(angleRadians + AngleWidth, Radius);
            PathFigureInternal.StartPoint = CreatePoint(angleRadians - AngleWidth, Radius);

            var pct = ((Radius - MinSize) / (MaxSize - MinSize));
            Arc.Opacity = (1.0 - pct);
            Arc.StrokeThickness = 5; //TOOO:  Thickness + FinalThicknessMultiplier * (Thickness * pct); // will go from Thickness to 2x
        }
    }

    public class Radar
    {
        Point Center;
    }


    public sealed partial class ProgressRadar : UserControl
    {
        Timer AnimationTimer;
        List<Reflector> Reflectors = new List<Reflector>();
        List<Ring> CentralRings = new List<Ring>();
        List<Reflection> Reflections = new List<Reflection>();
        const int NRING = 5;

        public ProgressRadar()
        {
            this.InitializeComponent();
            this.Loaded += ProgressRadar_Loaded;
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

        private void UpdateRadar()
        {
            var now = DateTime.UtcNow;
            var delta = now.Subtract(LastUpdateTime).TotalSeconds;
            LastUpdateTime = now;

            var center = new Point(uiCanvas.ActualWidth / 2.0, uiCanvas.ActualHeight / 2.0); // Canvas doesn't have a size until it's displayed once.
            var maxRadius = Math.Max(uiCanvas.ActualWidth, uiCanvas.ActualHeight) / 2.0;

            // Time to add a new ring?
            bool addRing = false;
            addRing = CentralRings.Count == 0;
            var ringAddRadius = maxRadius / (double)NRING;
            if (CentralRings.Count > 0 && CentralRings.Count < NRING && CentralRings[CentralRings.Count - 1].Radius > ringAddRadius) addRing = true;

            if (Reflectors.Count == 0)
            {
                const int NReflectors = 7; // TODO: pick a good number
                double Distance = center.X / 4.0; // TODO: pick this better
                double deltaAngle = Math.PI * 2.0 / NReflectors;
                for (double angle = 0.0; angle < Math.PI * 2; angle += deltaAngle)
                {
                    Reflectors.Add(new Reflector(new Point(Distance * Math.Cos(angle) + center.X, Distance * Math.Sin(angle) + center.Y)));
                }

                for (double angle = -deltaAngle * 0.33; angle < Math.PI * 2; angle += deltaAngle)
                {
                    Reflectors.Add(new Reflector(new Point(2.0 * Distance * Math.Cos(angle) + center.X, 2.0 * Distance * Math.Sin(angle) + center.Y)));
                }
                for (double angle = -deltaAngle * 0.66; angle < Math.PI * 2; angle += deltaAngle)
                {
                    Reflectors.Add(new Reflector(new Point(3.0 * Distance * Math.Cos(angle) + center.X, 3.0 * Distance * Math.Sin(angle) + center.Y)));
                }
            }
            var rnd = new Random(); // TODO: make this static.


            if (addRing)
            {
                var brush = new RadialGradientBrush()
                {
                    MappingMode = BrushMappingMode.RelativeToBoundingBox,
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    FallbackColor = Colors.Green,
                };
                brush.GradientStops.Add(new GradientStop() { Color = Colors.DarkBlue, Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop() { Color = Colors.Blue, Offset = 0.75 });
                brush.GradientStops.Add(new GradientStop() { Color = Colors.LightBlue, Offset = 1.0 });

                CentralRings.Add(new Ring(uiCanvas, center, brush, 30.0, 2.0, maxRadius));
            }

            foreach (var ring in CentralRings)
            {
                ring.Update(delta);
            }

            // Add new reflections as needed.
            foreach (var ring in CentralRings)
            {
                foreach (var reflector in Reflectors)
                {
                    var d = Distance(ring, reflector);
                    if (d >= ring.OldRadius && d < ring.Radius)
                    {
                        var reflection = new Reflection(uiCanvas, reflector.Center, ring.Center, new SolidColorBrush(Colors.DarkGreen),30.0, 2.0, d);
                        Reflections.Add(reflection);
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

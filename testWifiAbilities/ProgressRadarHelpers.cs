using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace testWifiAbilities
{
    /// <summary>
    /// The "Ring" which is going from the central point outwards
    /// </summary>
    class Ring
    {
        public bool CycleComplete = false;
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

        Brush defaultBrush = null;
        private Brush GetDefaultBrush()
        {
            if (defaultBrush == null)
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
                defaultBrush = brush;
            }
            return defaultBrush;
        }
        const int RingZIndex = -3;
        public Ring(Canvas parent, Point center, Brush stroke, double speed, double minSize, double maxSize)
        {
            if (stroke == null) stroke = GetDefaultBrush();
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
            Canvas.SetZIndex(Circle, RingZIndex);
            Update(0.0, false);
        }

        public void Update(double delta, bool amStopping)
        {
            OldRadius = Radius;
            Radius += delta * Speed;
            if (Radius > MaxSize)
            {
                if (amStopping)
                {
                    CycleComplete = true;
                    Radius = 0;
                    Circle.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Radius = MinSize;
                    OldRadius = Radius;
                    //Circle.Visibility = Visibility.Collapsed;
                }
            }
            else if (Radius < MinSize)
            {
                Radius = MinSize;
                OldRadius = Radius;
                Circle.Visibility = Visibility.Collapsed;
            }
            Circle.Width = Radius * 2.0;
            Circle.Height = Radius * 2.0;
            Canvas.SetLeft(Circle, Center.X - Radius);
            Canvas.SetTop(Circle, Center.Y - Radius);

            var pct = ((Radius - MinSize) / (MaxSize - MinSize));
            Circle.Opacity = (1.0 - pct);
            Circle.StrokeThickness = Thickness + FinalThicknessMultiplier * (Thickness * pct); // will go from Thickness to 2x
        }

        public override string ToString()
        {
            return $"Ring: Radius={this.Radius} cycleComplete={CycleComplete} Max={this.MaxSize} Min={MinSize}";
        }
    }


    /// <summary>
    /// The graphical "bounce back" from the found or dummy APs.
    /// </summary>

    class Reflection
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

        const int ArcZIndex = -2;

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
                Size = new Size(minSize * 2.0, minSize * 2.0),
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
            Canvas.SetZIndex(Arc, ArcZIndex);
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

}

using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace WiFiRadarControl
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
                IsHitTestVisible = false,
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
            Reposition(Center); // keep the original center.

            var pct = ((Radius - MinSize) / (MaxSize - MinSize));
            Circle.Opacity = (1.0 - pct);
            Circle.StrokeThickness = Thickness + FinalThicknessMultiplier * (Thickness * pct); // will go from Thickness to 2x
        }

        public void Reposition(Point newCenter)
        {
            Center = newCenter; // might be the old center
            Circle.Width = Radius * 2.0;
            Circle.Height = Radius * 2.0;
            Canvas.SetLeft(Circle, Center.X - Radius);
            Canvas.SetTop(Circle, Center.Y - Radius);

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
        public Reflector CenterObject = null;
        public Ring PointToObject = null;
        public Path Arc;
        private ArcSegment ArcSegmentInternal;
        PathFigure PathFigureInternal;
        //public Point Center;
        public double Radius;
        public Brush Stroke;
        public double Speed;
        public double MinSize;
        public double MaxSize;
        public double Thickness = 7.0;
        public const double FinalThicknessMultiplier = 10.0;

        public double AngleDegrees = -45.0; // This is updated as needed
        const double AngleWidth = .45;

        const int ArcZIndex = -2;

        private Point CreatePoint(double angleRadians, double radius)
        {
            return new Point(Math.Cos(angleRadians) * radius, Math.Sin(angleRadians) * radius);
        }

        public void ResetPointTo()
        {
            AngleDegrees = Math.Atan2(PointToObject.Center.Y - CenterObject.Center.Y, PointToObject.Center.X - CenterObject.Center.X) * 180.0 / Math.PI;
            ArcSegmentInternal.RotationAngle = AngleDegrees;
        }
        public Reflection(Canvas parent, Reflector centerObject, Ring pointToObject, Brush stroke, double speed, double minSize, double maxSize)
        {
            Radius = minSize;
            CenterObject = centerObject;
            //Center = CenterObject.Center;
            PointToObject = pointToObject;
            AngleDegrees = Math.Atan2(PointToObject.Center.Y - CenterObject.Center.Y, PointToObject.Center.X - CenterObject.Center.X) * 180.0 / Math.PI;
            //var angleRadians = AngleDegrees * Math.PI / 180.0;
            Stroke = stroke;
            Speed = speed;
            MinSize = minSize;
            MaxSize = maxSize;

            ArcSegmentInternal = new ArcSegment()
            {
                IsLargeArc = false,
                SweepDirection = SweepDirection.Clockwise,
                Size = new Size(minSize * 2.0, minSize * 2.0),
            };
            ArcSegmentInternal.RotationAngle = AngleDegrees;
            PathFigureInternal = new PathFigure()
            {
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
            SetPosition();
            Canvas.SetZIndex(Arc, ArcZIndex);
            Update(0.0);
        }

        public void SetPosition()
        {
            Canvas.SetLeft(Arc, CenterObject.Center.X); // - Radius); Nope, don't offset by radius
            Canvas.SetTop(Arc, CenterObject.Center.Y); // - Radius); Nope, don't offset by radius
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


    static class RadarHelpers
    {
        public static void InitializeReflectorLocations(Canvas uiCanvas, ProgressRadar.RingLayoutData layoutData, List<Reflector> Reflectors)
        {
            if (Reflectors.Count == 0) return;

            var center = new Point(uiCanvas.ActualWidth / 2.0, (uiCanvas.ActualHeight / 2.0) - 10.0); // Canvas doesn't have a size until it's displayed once.
            var maxRadius = 0.0;
            var minRadius = 100.0;
            var minDistance = Math.Min(center.X, center.Y);
            if (minDistance < 300)
            {
                minRadius = 0;
                maxRadius = minDistance;
            }
            else
            {
                minRadius = 100;
                maxRadius = minDistance * 0.90;
            }
            var reflectorsPerRing = RadarHelpers.SetupRingList(Reflectors.Count, minRadius, maxRadius);
            int currRing = 0;
            //const int NReflectorsPerRing = Reflector.PreferredAPPerRing;
            var nPerRing = reflectorsPerRing[currRing];
            double deltaAngle = Math.PI * 2.0 / nPerRing;

            //var maxRings = Math.Ceiling ((double)Reflectors.Count / (double)nPerRing);
            var nRings = reflectorsPerRing.Count;
            var distanceDelta = (maxRadius - minRadius) / (nRings > 1 ? nRings - 1 : nRings);
            if (nRings == 1) minRadius = maxRadius;


            layoutData.NDrawnRings = nRings;
            layoutData.DrawnRingRadius = distanceDelta;
            layoutData.InnerRadius = minRadius;
            layoutData.OuterRadius = maxRadius;
            layoutData.CenterYOffset = 10;

            var distance = minRadius;
            var offsetAngle = -Math.PI / 2; // at the top
            int ringIndex = 0;
            for (int i = 0; i < Reflectors.Count; i++)
            {
                var reflector = Reflectors[i];

                double angle = ringIndex * deltaAngle + offsetAngle;
                reflector.Center = new Point(distance * Math.Cos(angle) + center.X, distance * Math.Sin(angle) + center.Y);

                ringIndex++;
                if (ringIndex >= nPerRing)
                {
                    ringIndex = 0;
                    offsetAngle -= deltaAngle / nRings; // actually a constant depending on the number of reflectors.
                    distance += distanceDelta;

                    currRing++;
                    if (currRing >= reflectorsPerRing.Count) currRing = reflectorsPerRing.Count - 1;
                    nPerRing = reflectorsPerRing[currRing];
                    deltaAngle = Math.PI * 2.0 / nPerRing;
                }
            }

            layoutData.NItemsInOuterRing = reflectorsPerRing[reflectorsPerRing.Count - 1];
        }
        /// <summary>
        /// Given a number of reflectors, returns a list of how many reflectors should be on each ring
        /// </summary>
        /// <param name="nitems"></param>
        /// <returns></returns>
        public static List<int> SetupRingList(int nitems, double minRadius, double maxRadius)
        {
            var retval = new List<int>();
            var remainder = nitems;
            var nextsize = Reflector.PreferredAPPerRing;
            while (remainder > 0)
            {
                retval.Add(nextsize);
                remainder -= nextsize;
                nextsize = nextsize * 3 / 2;
                if (remainder > 0 && nextsize > remainder)
                {
                    // Even out the number of items in the ring.
                    if (remainder < (nextsize / 2))
                    {
                        // There will be too many orphans. The current ring is the last ring.
                        if (retval.Count == 1)
                        {
                            //Log($"DBG: Distribute: OneRing={remainder}");
                            retval[retval.Count - 1] += remainder;
                            remainder = 0;
                        }
                        else
                        {
                            // if ring A has X items, ring A+1 has 1.5X items.
                            double perRing = 1.0;
                            double total = 0;
                            for (int i = 0; i < retval.Count; i++)
                            {
                                total += perRing;
                                perRing = perRing * 1.5;
                            }
                            perRing = perRing / 1.5; // is multiplied once too often.
                            // e.g., for 3 rings, total is 1 + 1.5 + 2.25 = 4.75
                            // Apply a big of algebra, if we have N items to distribute,
                            // then the the bottom ring gets (1*(N/4.75)), the second
                            // ring gets (1.5*(N/4.75)) and the last ring (2.25*(N/4.75))
                            // We will actually round up so the outer rings get more.
                            // total is the 4.75 and perRing is the 2.25
                            var k = (double)remainder / total;
                            var overage = 0.0;
                            for (int i = retval.Count - 1; i >= 0; i--)
                            {
                                var additional = Math.Ceiling(k * perRing - overage);
                                overage += additional - (k * perRing);
                                if (additional > remainder) additional = remainder;
                                retval[i] += (int)additional;
                                remainder -= (int)additional;
                                perRing = perRing * 2.0 / 3.0;
                                //Log($"DBG: Disribute: ring={i} additional={additional} overage={overage}");
                            }
                            //Log($"DBG: Disribute: complete remainder={remainder}");
                        }
                    }
                    nextsize = remainder;
                }
            }

            // Fixup sizes. Going outside to the inside, if an outer ring has fewer items then
            // an inner ring, swap them.
            for (int i = retval.Count - 1; i > 0; i--) // don't work on the last one
            {
                var outerval = retval[i];
                var innerval = retval[i - 1];
                if (outerval < innerval)
                {
                    retval[i] = innerval;
                    retval[i - 1] = outerval;
                }
                else if (outerval == innerval)
                {
                    retval[i] += 1;
                    retval[i - 1] -= 1;
                }
            }

            return retval;
        }

    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is
    /// to be used:
    /// xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf"
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is
    /// to be used:
    /// xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf;assembly=EmnExtensions.Wpf"
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    /// Right click on the target project in the Solution Explorer and
    /// "Add Reference"->"Projects"->[Browse to and select this project]
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    /// <MyNamespace:TickedLegendControl />
    /// </summary>
    public sealed class TickedLegendControl : FrameworkElement
    {
        readonly Typeface labelType;
        readonly double fontSize = 12.0 * 4.0 / 3.0;
        Pen tickPen;

        public Brush TickColor
        {
            set {
                tickPen = new(value, 1.5) {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                tickPen.Freeze();
                InvalidateVisual();
            }
            get => tickPen.Brush;
        }

        public TickedLegendControl()
        {
            TickColor = Brushes.Black;
            labelType = new(new("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new("Verdana"));
            //            StartVal = 0;
            //          EndVal = 1;
        }

        GraphControl watchedGraph;

        public GraphControl Watch
        {
            get => watchedGraph;
            set {
                if (watchedGraph != null) {
                    try {
                        watchedGraph.GraphBoundsUpdated -= OnGraphBoundsUpdated;
                    } catch {
                        //who cares if it doesn't work
                    }
                }

                watchedGraph = value;
                if (watchedGraph != null) {
                    watchedGraph.GraphBoundsUpdated += OnGraphBoundsUpdated;
                    TickColor = watchedGraph.GraphLineColor;
                    LegendLabel = (IsHorizontal ? watchedGraph.XLabel : watchedGraph.YLabel) ?? watchedGraph.Name;
                    OnGraphBoundsUpdated(watchedGraph, watchedGraph.GraphBounds);
                } else {
                    Visibility = Visibility.Collapsed;
                }
            }
        }

        void OnGraphBoundsUpdated(GraphControl graph, Rect newBounds)
        {
            if (graph != watchedGraph) {
                return; //shouldn't happen, but heck;
            }

            if (newBounds.IsEmpty) {
                StartVal = double.NaN;
                EndVal = double.NaN;
                Visibility = Visibility.Collapsed;
            } else {
                if (IsHorizontal) {
                    StartVal = newBounds.Left;
                    EndVal = newBounds.Right;
                } else {
                    StartVal = newBounds.Top; //these are inverted since WPF has 0 at the top, and we work with a flipped coordinate system!
                    EndVal = newBounds.Bottom;
                }

                Visibility = Visibility.Visible;
            }
        }

        double startVal, endVal;

        /// <summary>
        /// Left or bottom
        /// </summary>
        public double StartVal
        {
            get => startVal;
            set {
                startVal = value;
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Right or top
        /// </summary>
        public double EndVal
        {
            get => endVal;
            set {
                endVal = value;
                InvalidateVisual();
            }
        }

        string legendLabel = "test";

        public string LegendLabel
        {
            get => legendLabel;
            set {
                legendLabel = value;
                InvalidateVisual();
            }
        }

        public enum Side { Top, Right, Bottom, Left }

        Side snapTo = Side.Top;

        public Side SnapTo
        {
            get => snapTo;
            set {
                snapTo = value;
                InvalidateVisual();
            }
        }

        CultureInfo cachedCulture;
        int orderOfMagnitude, orderOfMagnitudeDiff; //TODO:really, these should be calculated in measure.  The measure pass should have everything measured!
        double pixelsPerDip = 1.0;

        bool IsHorizontal
            => snapTo == Side.Bottom || snapTo == Side.Top;

        protected override Size MeasureOverride(Size constraint)
        {
            if (Visibility == Visibility.Collapsed) {
                return new(0, 0);
            }
            pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            cachedCulture = CultureInfo.CurrentCulture;

            orderOfMagnitudeDiff = (int)Math.Floor(Math.Log10(Math.Abs(startVal - endVal)));
            orderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));
            var text = MakeText(8.88888888888888888);
            MakeLegendText(out _, out _, out _, out var labelWidth);

            if (IsHorizontal) {
                return new(constraint.Width.IsFinite() ? constraint.Width : labelWidth, Math.Max(constraint.Height.IsFinite() ? constraint.Height : 0, 17 + text.Height * 2));
            }

            return new(Math.Max(17 + text.Width + text.Height, constraint.Width.IsFinite() ? constraint.Width : 0), constraint.Height.IsFinite() ? constraint.Height : labelWidth);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Visibility != Visibility.Visible) {
                return;
            }

            var ticksPixelsDim = IsHorizontal ? ActualWidth : ActualHeight;

            if (ticksPixelsDim <= 0 ||
                !ticksPixelsDim.IsFinite() ||
                endVal == startVal ||
                !endVal.IsFinite() ||
                !startVal.IsFinite()) {
                return; // no point!
            }

            cachedCulture = CultureInfo.CurrentCulture;
            orderOfMagnitudeDiff = (int)Math.Floor(Math.Log10(Math.Abs(startVal - endVal)));
            orderOfMagnitude = (int)Math.Floor(Math.Log10(Math.Max(Math.Abs(startVal), Math.Abs(endVal))));

            var mirrTrans = Matrix.Identity; //top-left is 0,0, so if you're on the bottom you're happy
            if (snapTo == Side.Right || snapTo == Side.Bottom) {
                mirrTrans.ScaleAt(1.0, -1.0, 0.0, (ActualHeight + ActualWidth - ticksPixelsDim) / 2.0);
            }

            if (snapTo == Side.Right || snapTo == Side.Left) {
                mirrTrans.Rotate(-90.0);
                mirrTrans.Translate(0.0, ticksPixelsDim);
            }

            var textHMax = 0.0;
            //now mirrTrans projects from an "ideal" SnapTo-Top world-view to what we need.
            var geom = DrawTicks(
                ticksPixelsDim,
                (val, pixPos) => {
                    var text = MakeText(val / Math.Pow(10, orderOfMagnitude));
                    var textH = IsHorizontal ? text.Height : text.Width;
                    var altitudeCenter = 17 + textH / 2.0;
                    if (textH > textHMax) {
                        textHMax = textH;
                    }

                    var textPos = mirrTrans.Transform(new Point(pixPos, altitudeCenter));
                    //but we need to shift from center to top-left...
                    textPos.Offset(text.Width / -2.0, text.Height / -2.0);
                    drawingContext.DrawText(text, textPos);
                }
            );
            geom.Transform = new MatrixTransform(mirrTrans);

            drawingContext.DrawGeometry(null, tickPen, geom);

            MakeLegendText(out var baseL, out var powL, out var textL, out var totalLabelWidth);
            var centerPix = ticksPixelsDim / 2;

            if (!IsHorizontal) {
                textHMax += baseL.Height * 0.1;
            }

            var centerAlt = 17 + textHMax + baseL.Height / 2.0;

            var labelPos = mirrTrans.Transform(new Point(centerPix, centerAlt));

            if (snapTo == Side.Right) {
                drawingContext.PushTransform(new RotateTransform(90.0, labelPos.X, labelPos.Y));
            } else if (snapTo == Side.Left) {
                drawingContext.PushTransform(new RotateTransform(-90.0, labelPos.X, labelPos.Y));
            }

            labelPos.Offset(-totalLabelWidth / 2.0, -baseL.Height / 2.0);
            drawingContext.DrawText(baseL, labelPos);
            labelPos.Offset(baseL.Width, -0.1 * baseL.Height);
            drawingContext.DrawText(powL, labelPos);
            labelPos.Offset(powL.Width, 0.1 * baseL.Height);
            drawingContext.DrawText(textL, labelPos);

            if (snapTo == Side.Right || snapTo == Side.Left) {
                drawingContext.Pop();
            }
        }

        FormattedText MakeText(double val)
        {
            var numericValueString = val.ToString("g" + (orderOfMagnitude - orderOfMagnitudeDiff + 2));
            return new(numericValueString, cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black, pixelsPerDip);
        }

        void MakeLegendText(out FormattedText baseL, out FormattedText powL, out FormattedText textL, out double totalLabelWidth)
        {
            baseL = new("×10", cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black, pixelsPerDip);
            powL = new(orderOfMagnitude.ToString(), cachedCulture, FlowDirection.LeftToRight, labelType, fontSize * 0.8, Brushes.Black, pixelsPerDip);
            textL = new(" – " + legendLabel, cachedCulture, FlowDirection.LeftToRight, labelType, fontSize, Brushes.Black, pixelsPerDip);
            totalLabelWidth = baseL.Width + powL.Width + textL.Width;
        }

        StreamGeometry DrawTicks(double pixelsWide, Action<double, double> rank0ValAtPixel)
        {
            var scale = pixelsWide / (endVal - startVal);
            var geom = new StreamGeometry();
            using (var context = geom.Open()) {
                FindAllTicks(
                    pixelsWide / 100,
                    Math.Min(startVal, endVal),
                    Math.Max(startVal, endVal),
                    (val, rank) => {
                        var pixelPos = (val - startVal) * scale;

                        context.BeginFigure(new(pixelPos, 0), false, false);
                        context.LineTo(new(pixelPos, (4 - rank) * 4), true, false);

                        if (rank == 0) {
                            rank0ValAtPixel(val, pixelPos);
                        }
                    }
                );
            }

            return geom;
        }

        static void FindAllTicks(double preferredNum, double minVal, double maxVal, Action<double, int> foundTickWithRank)
        {
            CalcTickPositions(minVal, maxVal, preferredNum, out var firstTickAt, out var totalSlotSize, out var subDivTicks, out _);
            //we want the first tick to start before the range
            firstTickAt -= totalSlotSize;
            //convert subDivTicks into multiples:
            for (var i = subDivTicks.Length - 2; i >= 0; i--) {
                subDivTicks[i] = subDivTicks[i] * subDivTicks[i + 1];
            }

            var subSlotSize = totalSlotSize / subDivTicks[0];

            var firstSubTickMult = (int)Math.Ceiling((minVal - firstTickAt) / subSlotSize); //some positive number;

            for (var i = firstSubTickMult; firstTickAt + subSlotSize * i <= maxVal + subSlotSize * 0.00001; i++) {
                var rank = 0;
                while (rank < subDivTicks.Length && i % subDivTicks[rank] != 0) {
                    rank++;
                }

                foundTickWithRank(firstTickAt + subSlotSize * i, rank);
            }
        }

        /// <summary>
        /// Calculates optimal parameters for the placements of the legend
        /// </summary>
        /// <param name="minVal">the start of the range to be ticked</param>
        /// <param name="maxVal">the end of the range to be ticked</param>
        /// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 1.5 from that</param>
        /// <param name="firstTickAt">output: where the first tick should be placed</param>
        /// <param name="slotSize">output: the distance between consequetive ticks</param>
        /// <param name="ticks">
        /// output: the additional order of subdivisions each slot can be divided into.
        /// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
        /// and slightly less when the actual number of slots greater than requested.
        /// </param>
        /// <param name="orderOfMagnitude">the order of magnitude of the slot size</param>
        public static void CalcTickPositions(
            double minVal,
            double maxVal,
            double preferredNum,
            out double firstTickAt,
            out double slotSize,
            out int[] ticks,
            out int orderOfMagnitude)
        {
            var range = maxVal - minVal;
            var idealSlotSize = range / (preferredNum + 1);
            orderOfMagnitude = (int)Math.Floor(Math.Log10(idealSlotSize)); //i.e  for 143 or 971 this is 2
            var relSlotSize = idealSlotSize / Math.Pow(10, orderOfMagnitude); //some number between 1 and 10
            int fixedSlot;

            if (relSlotSize < 1.42) {
                fixedSlot = 1;
                ticks = new[] { 2, 5 };
            } else if (relSlotSize < 3.16) {
                fixedSlot = 2;
                ticks = relSlotSize < 2 ? new[] { 2, 2, 5 } : new[] { 2, 2 };
            } else if (relSlotSize < 7.07) {
                fixedSlot = 5;
                ticks = new[] { 5, 2 };
            } else {
                fixedSlot = 10;
                ticks = new[] { 2, 5, 2 };
            }

            slotSize = fixedSlot * Math.Pow(10, orderOfMagnitude);

            //now to find the first tick!

            firstTickAt = Math.Ceiling(minVal / slotSize) * slotSize;
        }
    }
}

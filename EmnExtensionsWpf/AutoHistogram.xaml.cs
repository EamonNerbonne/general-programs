using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Media.TextFormatting;
using System.Globalization;
using EmnExtensions.Algorithms;

namespace EmnExtensions.Wpf
{
    /// <summary>
    /// Interaction logic for AutoHistogram.xaml
    /// </summary>
    public partial class AutoHistogram : UserControl
    {
        public static DependencyProperty ValuesProperty =
DependencyProperty.Register("Values", typeof(IEnumerable<double>), typeof(AutoHistogram),
new FrameworkPropertyMetadata(null,
FrameworkPropertyMetadataOptions.AffectsRender,
new PropertyChangedCallback(ValuesSet))
);
        static void ValuesSet(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((AutoHistogram)d).RecalcHistogram();//TODO: is this necessary?  maybe implied by AffectsRender
        }

        public IEnumerable<double> Values {
            get { return (IEnumerable<double>)GetValue(ValuesProperty); }
            set { SetValue(ValuesProperty, value); }
        }

        public static DependencyProperty BucketSizeProperty =
DependencyProperty.Register("BucketSize", typeof(int), typeof(AutoHistogram),
new FrameworkPropertyMetadata(0,
FrameworkPropertyMetadataOptions.AffectsRender,
new PropertyChangedCallback(BucketSizeSet))
);
        static void BucketSizeSet(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((AutoHistogram)d).RecalcHistogram();//TODO: is this necessary?  maybe implied by AffectsRender
        }
        public int BucketSize {
            get { return (int)GetValue(BucketSizeProperty); }
            set { SetValue(BucketSizeProperty, value); }
        }


        private void RecalcHistogram() {
            if (Values == null || BucketSize == 0) return;
            current = new Histogrammer(Values, BucketSize, Math.Max((int)ActualWidth, 10));

            DrawGraph();

            InvalidateVisual();
        }

        private void DrawGraph() {
            if (current == null) return;
            DrawGraphLine();
            lowerLegend.Background = DrawLegend(current.minVal, current.maxVal, densityCanvas.ActualWidth, lowerLegend.ActualHeight, false);
            var vis = DrawLegend(0, current.MaximumDensity, densityCanvas.ActualHeight, leftLegend.ActualWidth, true);
            leftLegend.Background = vis;
            leftLegend.Clip = null;

            lowerLegend.InvalidateVisual();
            leftLegend.InvalidateVisual();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            DrawGraph();
            base.OnRenderSizeChanged(sizeInfo);
        }
        private void DrawGraphLine() {
            PathGeometry geom = new PathGeometry();
            PathFigure graph = new PathFigure();
            bool isFirst = true;
            double xScale = densityCanvas.ActualWidth / (current.maxVal - current.minVal);
            double yScale = densityCanvas.ActualHeight / current.MaximumDensity;

            foreach (var datapoint in current.GenerateHistogram()) {
                Point p = new Point {
                    X = xScale * (datapoint.point - current.minVal),
                    Y = densityCanvas.ActualHeight - datapoint.density * yScale
                };
                if (isFirst) {
                    graph.StartPoint = p;
                    isFirst = false;
                } else {
                    graph.Segments.Add(new LineSegment(p, true));
                }
            }
            geom.Figures.Add(graph);
            geom.Freeze();
            Path path = new Path {
                Stroke = Brushes.Black,
                StrokeThickness = 1.0,
                Data = geom

            };

            densityCanvas.Children.Clear();
            densityCanvas.Children.Add(path);
        }

        object syncroot = new object();
        Histogrammer current;

        static Pen graphLinePen = new Pen(Brushes.Black, 1.0);
        static AutoHistogram() {
            graphLinePen.Freeze();
        }

        private static VisualBrush DrawLegend(double minVal, double maxVal, double viewWidth, double legendSize, bool rotateLeft) {
            double firstTickAt, totalSlotSize;
            int[] subDivTicks;
            int orderOfMagnitude;
            FindTickPositions(minVal, maxVal, viewWidth / 100, out firstTickAt, out totalSlotSize, out subDivTicks, out orderOfMagnitude);
            if (totalSlotSize * 4 < viewWidth) orderOfMagnitude++;
            double magnitudeCorr;
            if (orderOfMagnitude < -2 || orderOfMagnitude > 2)
                magnitudeCorr = Math.Pow(10, orderOfMagnitude);
            else {
                orderOfMagnitude = 0;
                magnitudeCorr = 1;
            }
            double scale = viewWidth / (maxVal - minVal);
            CultureInfo cachedCulture = CultureInfo.CurrentCulture;
            Typeface labelType = new Typeface(new FontFamily("Calibri"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal, new FontFamily("Verdana"));

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                if (rotateLeft) drawingContext.PushTransform(new TransformGroup() {
                    Children = new TransformCollection() {
                    new RotateTransform(-90),
                    //new TranslateTransform(0, 70),
                }
                });  // new RotateTransform(-90.0,densityCanvas.ActualHeight/2,densityCanvas.ActualHeight/2);
                drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, viewWidth + legendSize * 2, legendSize)));
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, viewWidth + legendSize * 2, legendSize));
                PathGeometry legendGeom = new PathGeometry();

                Action<double, int> DrawTick = null;
                DrawTick = (double val, int rank) => {

                    for (int subrank = rank; subrank < subDivTicks.Length; subrank++) {
                        double subSlotSize = totalSlotSize / subDivTicks.Take(subrank + 1).Aggregate(1, (i, j) => i * j);
                        for (int i = 1; i < subDivTicks[subrank]; i++)
                            DrawTick(val + subSlotSize * i, subrank + 1);
                    }

                    if (val < minVal || val > maxVal) return;

                    double pos = legendSize + scale * (val - minVal);
                    double ticklen = 30 / (rank + 2);

                    double lineTopPos = rotateLeft ? legendSize : 0;
                    double lineBotPos = rotateLeft ? legendSize - ticklen : ticklen;
                    Point lineTop = new Point(pos, lineTopPos);
                    Point lineBot = new Point(pos, lineBotPos);
                    legendGeom.Figures.Add(new PathFigure(lineTop, new[] { new LineSegment(lineBot, true) }, false));

                    if (rank == 0) {
                        string numericValueString = (val / Math.Pow(10, orderOfMagnitude)).ToString("f1");
                        FormattedText label = new FormattedText(numericValueString, cachedCulture, FlowDirection.LeftToRight, labelType, 10 * 4.0 / 3.0, Brushes.Black);
                        drawingContext.DrawText(label, new Point(pos - label.Width / 2, rotateLeft ? legendSize - label.Height - ticklen - 1 : ticklen + 1));
                        //                    drawingContext.DrawRectangle(null, graphLinePen, new Rect(pos - label.Width / 2 - 2, ticklen - 2+5, label.Width + 4, label.Height + 4));
                    }
                };

                for (double majorTick = firstTickAt - totalSlotSize; majorTick < maxVal; majorTick += totalSlotSize) {
                    DrawTick(majorTick, 0);
                }
                drawingContext.DrawGeometry(null, graphLinePen, legendGeom);
                FormattedText maglabel = new FormattedText(
                    string.Format("×10", orderOfMagnitude),
                    cachedCulture, FlowDirection.LeftToRight, labelType, 10 * 4.0 / 3.0, Brushes.Black);
                FormattedText magpowlabel = new FormattedText(
                    orderOfMagnitude.ToString(),
                    cachedCulture, FlowDirection.LeftToRight, labelType, 8 * 4.0 / 3.0, Brushes.Black);

                drawingContext.DrawText(maglabel, new Point(viewWidth + 2 * legendSize - maglabel.Width - magpowlabel.Width - 3, rotateLeft ? legendSize - maglabel.Height - 16 : 16));

                drawingContext.DrawText(magpowlabel, new Point(viewWidth + 2 * legendSize - magpowlabel.Width - 3, rotateLeft ? legendSize - magpowlabel.Height - 20 : 14));


                drawingContext.Close();
            }
            return new VisualBrush(drawingVisual) { Stretch = Stretch.None, TileMode = TileMode.None, AlignmentX = AlignmentX.Right, AlignmentY = AlignmentY.Top };
        }

        public AutoHistogram() {
            InitializeComponent();
        }

        /// <summary>
        /// Calculates optimal parameters for the placements of the legend
        /// </summary>
        /// <param name="minVal">the start of the range to be ticked</param>
        /// <param name="maxVal">the end of the range to be ticked</param>
        /// <param name="preferredNum">the preferred number of labelled ticks.  This method will deviate by at most a factor 1.5 from that</param>
        /// <param name="firstTickAt">output: where the first tick should be placed</param>
        /// <param name="slotSize">output: the distance between consequetive ticks</param>
        /// <param name="ticks">output: the additional order of subdivisions each slot can be divided into.
        /// This value aims to have around 10 subdivisions total, slightly more when the actual number of slots is fewer than requested
        /// and slightly less when the actual number of slots greater than requested.</param>
        public static void FindTickPositions(double minVal, double maxVal, double preferredNum, out double firstTickAt, out double slotSize, out int[] ticks, out int orderOfMagnitude) {
            double range = maxVal - minVal;
            double idealSlotSize = range / (preferredNum + 1);
            orderOfMagnitude = (int)Math.Log10(idealSlotSize); //i.e  for 143 or 971 this is 100
            double relSlotSize = idealSlotSize / Math.Pow(10, orderOfMagnitude); //some number between 1 and 10
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

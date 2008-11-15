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

namespace EmnExtensions.Wpf
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmnExtensions.Wpf;assembly=EmnExtensions.Wpf"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:GraphControl/>
    ///
    /// </summary>
    public class GraphControl : FrameworkElement
    {
        static Random GraphColorRandom = new Random();
        static Brush RandomGraphColor() {
            double r, g, b, sum;
            r = GraphColorRandom.NextDouble() + 0.01;
            g = GraphColorRandom.NextDouble() + 0.01;
            b = GraphColorRandom.NextDouble() + 0.01;
            sum = GraphColorRandom.NextDouble() * 0.5 + 0.5;
            SolidColorBrush brush = new SolidColorBrush(
                new Color {
                    A = (byte)255,
                    R = (byte)(255 * r * sum / (r + g + b)),
                    G = (byte)(255 * g * sum / (r + g + b)),
                    B = (byte)(255 * b * sum / (r + g + b)),
                }
                );
            brush.Freeze();
            return brush;
        }


        protected override Size MeasureOverride(Size constraint) {
            return new Size(
                constraint.Width.IsFinite() ? constraint.Width : 150,
                constraint.Height.IsFinite() ? constraint.Height : 150
                );
        }
        public event Action<GraphControl, Rect> GraphBoundsUpdated;

        Rect oldBounds = Rect.Empty;
        Rect graphBoundsPrivate = Rect.Empty;
        public Rect GraphBounds { //TODO dependency property?
            get {
                return graphBoundsPrivate;
            }
            set {
                graphBoundsPrivate = value;
                UpdateBounds();
            }
        }

        string xLabel;
        public string XLabel { get { return xLabel; } set { xLabel = value; InvalidateVisual(); } }
        string yLabel;
        public string YLabel { get { return yLabel; } set { yLabel = value; InvalidateVisual(); } }

        public void EnsurePointInBounds(Point p) {
            graphBoundsPrivate.Union(p);
            UpdateBounds();
        }

        Size lastDispSize = Size.Empty;
        void UpdateBounds() {
            Size curSize = new Size(ActualWidth, ActualHeight);
            if (oldBounds == graphBoundsPrivate && curSize== lastDispSize) 
                return;
            lastDispSize = curSize;

            Matrix translateThenScale = Matrix.Identity;
            //we first translate since that's just easier
            translateThenScale.Translate(-graphBoundsPrivate.Location.X, -graphBoundsPrivate.Location.Y);
            //now we scale the graph to the appropriate dimensions
            translateThenScale.Scale(ActualWidth / graphBoundsPrivate.Width, ActualHeight / graphBoundsPrivate.Height);
            //then we flip the graph vertically around the viewport middle since in our graph positive is up, not down.
            translateThenScale.ScaleAt(1.0, -1.0, 0.0, ActualHeight / 2.0);
            graphGeom2.Transform = new MatrixTransform(translateThenScale);


            InvalidateVisual();
            if (oldBounds == graphBoundsPrivate)
                return;
            oldBounds = graphBoundsPrivate;

            if (GraphBoundsUpdated != null) GraphBoundsUpdated(this, graphBoundsPrivate);

        }


        Pen graphLinePen;
        public Brush GraphLineColor {
            set {
                graphLinePen = new Pen(value, 1.5);
                graphLinePen.StartLineCap = PenLineCap.Round;
                graphLinePen.EndLineCap = PenLineCap.Round;
                graphLinePen.LineJoin = PenLineJoin.Round;
                graphLinePen.Freeze();
                InvalidateVisual();
            }
            get {
                return graphLinePen.Brush;
            }
        }

        //List<Point> points;
        PathGeometry graphGeom2;
//        StreamGeometry graphGeom;
        PathFigure fig=null;
       // bool needUpdate = false;
        public void NewLine(IEnumerable<Point> lineOfPoints) {
            InvalidateVisual();
            graphBoundsPrivate = Rect.Empty;
            if (lineOfPoints == null) {
                graphGeom2 = null;
                fig = null;
            } else {
                var points = lineOfPoints.ToArray();
                graphGeom2 = new PathGeometry();
                foreach (Point startPoint in points.Take(1)) {
                    fig = new PathFigure();
                    fig.StartPoint = startPoint;
                    graphGeom2.Figures.Add(fig);
                    graphBoundsPrivate.Union(startPoint);
                }
                foreach (Point point in points.Skip(1)) {
                    fig.Segments.Add(new LineSegment(point, true));
                    graphBoundsPrivate.Union(point);
                }
                //note that graphGeom.Bounds are in view-space, i.e. calling these without a Transformation should result in the same Bounds...
            }
            UpdateBounds();
        }

        public IEnumerable<Point> CurrentPoints {
            get {
                if (fig == null) yield break;
                yield return fig.StartPoint;
                foreach (LineSegment lineTo in fig.Segments)
                    yield return lineTo.Point;
            }
        }

        public void AddPoint(Point point) {
            if (graphGeom2 == null) NewLine(new[] { point });
            else {
                if (fig == null) {
                    fig = new PathFigure();
                    fig.StartPoint = point;
                    graphGeom2.Figures.Add(fig);
                } else {
                    fig.Segments.Add(new LineSegment(point, true));
                }
                graphBoundsPrivate.Union(point);
                InvalidateVisual();
                if (GraphBoundsUpdated != null)
                    GraphBoundsUpdated(this, graphBoundsPrivate);
            }
        }

        public GraphControl() {
            GraphLineColor = RandomGraphColor();
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (graphGeom2 == null) return;
            UpdateBounds();
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            drawingContext.DrawGeometry(null, graphLinePen, graphGeom2);
        }

    }
}

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
            double r, g, b, max,min,minV,maxV;
			max = GraphColorRandom.NextDouble() * 0.5 + 0.5;
			min = GraphColorRandom.NextDouble() * 0.5;
			r = GraphColorRandom.NextDouble();
            g = GraphColorRandom.NextDouble();
            b = GraphColorRandom.NextDouble();
			maxV = Math.Max(r, Math.Max(g, b));
			minV = Math.Min(r, Math.Min(g, b));
			r = (r - minV) / (maxV - minV) * (max - min) + min;
			g = (g - minV) / (maxV - minV) * (max - min) + min;
			b = (b - minV) / (maxV - minV) * (max - min) + min;
			if (r + g + b > 1.5) {
				double scale = 1.5 / (r + g + b);
				r *= scale; g *= scale; b *= scale;
			}
			SolidColorBrush brush = new SolidColorBrush(
                new Color {
                    A = (byte)255,
                    R = (byte)(255 * r + 0.5),
					G = (byte)(255 * g + 0.5),
					B = (byte)(255 * b + 0.5),
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
            if (!(graphBoundsPrivate.Height.IsFinite()&&graphBoundsPrivate.Width.IsFinite()) || (oldBounds == graphBoundsPrivate && curSize== lastDispSize)) 
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
		public Pen GraphPen {
			set {
				graphLinePen = value;
				graphLinePen.Freeze();
				InvalidateVisual();
			}
			get {
				return graphLinePen;
			}
		}
        public Brush GraphLineColor {
			set {
				Pen newPen= graphLinePen.CloneCurrentValue();
				newPen.Brush = value;
				GraphPen = newPen;
			}
			get {
                return graphLinePen.Brush;
            } 
		}
		public double PenThickness {
			get {
				return graphLinePen.Thickness;
			}
			set {
				Pen newPen = graphLinePen.CloneCurrentValue();
				newPen.Thickness = value;
				GraphPen = newPen;
			}

		}
		public static Pen MakeDefaultPen(bool randomColor) {
			var newPen = new Pen(randomColor?RandomGraphColor(): Brushes.Black, 1.0);
			newPen.StartLineCap = PenLineCap.Round;
			newPen.EndLineCap = PenLineCap.Round;
			newPen.LineJoin = PenLineJoin.Round;
			return newPen;
		}

        PathGeometry graphGeom2;
        // StreamGeometry graphGeom;
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

		public void NewLine(PathGeometry newGeom) {
			InvalidateVisual();
			newGeom.Transform = Transform.Identity;

			graphBoundsPrivate = newGeom.Bounds;
			graphGeom2 = newGeom;
			fig = null;
			UpdateBounds();
		}

		public PathGeometry LineGeometry {
			get { return graphGeom2; }
			set { NewLine(value); }
		}


		public static PathGeometry LineWithErrorBars(Point[] lineOfPoints, double[] ErrBars) {
			PathGeometry geom = new PathGeometry();
			PathFigure fig=null;
			foreach (Point startPoint in lineOfPoints.Take(1)) {
				fig = new PathFigure();
				fig.StartPoint = startPoint;
				geom.Figures.Add(fig);
			}
			foreach (Point point in lineOfPoints.Skip(1)) {
				fig.Segments.Add(new LineSegment(point, true));
			}
			Rect bounds = geom.Bounds;
			double errWidth = bounds.Width / 200.0;
			for (int i = 0; i < lineOfPoints.Length; i++) {
				if (ErrBars[i].IsFinite()) {
					PathFigure errf = new PathFigure();
					errf.StartPoint = lineOfPoints[i] + new Vector(-errWidth, -ErrBars[i]);
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(errWidth, -ErrBars[i]),true));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(0, -ErrBars[i]), false));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(0, ErrBars[i]), true));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(errWidth, ErrBars[i]), false));
					errf.Segments.Add(new LineSegment(lineOfPoints[i] + new Vector(-errWidth, ErrBars[i]), true));
					geom.Figures.Add(errf);
				}
			}

			return geom;
		}

		private static bool IsOK(Point p) {		return p.X.IsFinite() && p.Y.IsFinite();		}
		public static PathGeometry Line(Point[] lineOfPoints) {
			PathGeometry geom = new PathGeometry();
			PathFigure fig = null;
			foreach (Point startPoint in lineOfPoints.SkipWhile(p=>!IsOK(p)).Take(1)) {
				fig = new PathFigure();
				fig.StartPoint = startPoint;
				geom.Figures.Add(fig);
			}
			bool wasOK = true;
			foreach (Point point in lineOfPoints.SkipWhile(p => !IsOK(p)).Skip(1)) {
				if (IsOK(point)) {
					fig.Segments.Add(new LineSegment(point, wasOK));
					wasOK = true;
				} else {
					wasOK = false;
				}
			}
			return geom;
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
			GraphPen = MakeDefaultPen(true);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (graphGeom2 == null) return;
            UpdateBounds();
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            drawingContext.DrawGeometry(null, graphLinePen, graphGeom2);
        }

    }
}

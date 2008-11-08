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
        static Pen graphLinePen;
        static GraphControl() {
            graphLinePen = new Pen(Brushes.Red, 1.0);
            graphLinePen.StartLineCap = PenLineCap.Round;
            graphLinePen.EndLineCap = PenLineCap.Round;
            graphLinePen.LineJoin = PenLineJoin.Round;
            graphLinePen.Freeze();
        }

        protected override Size MeasureOverride(Size constraint) {
            return new Size(
                constraint.Width.IsFinite()?constraint.Width:150,
                constraint.Height.IsFinite()?constraint.Height:150
                );
        }

        Rect graphBoundsPrivate = Rect.Empty;
        public Rect GraphBounds { //TODO dependency property?
            get {
                return graphBoundsPrivate;
            }
            set {
                graphBoundsPrivate = value;

                InvalidateVisual();
            }
        }

        Point[] points;
        StreamGeometry graphGeom;
        public void ShowLine(IEnumerable<Point> lineOfPoints) {
            InvalidateVisual ();
            if (lineOfPoints == null) {
                graphGeom = null;
                points = null;
            } else {
                points = lineOfPoints.ToArray();
                graphGeom = new StreamGeometry();
                Rect newBounds = Rect.Empty;
                using (StreamGeometryContext context = graphGeom.Open()) {
                    foreach (Point startPoint in points.Take(1)) {
                        context.BeginFigure(startPoint, false, false);
                        newBounds.Union(startPoint);
                    }
                    foreach (Point point in points.Skip(1)) {
                        context.LineTo(point, true, true);
                        newBounds.Union(point);
                    }
                }
                //note that graphGeom.Bounds are in view-space, i.e. calling these without a Transformation should result in the same Bounds...
                GraphBounds = newBounds;
            }
        }

        public GraphControl() {
            graphBoundsPrivate = new Rect(new Point(0, 0), new Point(1, 1));
            ShowLine(new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) });
        }

        protected override void OnRender(DrawingContext drawingContext) {
            // base.OnRender(drawingContext); // not necessary...
            
            Matrix translateThenScale = Matrix.Identity;
            //we first translate since that's just easier
            translateThenScale.Translate(-graphBoundsPrivate.Location.X, -graphBoundsPrivate.Location.Y);
            //now we scale the graph to the appropriate dimensions
            translateThenScale.Scale(ActualWidth / graphBoundsPrivate.Width, ActualHeight / graphBoundsPrivate.Height);
            //then we flip the graph vertically around the viewport middle since in our graph positive is up, not down.
            translateThenScale.ScaleAt(1.0, -1.0, 0.0, ActualHeight / 2.0);
            graphGeom.Transform = new MatrixTransform( translateThenScale);
            

            drawingContext.DrawGeometry(null, graphLinePen, graphGeom);
        }

    }
}

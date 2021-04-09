using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf.Plot;

namespace EmnExtensions.Wpf.OldGraph
{
    /// <summary>
    /// Interaction logic for PlotControl.xaml
    /// </summary>
    public partial class PlotControl
    {
        bool needRedrawGraphs;
        bool needRecomputeBounds;
        bool showGridLines;
        public ObservableCollection<GraphableData> Graphs { get; } = new();

        public PlotControl()
        {
            Graphs.CollectionChanged += graphs_CollectionChanged;
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(dg, BitmapScalingMode.Linear);
            Background = new DrawingBrush(dg) {
                Stretch = Stretch.None, //No stretch since we want the ticked axis to determine stretch
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new(0, 0, 0, 0), //we want to to start displaying in the corner 0,0 - and width+height are irrelevant due to Stretch.None.
                AlignmentX = AlignmentX.Left, //and corner 0,0 is in the Top-Left!
                AlignmentY = AlignmentY.Top,
            };
        }

        void RegisterChanged(IEnumerable<GraphableData> newGraphs)
        {
            foreach (var newgraph in newGraphs) {
                newgraph.Changed += graphChanged;
            }
        }

        void UnregisterChanged(IEnumerable<GraphableData> oldGraphs)
        {
            foreach (var oldgraph in oldGraphs) {
                oldgraph.Changed -= graphChanged;
            }
        }

        void graphs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) {
                UnregisterChanged(e.OldItems.Cast<GraphableData>());
            }

            if (e.NewItems != null) {
                RegisterChanged(e.NewItems.Cast<GraphableData>());
            }

            RequireRedisplay();
        }

        void RequireRedisplay()
        {
            needRecomputeBounds = true;
            InvalidateMeasure(); //todo; flag and invalidatemeasure always together?

            needRedrawGraphs = true;
            InvalidateVisual(); //todo; flag and InvalidateVisual always together?
        }

        void graphChanged(GraphableData graph, GraphChange graphChange)
        {
            if (graphChange == GraphChange.Drawing) {
                needRedrawGraphs = true;
                InvalidateVisual();
            } else if (graphChange == GraphChange.Labels || graphChange == GraphChange.Projection) {
                needRecomputeBounds = true;
                InvalidateMeasure();
            }
        }

        IEnumerable<TickedAxis> Axes
            => new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop };

        public bool? AttemptBorderTicks
        {
            set {
                if (value.HasValue) {
                    foreach (var axis in Axes) {
                        axis.AttemptBorderTicks = value.Value;
                    }
                }
            }
            get {
                var vals = Axes.Select(axis => axis.AttemptBorderTicks).Distinct().ToArray();
                return vals.Length != 1 ? null : vals[0];
            }
        }

        #region Static Helper Functions
        static IEnumerable<TickedAxisLocation> ProjectionCorners
        {
            get {
                yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph;
                yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
                yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.LeftOfGraph;
                yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.RightOfGraph;
            }
        }

        static DimensionBounds ToDimBounds(Rect bounds, bool isHorizontal)
            => isHorizontal ? DimensionBounds.FromRectX(bounds) : DimensionBounds.FromRectY(bounds);

        static DimensionMargins ToDimMargins(Thickness margins, bool isHorizontal)
            => isHorizontal ? DimensionMargins.FromThicknessX(margins) : DimensionMargins.FromThicknessY(margins);

        static TickedAxisLocation ChooseProjection(GraphableData graph)
            => ProjectionCorners.FirstOrDefault(corner => (graph.AxisBindings & corner) == corner);
        #endregion

        void RecomputeBounds()
        {
            Trace.WriteLine("RecomputeBounds");
            foreach (var axis in Axes) {
                var boundGraphs = Graphs.Where(graph => (graph.AxisBindings & axis.AxisPos) != 0);
                var bounds =
                    boundGraphs
                        .Select(graph => ToDimBounds(graph.DataBounds, axis.IsHorizontal))
                        .Aggregate(DimensionBounds.Empty, (bounds1, bounds2) => DimensionBounds.Merge(bounds1, bounds2));
                var margin =
                    boundGraphs
                        .Select(graph => ToDimMargins(graph.Margin, axis.IsHorizontal))
                        .Aggregate(DimensionMargins.Empty, (m1, m2) => DimensionMargins.Merge(m1, m2));
                var dataUnits = string.Join(", ", Graphs.Select(graph => axis.IsHorizontal ? graph.XUnitLabel : graph.YUnitLabel).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());

                axis.DataBound = bounds;
                axis.DataMargin = margin;
                axis.DataUnits = dataUnits;
            }

            needRecomputeBounds = false;
        }

        void RedrawGraphs(TickedAxisLocation gridLineAxes)
        {
            Trace.WriteLine("Redrawing Graphs");
            using (var drawingContext = dg.Open()) {
                RedrawScene(drawingContext, gridLineAxes);
            }

            needRedrawGraphs = false;
        }

        public bool ShowGridLines
        {
            get => showGridLines;
            set {
                if (showGridLines != value) {
                    showGridLines = value;
                    needRedrawGraphs = true;
                    InvalidateVisual();
                }
            }
        }

        void RedrawScene(DrawingContext drawingContext, TickedAxisLocation gridLineAxes)
        {
            if (showGridLines) {
                foreach (var axis in Axes) {
                    if ((axis.AxisPos & gridLineAxes) != 0) {
                        drawingContext.DrawDrawing(axis.GridLines);
                    }
                }
            }

            foreach (var graph in Graphs.AsEnumerable().Reverse()) {
                graph.DrawGraph(drawingContext);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (needRecomputeBounds) {
                RecomputeBounds();
            }

            return base.MeasureOverride(constraint);
        }

        readonly DrawingGroup dg = new();

        protected override void OnRender(DrawingContext drawingContext)
        {
            Trace.WriteLine("PlotControl.OnRender");
            //axes which influence projection matrices:
            var relevantAxes = Graphs.Aggregate(TickedAxisLocation.None, (axisLoc, graph) => axisLoc | ChooseProjection(graph));
            var transforms =
                from axis in Axes
                where (axis.AxisPos & relevantAxes) != 0
                select new {
                    axis.AxisPos,
                    Transform = axis.DataToDisplayTransform,
                    HorizontalClip = axis.IsHorizontal ? axis.DisplayClippingBounds : DimensionBounds.Empty,
                    VerticalClip = axis.IsHorizontal ? DimensionBounds.Empty : axis.DisplayClippingBounds,
                };

            var cornerProjection =
                ProjectionCorners
                    .Where(corner => corner == (corner & relevantAxes))
                    .ToDictionary( //we have only relevant corners...
                        corner => corner,
                        corner => transforms.Where(transform => transform.AxisPos == (transform.AxisPos & corner))
                            .Aggregate(
                                (t1, t2) => new {
                                    AxisPos = t1.AxisPos | t2.AxisPos,
                                    Transform = t1.Transform * t2.Transform,
                                    HorizontalClip = DimensionBounds.Merge(t1.HorizontalClip, t2.HorizontalClip),
                                    VerticalClip = DimensionBounds.Merge(t1.VerticalClip, t2.VerticalClip),
                                }
                            )
                    );

            foreach (var graph in Graphs) {
                var trans = cornerProjection[ChooseProjection(graph)];
                var bounds = new Rect(new(trans.HorizontalClip.Start, trans.VerticalClip.Start), new Point(trans.HorizontalClip.End, trans.VerticalClip.End));
                graph.SetTransform(trans.Transform, bounds);
            }

            foreach (var axis in Axes) {
                axis.SetGridLineExtent(RenderSize);
            }

            if (needRedrawGraphs) {
                RedrawGraphs(relevantAxes);
            }

            //    drawingContext.DrawDrawing(dg);
            base.OnRender(drawingContext);
        }
    }
}

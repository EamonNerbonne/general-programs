//select one of three rendering methods:
#define RENDER_VIA_BG_BRUSH //benchmarked: 20.2 sec or with MAKEDV: 23.5
//#define RENDER_VIA_ONRENDER //benchmarked:24.3 sec
//#define RENDER_VIA_CHILD_CONTROL //benchmarked:23.5

#if RENDER_VIA_BG_BRUSH
//#define MAKEDV //fastest without
#endif

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

namespace EmnExtensions.Wpf.Plot
{
	/// <summary>
	/// Interaction logic for PlotControl.xaml
	/// </summary>
	public partial class PlotControl : UserControl
	{
		bool needRedrawGraphs = false;
		bool needRecomputeBounds = false;
		List<GraphableData> graphs = new List<GraphableData>();
		Dictionary<TickedAxisLocation, TickedAxis> axes;
		public PlotControl() {
			InitializeComponent();
			axes = new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop }.ToDictionary(axis => axis.AxisPos);
		}

		public void AddPlot(GraphableData newgraph) {
			graphs.Add(newgraph);
			needRecomputeBounds = true;
			needRedrawGraphs = true;
			InvalidateVisual();
		}

		private IEnumerable<TickedAxis> Axes { get { return new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop }; } }

		#region Static Helper Functions
		private static IEnumerable<TickedAxisLocation> ProjectionCorners {
			get {
				yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.LeftOfGraph;
				yield return TickedAxisLocation.BelowGraph | TickedAxisLocation.RightOfGraph;
				yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.LeftOfGraph;
				yield return TickedAxisLocation.AboveGraph | TickedAxisLocation.RightOfGraph;
			}
		}
		private static DimensionBounds ToDimBounds(Rect bounds, bool isHorizontal) { return isHorizontal ? DimensionBounds.FromRectX(bounds) : DimensionBounds.FromRectY(bounds); }
		private static DimensionMargins ToDimMargins(Thickness margins, bool isHorizontal) { return isHorizontal ? DimensionMargins.FromThicknessX(margins) : DimensionMargins.FromThicknessY(margins); }
		private static TickedAxisLocation ChooseProjection(GraphableData graph) { return ProjectionCorners.FirstOrDefault(corner => (graph.AxisBindings & corner) == corner); }
		#endregion

		private void RecomputeBounds() {
			foreach (TickedAxis axis in Axes) {
				var boundGraphs = graphs.Where(graph => (graph.AxisBindings & axis.AxisPos) != TickedAxisLocation.None);
				DimensionBounds bounds =
					boundGraphs
					.Select(graph => ToDimBounds(graph.DataBounds, axis.IsHorizontal))
					.Aggregate(DimensionBounds.Undefined, (bounds1, bounds2) => DimensionBounds.Merge(bounds1, bounds2));
				DimensionMargins margin =
					boundGraphs
					.Select(graph => ToDimMargins(graph.Margin, axis.IsHorizontal))
					.Aggregate(DimensionMargins.Undefined, (m1, m2) => DimensionMargins.Merge(m1, m2));
				string dataUnits = string.Join(", ", graphs.Select(graph => axis.IsHorizontal ? graph.XUnitLabel : graph.YUnitLabel).Distinct().ToArray());

				axis.DataBound = bounds;
				axis.DataMargin = margin;
				axis.DataUnits = dataUnits;
			}
			needRecomputeBounds = false;
		}
		private void RedrawGraphs() {
#if RENDER_VIA_BG_BRUSH||RENDER_VIA_CHILD_CONTROL
#if MAKEDV
			DrawingVisual dv = new DrawingVisual();
			using (var drawingContext = dv.RenderOpen()) {
#else
			DrawingGroup bg = new DrawingGroup();
			using (var drawingContext = bg.Open()) {
#endif
				RedrawScene(drawingContext);
			}
#if RENDER_VIA_BG_BRUSH
			this.Background =
#if MAKEDV
				new VisualBrush(dv) {
#else
 new DrawingBrush(bg) {
#endif
	 Stretch = Stretch.None, //No stretch since we want the ticked axis to determine stretch
	 ViewboxUnits = BrushMappingMode.Absolute,
	 Viewbox = new Rect(0, 0, 0, 0), //we want to to start displaying in the corner 0,0 - and width+height are irrelevant due to Stretch.None.
	 AlignmentX = AlignmentX.Left, //and corner 0,0 is in the Top-Left!
	 AlignmentY = AlignmentY.Top,
 };
#endif
#if RENDER_VIA_CHILD_CONTROL
			graphDisp.GraphDrawing = bg;
			graphDisp.Visibility = Visibility.Visible;
#endif
#endif
			needRedrawGraphs = false;
		}

		private void RedrawScene(DrawingContext drawingContext) {
			foreach (var axis in Axes)
				drawingContext.DrawDrawing(axis.GridLines);
			foreach (var graph in graphs) 
				graph.DrawGraph(drawingContext);
		}

		#region Wpf Layout Overrides
		protected override Size MeasureOverride(Size constraint) {
			if (needRecomputeBounds) RecomputeBounds();
			return base.MeasureOverride(constraint);
		}

		protected override void OnRender(DrawingContext drawingContext) {
			if (needRedrawGraphs) RedrawGraphs();
			base.OnRender(drawingContext);
#if RENDER_VIA_ONRENDER
		        RedrawScene(drawingContext);
#endif

			//axes which influence projection matrices:
			TickedAxisLocation relevantAxes = graphs.Aggregate(TickedAxisLocation.None, (axisLoc, graph) => axisLoc | ChooseProjection(graph));

			var transforms = Axes
				.Where(axis => (axis.AxisPos & relevantAxes) != TickedAxisLocation.None)
				.Select(axis => new { AxisPos = axis.AxisPos, Transform = axis.DataToDisplayTransform });

			var cornerProjection = (from corner in ProjectionCorners
									where corner == (corner & relevantAxes)
									select corner
								   ).ToDictionary(//we have only relevant corners...
										corner => corner,
										corner => (from transform in transforms
												   where transform.AxisPos == (transform.AxisPos & corner)
												   select transform.Transform
												  ).Aggregate((mat1, mat2) => mat1 * mat2)
												 );

			foreach (var graph in graphs)
				graph.SetTransform(cornerProjection[ChooseProjection(graph)]);
			foreach (var axis in Axes)
				axis.SetGridLineExtent(RenderSize);


		}
		#endregion

	}
}

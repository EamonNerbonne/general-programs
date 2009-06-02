#define ASBG //benchmarked: 20.2 sec or with MAKEDV: 23.5
//#define INRENDER //benchmarked:24.3 sec
//#define GRAPHDISP //benchmarked:23.5

#if ASBG||GRAPHDISP
#define MAKEDRAWING
#endif

#if ASBG
//#define MAKEDV //fastest without
#endif
#if GRAPHDISP
#define MAKEDV
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
		bool redrawGraphs = false;
		List<GraphableData> graphs = new List<GraphableData>();
		Dictionary<TickedAxisLocation, TickedAxis> axes;
		public PlotControl() {
			InitializeComponent();
			axes = new[] { tickedAxisLft, tickedAxisBot, tickedAxisRgt, tickedAxisTop }.ToDictionary(axis => axis.AxisPos);
		}

		public void AddPlot(GraphableData newgraph) {
			graphs.Add(newgraph);
			tickedAxisBot.DataBound = DimensionBounds.Merge(tickedAxisBot.DataBound, DimensionBounds.FromRectX(newgraph.DataBounds));
			tickedAxisBot.DataUnits = newgraph.XUnitLabel;
			tickedAxisBot.DataMargin = DimensionMargins.Merge(tickedAxisBot.DataMargin, DimensionMargins.FromThicknessX(newgraph.Margin));

			tickedAxisLft.DataBound = DimensionBounds.Merge(tickedAxisLft.DataBound, DimensionBounds.FromRectY(newgraph.DataBounds));
			tickedAxisLft.DataUnits = newgraph.YUnitLabel;
			tickedAxisLft.DataMargin = DimensionMargins.Merge(tickedAxisLft.DataMargin, DimensionMargins.FromThicknessY(newgraph.Margin));

			redrawGraphs = true;
			InvalidateVisual();
		}

#if INRENDER
		private void MakeDrawing() { }
#endif

#if MAKEDRAWING
		private void MakeDrawing() {
#if MAKEDV
			DrawingVisual dv = new DrawingVisual();
			using (var drawingContext = dv.RenderOpen()) {
#else
			DrawingGroup bg = new DrawingGroup();
			using (var drawingContext = bg.Open()) {
#endif
				foreach (var graph in graphs) {
					graph.DrawGraph(drawingContext);
				}
			}
#if ASBG

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
#if GRAPHDISP
			graphDisp.GraphDrawing = dv;
#endif
		}
#endif


		protected override void OnRender(DrawingContext drawingContext) {
			MakeDrawing();
			base.OnRender(drawingContext);
			var projection = LftBot();
			foreach (var graph in graphs) {
#if INRENDER
		        graph.DrawGraph(drawingContext);
#endif
				graph.SetTransform(projection);
			}

		}


		Matrix LftBot() { return tickedAxisBot.DataToDisplayTransform * tickedAxisLft.DataToDisplayTransform; }
	}
}

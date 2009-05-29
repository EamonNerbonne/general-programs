//#define ASBG
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
		public PlotControl() {
			InitializeComponent();
		}
		List<GraphableData> graphs = new List<GraphableData>();

		public void AddPlot(GraphableData newgraph) {
			graphs.Add(newgraph);
			tickedAxisBot.DataBound = DimensionBounds.Merge(tickedAxisBot.DataBound, new DimensionBounds { Min = newgraph.DataBounds.Left, Max = newgraph.DataBounds.Right });
			tickedAxisLft.DataBound = DimensionBounds.Merge(tickedAxisLft.DataBound, new DimensionBounds { Min = newgraph.DataBounds.Top, Max = newgraph.DataBounds.Bottom });
			tickedAxisLft.DataUnits = newgraph.YUnitLabel;
			tickedAxisBot.DataUnits = newgraph.XUnitLabel;
			tickedAxisBot.DataMargin = DimensionMargins.Merge(tickedAxisBot.DataMargin, new DimensionMargins { AtStart = newgraph.Margin.Left, AtEnd = newgraph.Margin.Right });
			tickedAxisLft.DataMargin = DimensionMargins.Merge(tickedAxisLft.DataMargin, new DimensionMargins { AtStart = newgraph.Margin.Top, AtEnd = newgraph.Margin.Bottom });
#if ASBG
			DrawingGroup bg = new DrawingGroup();
			using (var drawingContext = bg.Open()) {
				foreach (var graph in graphs) {
					graph.DrawGraph(drawingContext);
				}
			}
			this.Background = new DrawingBrush(bg) { Stretch = Stretch.None, ViewboxUnits =  BrushMappingMode.Absolute };
#endif
		}

#if ASBG
		protected override Size ArrangeOverride(Size arrangeBounds) {
			Size retval = base.ArrangeOverride(arrangeBounds);
			var projection = LftBot();
			foreach (var graph in graphs) {
				graph.SetTransform(projection);
			}
			if (Background != null) ((DrawingBrush)Background).Viewbox = new Rect(0, 0, retval.Width, retval.Height);
			return retval;
		}
#else
		protected override void OnRender(DrawingContext drawingContext) {
		    var projection = LftBot();
		    foreach (var graph in graphs) {
		        graph.SetTransform(projection);
		        graph.DrawGraph(drawingContext);
		    }
		    base.OnRender(drawingContext);
		}
#endif

		Matrix LftBot() { return tickedAxisBot.DataToDisplayTransform * tickedAxisLft.DataToDisplayTransform; }
	}
}

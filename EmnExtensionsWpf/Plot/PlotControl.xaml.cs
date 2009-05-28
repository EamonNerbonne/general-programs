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

		public void AddPlot(GraphableData graph) {
			graphs.Add(graph);
			tickedAxisBot.DataBound = DimensionBounds.Merge(tickedAxisBot.DataBound, new DimensionBounds { Min = graph.DataBounds.Left, Max = graph.DataBounds.Right });
			tickedAxisLft.DataBound = DimensionBounds.Merge(tickedAxisLft.DataBound, new DimensionBounds { Min = graph.DataBounds.Top, Max = graph.DataBounds.Bottom });
			tickedAxisLft.DataUnits = graph.YUnitLabel;
			tickedAxisBot.DataUnits = graph.XUnitLabel;
			tickedAxisBot.DataMargin = DimensionMargins.Merge(tickedAxisBot.DataMargin, new DimensionMargins { AtStart = graph.Margin.Left, AtEnd = graph.Margin.Right });
			tickedAxisLft.DataMargin = DimensionMargins.Merge(tickedAxisLft.DataMargin, new DimensionMargins { AtStart = graph.Margin.Top, AtEnd = graph.Margin.Bottom });
		}

		Matrix LftBot() { return tickedAxisBot.DataToDisplayTransform * tickedAxisLft.DataToDisplayTransform; }

		protected override void OnRender(DrawingContext drawingContext) {
			foreach (var graph in graphs) {
				graph.SetTransform(LftBot());
				graph.DrawGraph(drawingContext);
			}
			base.OnRender(drawingContext);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Wpf.Plot
{
	public interface IPlotView
	{
		string XUnitLabel { get; }
		string YUnitLabel { get; }
		string DataLabel { get; }
		object Tag { get; }

		event Action<IPlotView, GraphChange> Changed;
		TickedAxisLocation AxisBindings { get; set; }
		IPlotViz PlotVisualizer { get; }
	}
}

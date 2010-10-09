using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot {
	public enum PlotClass { Auto, PointCloud, Line }

	public interface IPlot {
		string XUnitLabel { get; }
		string YUnitLabel { get; }
		string DataLabel { get; }
		Drawing SampleDrawing { get; }
		object Tag { get; }
		int ZIndex { get; }

		event Action<IPlot, GraphChange> Changed;
		TickedAxisLocation AxisBindings { get; set; }
		IPlotWithViz PlotVisualizer { get; }

		void TriggerChange(GraphChange changeType);

		Rect? OverrideBounds { get;  }
		Rect? MinimalBounds { get;  }
        
		Color? RenderColor { get; set; }
		double? RenderThickness { get; }
		bool VizSupportsColor { get; }
		bool VizSupportsThickness { get; }
		void TriggerDataChanged();
	}

	public interface IPlotWriteable<in T> : IPlot {
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new object Tag { get; set; }
		new int ZIndex { get; set; }
		new Rect? OverrideBounds { get; set; }
		new Rect? MinimalBounds { get; set; }
		new double? RenderThickness { get; set; }

		IVizEngine<T> Visualizer { get;  }
		T Data { set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot {
	public enum PlotClass { Auto, PointCloud, Line }

	public interface IPlotContainer {
		Dispatcher Dispatcher { get; }
		void GraphChanged(IPlot plot, GraphChange changeType);
	}

	public interface IPlot {
		string XUnitLabel { get; }
		string YUnitLabel { get; }
		string DataLabel { get; }
		Drawing SampleDrawing { get; }
		int ZIndex { get; }

		IPlotContainer Container { set; }
		TickedAxisLocation AxisBindings { get; set; }

		void TriggerChange(GraphChange changeType);

		Rect? OverrideBounds { get; }
		Rect? MinimalBounds { get; }

		Color? RenderColor { get; set; }
		double? RenderThickness { get; }
		IVizEngine Visualizer { get; }

		DispatcherOperation TriggerDataChanged();
	}

	public interface IPlotWriteable<in T> : IPlot {
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new int ZIndex { get; set; }
		new Rect? OverrideBounds { get; set; }
		new Rect? MinimalBounds { get; set; }
		new double? RenderThickness { get; set; }
		new IPlotContainer Container { get; set; }

		object Tag { get; set; }

		/// <summary>
		/// changes the data being graphed.  The data is mapped in the current thread, then, if necessary, graphed on another;
		/// If a thread change was necessary, the appropriate DispatcherOperation object is return, otherwise null.
		/// </summary>
		DispatcherOperation SetData(T newData);
	}

	public static class PlotExtensions {
		public static Rect EffectiveDataBounds(this IPlot plot) { return plot.OverrideBounds ?? Rect.Union(plot.Visualizer.DataBounds, plot.MinimalBounds ?? Rect.Empty); }
	}
}

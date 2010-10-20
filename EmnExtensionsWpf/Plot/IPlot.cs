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

	public interface IPlotMetaData {
		string XUnitLabel { get; }
		string YUnitLabel { get; }
		string DataLabel { get; }
		int ZIndex { get; }

		IPlot Plot { set; }
		TickedAxisLocation AxisBindings { get; set; }

		void TriggerChange(GraphChange changeType);

		Rect? OverrideBounds { get; }
		Rect? MinimalBounds { get; }

		Color? RenderColor { get; set; }
		double? RenderThickness { get; }
		Dispatcher Dispatcher { get; }
	}

	public interface IPlotMetaDataWriteable : IPlotMetaData {
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new int ZIndex { get; set; }
		new Rect? OverrideBounds { get; set; }
		new Rect? MinimalBounds { get; set; }
		new double? RenderThickness { get; set; }
		new IPlot Plot { get; set; }
		object Tag { get; set; }
	}


	public interface IPlot {
		IPlotContainer Container { set; }
		IPlotMetaData MetaData { get; }
		IVizEngine Visualisation { get; }
		void GraphChanged(GraphChange changeType);
	}

	public static class PlotExtensions {
		public static Rect EffectiveDataBounds(this IPlot plot) { return plot.MetaData.OverrideBounds ?? Rect.Union(plot.Visualisation.DataBounds, plot.MetaData.MinimalBounds ?? Rect.Empty); }
		public static IVizEngine<TIn> Map<TOut, TIn>(this IVizEngine<TOut> impl, Func<TIn, TOut> map) { return new VizEngines.VizMapped<TIn, TOut>(impl, map); }
		public static DispatcherOperation BeginDataChange<T>(this IDataSink<T> sink, T data) { return sink.Dispatcher.BeginInvoke((Action<T>)sink.ChangeData, data); }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public interface IPlotViewOnly
	{
		string XUnitLabel { get; }
		string YUnitLabel { get; }
		string DataLabel { get; }
		object Tag { get; }

		event Action<IPlotViewOnly, GraphChange> Changed;
		TickedAxisLocation AxisBindings { get; set; }
		IPlotWithViz PlotVisualizer { get; }
	}
	public enum PlotClass { Auto, PointCloud, Line }

	public interface IPlot : IPlotViewOnly
	{
		Rect? OverrideBounds { get; }
		PlotClass PlotClass { get; }
		Color? RenderColor { get; }
		double? RenderThickness { get; }
		void TriggerChange(GraphChange changeType);
	}

	public interface IPlotWithSettings : IPlot
	{
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new Rect? OverrideBounds { get; set; }
		new PlotClass PlotClass { get; set; }
		new object Tag { get; set; }
		new Color? RenderColor { get; set; }
		new double? RenderThickness { get; set; }
		bool VizSupportsColor { get; }
		bool VizSupportsThickness { get; }
		void TriggerDataChanged();
	}

	public interface IPlotWriteable<T> : IPlotWithSettings
	{
		IVizEngine<T> Visualizer { get; set; }
		Func<T, PlotClass, IVizEngine<T>> ChooseVisualizer { get; set; }
		T Data { get; set; }
	}
}

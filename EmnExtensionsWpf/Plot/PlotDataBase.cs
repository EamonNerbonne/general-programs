using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{

	public interface IPlotVisualizerFactory<in T>
	{
		IVizEngine<T> ChooseVisualizer(T data, PlotClass plotClass);
	}

	class PlotDataImplementation<T> : IPlotControl<T>
	{
		public static IVizEngine<T> DefaultChooser(T data, PlotClass plotClass) { return (IVizEngine<T>)new VizEngines.VizNone(); }
		public event Action<IPlotView, GraphChange> Changed;
		internal protected void TriggerChange(GraphChange changeType) { if (Changed != null) Changed(this, changeType); }
		void IPlot.TriggerChange(GraphChange changeType) { TriggerChange(changeType); }

		string m_xUnitLabel, m_yUnitLabel, m_DataLabel;
		public string XUnitLabel { get { return m_xUnitLabel; } set { if (m_xUnitLabel != value) { m_xUnitLabel = value; TriggerChange(GraphChange.Labels); } } }
		public string YUnitLabel { get { return m_yUnitLabel; } set { if (m_yUnitLabel != value) { m_yUnitLabel = value; TriggerChange(GraphChange.Labels); } } }
		public string DataLabel { get { return m_DataLabel; } set { if (m_DataLabel != value) { m_DataLabel = value; TriggerChange(GraphChange.Labels); } } }

		TickedAxisLocation m_axisBindings = TickedAxisLocation.Default;
		public TickedAxisLocation AxisBindings { get { return m_axisBindings; } set { if (m_axisBindings != value) { m_axisBindings = value; TriggerChange(GraphChange.Projection); } } }

		Rect? m_OverrideBounds;
		public Rect? OverrideBounds { get { return m_OverrideBounds; } set { if (m_OverrideBounds != value) { m_OverrideBounds = value; TriggerChange(GraphChange.Projection); } } }

		public object Tag { get; set; }

		PlotClass m_PlotClass;
		public PlotClass PlotClass { get { return m_PlotClass; } set { if (m_PlotClass != value) { m_PlotClass = value; vizEngine = null; TriggerChange(GraphChange.Drawing); } } }

		IVizEngine<T> vizEngine;
		public IVizEngine<T> Visualizer
		{
			get { 
				if(vizEngine == null)
					Visualizer = ChooseVisualizer(Data, PlotClass);
				return vizEngine;
				}
			set { vizEngine = value; vizEngine.Owner = this; vizEngine.DataChanged(Data); TriggerChange(GraphChange.Drawing); }
		}
		IPlotViz IPlotView.PlotVisualizer { get { return PlotViz.Wrap(this, Data, Visualizer); } }

		/// <summary>
		/// Called to construct a visualizer whenever one is necessary an none currently is set (i.e. when needing to measure or render the graph and Visualizer == null).
		/// This should return a value
		/// </summary>
		public Func<T, PlotClass, IVizEngine<T>> ChooseVisualizer { get; set; }

		T m_Data;
		public T Data { get { return m_Data; } set { m_Data = value; if (vizEngine != null) vizEngine.DataChanged(Data); } }

		public PlotDataImplementation(T data = default(T)) { ChooseVisualizer = DefaultChooser;  Data = data; }
	}

	public static class PlotData
	{
		public static  IVizEngine<Point[]> PointArrayVisualizers(Point[] data, PlotClass plotClass)
		{
			if (plotClass == PlotClass.Line)
				return new VizEngines.VizLineSegments();
			else
				return new VizEngines.VizPixelScatterSmart();
		}

		public static IPlotControl<Point[]> Create(Point[] Data) { return new PlotDataImplementation<Point[]>(Data) { ChooseVisualizer = PointArrayVisualizers }; }
	}
}

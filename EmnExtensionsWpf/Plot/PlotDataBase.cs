using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public enum PlotClass { Auto, PointCloud, Line }

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

	public interface IPlot<out T> : IPlotView
	{
		Rect? OverrideBounds { get; }
		PlotClass PlotClass { get; }
		T Data { get; }
		void TriggerChange(GraphChange changeType);
	}

	public interface IPlotControl<T> : IPlot<T>
	{
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new Rect? OverrideBounds { get; set; }
		new PlotClass PlotClass { get; set; }
		new object Tag { get; set; }
		new T Data { get; set; }
	}

	public interface IPlotVisualizerFactory<in T>
	{
		IPlotViz<T> ChooseVisualizer(T data, PlotClass plotClass);
	}

	class PlotDataImplementation<T, TVizFactory> : IPlotControl<T> where TVizFactory : IPlotVisualizerFactory<T>, new()
	{
		public event Action<IPlotView, GraphChange> Changed;
		internal protected void TriggerChange(GraphChange changeType) { if (Changed != null) Changed(this, changeType); }
		void IPlot<T>.TriggerChange(GraphChange changeType) { TriggerChange(changeType); }

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

		IPlotViz<T> vizEngine;
		public IPlotViz<T> Visualizer
		{
			get { EnsureEngineExists(); return vizEngine; }
			set { vizEngine = value; TriggerChange(GraphChange.Drawing); }
		}
		IPlotViz IPlotView.PlotVisualizer { get { return Visualizer; } }


		void EnsureEngineExists()
		{
			if (vizEngine == null)
			{
				var vizFactory = new TVizFactory();
				var newVizEngine = vizFactory.ChooseVisualizer(Data, PlotClass);
				newVizEngine.SetOwner(this);
				vizEngine = newVizEngine;
				TriggerDataChange();
			}
		}

		T m_Data;
		public T Data { get { return m_Data; } set { m_Data = value; TriggerDataChange(); } }
		private void TriggerDataChange() { if (vizEngine != null) vizEngine.DataChanged(m_Data); }
		public PlotDataImplementation(T data = default(T)) { Data = data; }
	}

	public static class PlotData
	{

		class FacPointArr : IPlotVisualizerFactory<Point[]>
		{
			public FacPointArr() { }
			public IPlotViz<Point[]> ChooseVisualizer(Point[] data, PlotClass plotClass)
			{
				if (plotClass == PlotClass.Line)
					throw new NotImplementedException();
				else
					return new VizEngines.VizPixelScatterBitmap();
			}
		}


		//public static IPlotControl<T> Create<T>(T Data)
		//{
		//    return new PlotDataImplementation<Point[], FacPointArr>();
		//}
		public static IPlotControl<Point[]> Create(Point[] Data) { return new PlotDataImplementation<Point[], FacPointArr>(Data); }

	}
}

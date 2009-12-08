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

	class PlotDataImplementation<T, TVizFactory> : IPlotControl<T> where TVizFactory : IPlotVisualizerFactory<T>, new()
	{
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
			get { EnsureEngineExists(); return vizEngine; }
			set { vizEngine = value; TriggerChange(GraphChange.Drawing); }
		}
		IPlotViz IPlotView.PlotVisualizer { get { return PlotViz.Wrap(this, Data, Visualizer); } }


		void EnsureEngineExists()
		{
			if (vizEngine == null)
			{
				var vizFactory = new TVizFactory();
				var newVizEngine = vizFactory.ChooseVisualizer(Data, PlotClass);
				vizEngine = newVizEngine;
				TriggerDataChange();
			}
		}

		T m_Data;
		public T Data { get { return m_Data; } set { m_Data = value; TriggerDataChange(); } }
		private void TriggerDataChange() { if (vizEngine != null) vizEngine.DataChanged(Data); }
		public PlotDataImplementation(T data = default(T)) { Data = data; }
	}

	public static class PlotData
	{

		class FacPointArr : IPlotVisualizerFactory<Point[]>
		{
			public FacPointArr() { }
			public IVizEngine<Point[]> ChooseVisualizer(Point[] data, PlotClass plotClass)
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

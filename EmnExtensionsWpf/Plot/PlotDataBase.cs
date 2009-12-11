using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf.Plot
{
	class PlotDataImplementation<T> : IPlotWriteable<T>
	{
		public static IVizEngine<T> DefaultChooser(T data, PlotClass plotClass) { return (IVizEngine<T>)new VizEngines.VizNone(); }
		public event Action<IPlotViewOnly, GraphChange> Changed;
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

		Color? m_PrimaryColor;
		public Color? RenderColor { get { return m_PrimaryColor; } set { m_PrimaryColor = value; if (vizEngine != null) vizEngine.RenderOptionsChanged(); } }

		double? m_Thickness;
		public double? RenderThickness { get { return m_Thickness; } set { m_Thickness = value; if (vizEngine != null) vizEngine.RenderOptionsChanged(); } }

		IVizEngine<T> vizEngine;
		public IVizEngine<T> Visualizer
		{
			get
			{
				if (vizEngine == null)
					Visualizer = ChooseVisualizer(Data, PlotClass);
				return vizEngine;
			}
			set { vizEngine = value; vizEngine.Owner = this; vizEngine.DataChanged(Data); TriggerChange(GraphChange.Drawing); }
		}
		IPlotWithViz IPlotViewOnly.PlotVisualizer { get { return PlotViz.Wrap(this, Data, Visualizer); } }

		/// <summary>
		/// Called to construct a visualizer whenever one is necessary an none currently is set (i.e. when needing to measure or render the graph and Visualizer == null).
		/// This should return a value
		/// </summary>
		public Func<T, PlotClass, IVizEngine<T>> ChooseVisualizer { get; set; }

		T m_Data;
		public T Data { get { return m_Data; } set { m_Data = value; TriggerDataChanged(); TriggerChange(GraphChange.Drawing); } } //TODO: workaround for graphchange.drawing...
		public void TriggerDataChanged() { if (vizEngine != null) vizEngine.DataChanged(Data); }

		public PlotDataImplementation(T data = default(T)) { ChooseVisualizer = DefaultChooser; Data = data; }
		public bool VizSupportsColor { get { return Visualizer.SupportsColor; } }
		public bool VizSupportsThickness { get { return Visualizer.SupportsThickness; } }
	}

	public static class PlotData
	{
		public static IVizEngine<Point[]> PointArrayVisualizers(Point[] data, PlotClass plotClass)
		{
			if (plotClass == PlotClass.Line)
				return new VizEngines.VizLineSegments();
			else
				return new VizEngines.VizPixelScatterSmart();
		}

		public static IPlotWriteable<Point[]> Create(Point[] Data) { return new PlotDataImplementation<Point[]>(Data) { ChooseVisualizer = PointArrayVisualizers }; }

		public static IPlotWriteable<T> Create<T>(T Data, Action<WriteableBitmap, Matrix, int, int, T> displayFunc, Func<T, Rect> boundsFunc = null)
		{
			var visualizer = new VizEngines.VizDelegateBitmap<T> { UpdateBitmapDelegate = displayFunc };
			if (boundsFunc != null) visualizer.ComputeBounds = boundsFunc;
			return new PlotDataImplementation<T>(Data) { Visualizer = visualizer };
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EmnExtensions.Wpf.Plot {
	class PlotDataImplementation<T> : IPlotWriteable<T> {
		public static IVizEngine<T> DefaultChooser(T data, PlotClass plotClass) { return (IVizEngine<T>)new VizEngines.VizNone(); }
		public event Action<IPlot, GraphChange> Changed;
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

		Rect? m_MinimalBounds;
		public Rect? MinimalBounds { get { return m_MinimalBounds; } set { if (m_MinimalBounds != value) { m_MinimalBounds = value; TriggerChange(GraphChange.Projection); } } }

		public object Tag { get; set; }

		Color? m_PrimaryColor;
		public Color? RenderColor { get { return m_PrimaryColor; } set { m_PrimaryColor = value; vizEngine.RenderOptionsChanged(); } }

		int zIndex;
		public int ZIndex { get { return zIndex; } set { zIndex = value; TriggerChange(GraphChange.Drawing); } }

		public Drawing SampleDrawing { get { return Visualizer.SampleDrawing; } }

		double? m_Thickness;
		public double? RenderThickness { get { return m_Thickness; } set { m_Thickness = value; vizEngine.RenderOptionsChanged(); } }

		readonly IVizEngine<T> vizEngine;
		public IVizEngine<T> Visualizer { get { return vizEngine; } }

		IPlotWithViz IPlot.PlotVisualizer { get { return PlotViz.Wrap(this, Data, Visualizer); } }

		/// <summary>
		/// Called to construct a visualizer whenever one is necessary an none currently is set (i.e. when needing to measure or render the graph and Visualizer == null).
		/// This should return a value
		/// </summary>
		public Func<T, PlotClass, IVizEngine<T>> ChooseVisualizer { get; private set; }

		T m_Data;
		public T Data { get { return m_Data; } set { m_Data = value; TriggerDataChanged(); TriggerChange(GraphChange.Drawing); } } //TODO: workaround for graphchange.drawing...
		public void TriggerDataChanged() { vizEngine.DataChanged(Data); }

		public PlotDataImplementation(IVizEngine<T> vizualizer, T data = default(T)) { vizualizer.Owner = this; vizEngine = vizualizer; Data = data; }
		public bool VizSupportsColor { get { return Visualizer.SupportsColor; } }
		public bool VizSupportsThickness { get { return Visualizer.SupportsThickness; } }
	}

	public static class PlotData {
		public static IPlotWriteable<Point[]> Create(Point[] Data, PlotClass plotClass) {
			return new PlotDataImplementation<Point[]>(plotClass == PlotClass.Line ? (IVizEngine<Point[]>)new VizEngines.VizLineSegments() : new VizEngines.VizPixelScatterSmart(), Data);
		}

		public static IPlotWriteable<T> Create<T>(T Data, Action<WriteableBitmap, Matrix, int, int, T> displayFunc, Func<T, Rect> boundsFunc = null) {
			var visualizer = new VizEngines.VizDelegateBitmap<T> { UpdateBitmapDelegate = displayFunc };
			if (boundsFunc != null) visualizer.ComputeBounds = boundsFunc;
			return new PlotDataImplementation<T>(visualizer, Data);
		}

		public static IPlotWriteable<T> Create<T>(IVizEngine<T> viz) {
			return new PlotDataImplementation<T>(viz);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot {
	class PlotDataImplementation<T, TRender> : IPlotWriteable<T> {

		readonly object containerSync = new object();
		private IPlotContainer container;

		public IPlotContainer Container {
			get { lock (containerSync) return container; }
			set { lock (containerSync) container = value; }
		}

		internal protected void TriggerChange(GraphChange changeType) { if (Container != null) Container.GraphChanged(this, changeType); }
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
		public Color? RenderColor { get { return m_PrimaryColor; } set { m_PrimaryColor = value; vizEngine.OnRenderOptionsChanged(); } }

		int zIndex;
		public int ZIndex { get { return zIndex; } set { zIndex = value; TriggerChange(GraphChange.Drawing); } }

		public Drawing SampleDrawing { get { return Visualizer.SampleDrawing; } }

		double? m_Thickness;
		public double? RenderThickness { get { return m_Thickness; } set { m_Thickness = value; vizEngine.OnRenderOptionsChanged(); } }

		readonly IVizEngine<TRender> vizEngine;
		public IVizEngine Visualizer { get { return vizEngine; } }
		public Func<T, TRender> Map { get; private set; }

		T m_Data;
		public T Data { get { return m_Data; } } //TODO: workaround for graphchange.drawing...
		public DispatcherOperation SetData(T data) {
			m_Data = data;
			return TriggerDataChanged();
		}

		public DispatcherOperation TriggerDataChanged() {
			TRender mappedData = Map(Data);
			lock (containerSync) {
				if (Container == null || Container.Dispatcher.CheckAccess()) {
					vizEngine.DataChanged(mappedData);
					return null;
				} else
					return Container.Dispatcher.BeginInvoke((Action)(() => { vizEngine.DataChanged(mappedData); }));
			}
		}

		public PlotDataImplementation(IVizEngine<TRender> vizualizer, Func<T, TRender> map, T data = default(T)) { this.Map = map; vizualizer.Owner = this; vizEngine = vizualizer; SetData (data); }
	}

	public static class PlotData {
		public static IPlotWriteable<Point[]> Create(Point[] Data, PlotClass plotClass) {
			return new PlotDataImplementation<Point[], Point[]>(plotClass == PlotClass.Line ? (IVizEngine<Point[]>)new VizEngines.VizLineSegments() : new VizEngines.VizPixelScatterSmart(), x => x, Data);
		}

		public static IPlotWriteable<T> Create<T>(T data, PlotClass plotClass, Func<T, Point[]> map) {
			return new PlotDataImplementation<T, Point[]>(plotClass == PlotClass.Line ? (IVizEngine<Point[]>)new VizEngines.VizLineSegments() : new VizEngines.VizPixelScatterSmart(), map,data);
		}


		public static IPlotWriteable<T> Create<T>(T Data, Action<WriteableBitmap, Matrix, int, int, T> displayFunc, Func<T, Rect> boundsFunc = null) {
			var visualizer = new VizEngines.VizDelegateBitmap<T> { UpdateBitmapDelegate = displayFunc };
			if (boundsFunc != null) visualizer.ComputeBounds = boundsFunc;
			return new PlotDataImplementation<T, T>(visualizer, x => x, Data);
		}

		public static IPlotWriteable<T> Create<T>(IVizEngine<T> viz) {
			return new PlotDataImplementation<T, T>(viz, x => x);
		}
	}
}

using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot {
	public sealed class PlotMetaData : DispatcherObject, IPlotMetaDataWriteable {
		public IPlot Plot { get; set; }

		void TriggerChange(GraphChange changeType) { if (Plot != null) Plot.GraphChanged(changeType); }
		void IPlotMetaData.TriggerChange(GraphChange changeType) { TriggerChange(changeType); }

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
		public Color? RenderColor { get { return m_PrimaryColor; } set { m_PrimaryColor = value; TriggerChange(GraphChange.RenderOptions);} }

		int zIndex;
		public int ZIndex { get { return zIndex; } set { zIndex = value; TriggerChange(GraphChange.Drawing); } }

		double? m_Thickness;
		public double? RenderThickness { get { return m_Thickness; } set { m_Thickness = value; TriggerChange(GraphChange.RenderOptions); } }

		bool hidden;
		public bool Hidden {
			get {return hidden;}
			set { if (hidden != value) { hidden = value; TriggerChange(GraphChange.Visibility); } }
		}
	}
}

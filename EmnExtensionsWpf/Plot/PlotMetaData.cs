using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace EmnExtensions.Wpf.Plot {
	public class PlotMetaData : DispatcherObject, IPlotMetaDataWriteable {

		private IPlot container;

		public IPlot Container { get { return container; } set { container = value; } }

		internal protected void TriggerChange(GraphChange changeType) { if (Container != null) Container.GraphChanged(changeType); }
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
	}
}

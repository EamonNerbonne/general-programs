using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Wpf.Plot
{
	public enum PlotClass { Auto, PointCloud, Line }

	public class PlotMetaData : ICloneable
	{
		internal BasePlotData owner;
		string m_xUnitLabel, m_yUnitLabel, m_DataLabel;
		TickedAxisLocation m_axisBindings = TickedAxisLocation.LeftOfGraph | TickedAxisLocation.BelowGraph;
		PlotClass m_PlotClass;

		public string XUnitLabel { get { return m_xUnitLabel; } set { if (m_xUnitLabel != value) { m_xUnitLabel = value; OnChange(GraphChange.Labels); } } }
		public string YUnitLabel { get { return m_yUnitLabel; } set { if (m_yUnitLabel != value) { m_yUnitLabel = value; OnChange(GraphChange.Labels); } } }
		public string DataLabel { get { return m_DataLabel; } set { if (m_DataLabel != value) { m_DataLabel = value; OnChange(GraphChange.Labels); } } }
		public TickedAxisLocation AxisBindings { get { return m_axisBindings; } set { if (m_axisBindings != value) { m_axisBindings = value; OnChange(GraphChange.Projection); } } }
		public object Tag { get; set; }
		public PlotClass PlotClass { get { return m_PlotClass; } set { if (m_PlotClass != value) { m_PlotClass = value; OnChange(GraphChange.Labels); } } }

		void OnChange(GraphChange changeType) { if (owner != null) owner.OnChange(changeType); }


		public PlotMetaData Clone()
		{
			return new PlotMetaData
			{
				m_axisBindings = this.m_axisBindings,
				m_DataLabel = this.m_DataLabel,
				m_PlotClass = this.m_PlotClass,
				m_xUnitLabel = this.m_xUnitLabel,
				m_yUnitLabel = this.m_yUnitLabel,
				owner = null, //clone isn't owned by same owner!
			};
		}
		object ICloneable.Clone() { return Clone(); }
		public static PlotMetaData Default
		{
			get
			{
				return new PlotMetaData
				{
					m_axisBindings = TickedAxisLocation.Default,
					m_DataLabel = "",
					m_PlotClass = PlotClass.Auto,
					m_xUnitLabel = "",
					m_yUnitLabel = "",
				};
			}
		}
	}
}

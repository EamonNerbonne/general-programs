using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public enum PlotClass { Auto, PointCloud, Line }


	public interface IPlotData
	{
		event Action<IPlotData, GraphChange> Changed;
		object RawData { get; set; }
		string XUnitLabel { get; set; }
		string YUnitLabel { get; set; }
		string DataLabel { get; set; }
		TickedAxisLocation AxisBindings { get; set; }
		Rect? OverrideBounds { get; set; }
		PlotClass PlotClass { get; set; }
		object Tag { get; set; }
	}

	public interface IPlotVizOwner : IPlotData
	{
		void TriggerChange(GraphChange changeType);
	}

	public abstract class PlotDataBase : IPlotVizOwner
	{
		public event Action<IPlotData, GraphChange> Changed;
		internal protected void TriggerChange(GraphChange changeType) { if (Changed != null) Changed(this, changeType); }
		void IPlotVizOwner.TriggerChange(GraphChange changeType) { TriggerChange(changeType); }

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

		PlotViz vizEngine;
		public PlotViz Visualizer
		{
			get { EnsureEngineExists(); return vizEngine; }
			set { vizEngine = value; TriggerChange(GraphChange.Drawing); }
		}

		void EnsureEngineExists()
		{
			if (vizEngine == null)
			{
				var vizFactory = ChooseVizFactory();
				var newVizEngine = vizFactory();
				newVizEngine.SetOwner(this);
				vizEngine = newVizEngine;
			}
		}

		Func<PlotViz> ChooseVizFactory()
		{
			throw new NotImplementedException();
		}

		public object RawData { get; set; }//TODO
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public abstract class PlotVizBase<T> : IPlotViz<T>
	{
		Rect m_DataBounds = Rect.Empty;
		public Rect DataBounds
		{
			get { return m_owner.OverrideBounds ?? m_DataBounds; }
			protected set
			{
				bool boundsChanged = !m_owner.OverrideBounds.HasValue && m_DataBounds != value;
				m_DataBounds = value;
				if (boundsChanged) OnChange(GraphChange.Projection);
			}
		}
		protected Rect InternalDataBounds { get { return m_DataBounds; } }

		Thickness m_Margin;
		public Thickness Margin { get { return m_Margin; } protected set { if (m_Margin != value) { m_Margin = value; OnChange(GraphChange.Projection); } } }

		protected void OnChange(GraphChange changeType) { m_owner.TriggerChange(changeType); }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);
		public abstract void DataChanged(T newData);

		IPlot<T> m_owner = null;
		protected IPlot<T> Owner { get { return m_owner; } }
		public void SetOwner(IPlot<T> owner) { if (owner != null)	throw new PlotVizException("Owner already set"); m_owner = owner; }
	}

}

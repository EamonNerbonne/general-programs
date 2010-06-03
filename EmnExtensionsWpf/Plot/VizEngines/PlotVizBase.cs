using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public abstract class PlotVizBase<T> : IVizEngine<T>
	{
		protected IPlot m_owner;
		public IPlot Owner { get { return m_owner; } set { m_owner = value; } }


		Rect m_DataBounds = Rect.Empty;
		public Rect DataBounds(T data) { return m_DataBounds; }
		protected void SetDataBounds(Rect newBounds)
		{
			bool boundsChanged = !m_owner.OverrideBounds.HasValue && m_DataBounds != newBounds;
			m_DataBounds = newBounds;
			if (boundsChanged) TriggerChange(GraphChange.Projection);
		}

		protected void TriggerChange(GraphChange graphChange) { m_owner.TriggerChange(graphChange); }

		protected Rect InternalDataBounds { get { return m_DataBounds; } }

		Thickness m_Margin;
		public Thickness Margin(T data) { return m_Margin; }
		protected void SetMargin(Thickness newMargin) { if (m_Margin != newMargin) { m_Margin = newMargin; TriggerChange(GraphChange.Projection); } }

		public abstract void DrawGraph(T data, DrawingContext context);
		public abstract void SetTransform(T data, Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
		public abstract void DataChanged(T data);
		public abstract void RenderOptionsChanged();
		public abstract bool SupportsThickness { get; }
		public abstract bool SupportsColor { get; }
	}

}

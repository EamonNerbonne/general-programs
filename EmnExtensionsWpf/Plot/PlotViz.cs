using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public abstract class PlotViz
	{
		Rect m_DataBounds = Rect.Empty;
		Thickness m_Margin;
		protected readonly PlotDataBase m_owner;

		public Rect DataBounds {
			get { return m_owner.OverrideBounds ?? m_DataBounds; }
			set {
				bool boundsChanged = !m_owner.OverrideBounds.HasValue && m_DataBounds != value;
				m_DataBounds = value;
				if (boundsChanged) OnChange(GraphChange.Projection);
			}
		}
		public Thickness Margin { get { return m_Margin; } set { if (m_Margin != value) { m_Margin = value; OnChange(GraphChange.Projection); } } }

		protected void OnChange(GraphChange changeType) { m_owner.TriggerChange(changeType); }
		public PlotViz(PlotDataBase owner) { m_owner = owner; }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);
		public abstract void DataChanged(object newData);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	abstract class PlotViz
	{
		Rect m_DataBounds = Rect.Empty;
		Thickness m_Margin;
		IPlotDataInternal m_owner;

		public Rect DataBounds { get { return m_DataBounds; } set { if (m_DataBounds != value) { m_DataBounds = value; TriggerChange(GraphChange.Projection); } } }
		public Thickness Margin { get { return m_Margin; } set { if (m_Margin != value) { m_Margin = value; TriggerChange(GraphChange.Projection); } } }

		void TriggerChange(GraphChange changeType)	{ m_owner.TriggerChange(changeType); }
		public PlotViz(IPlotDataInternal owner) { m_owner = owner; }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);

	}
}

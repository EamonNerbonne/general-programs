using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	abstract class PlotViz<T>
	{
		Rect m_DataBounds = Rect.Empty;
		Thickness m_Margin;
		PlotData<T> m_owner;

		public Rect DataBounds { get { return m_DataBounds; } set { if (m_DataBounds != value) { m_DataBounds = value; OnChange(GraphChange.Projection); } } }
		public Thickness Margin { get { return m_Margin; } set { if (m_Margin != value) { m_Margin = value; OnChange(GraphChange.Projection); } } }

		void OnChange(GraphChange changeType)	{ m_owner.OnChange(changeType); }
		public PlotViz(PlotData<T> owner) { m_owner = owner; }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public abstract class PlotVizDynamic<T> : IPlotViz<T>
	{
		IPlot<T> m_owner = null;
		protected IPlot<T> Owner { get { return m_owner; } }

		private IPlotViz<T> underlyingImpl = (IPlotViz<T>)VizNone.Singleton;
		public IPlotViz<T> UnderlyingPlotImpl
		{
			get { return underlyingImpl; }
			set { underlyingImpl = value; Owner.TriggerChange(GraphChange.Projection); Owner.TriggerChange(GraphChange.Drawing); }
		}

		public void DataChanged(T newData)
		{
			ChooseImplementation(newData);
			UnderlyingPlotImpl.DataChanged(newData);
		}

		protected abstract void ChooseImplementation(T newData);
		public void SetOwner(IPlot<T> owner)
		{
			m_owner = owner;
			if (UnderlyingPlotImpl != null)
				UnderlyingPlotImpl.SetOwner(owner);
		}
		public Rect DataBounds { get { return UnderlyingPlotImpl.DataBounds; } }
		public Thickness Margin { get { return UnderlyingPlotImpl.Margin; } }
		public void DrawGraph(DrawingContext context) { UnderlyingPlotImpl.DrawGraph(context); }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip) { UnderlyingPlotImpl.SetTransform(boundsToDisplay, displayClip); }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	class PlotVizEngineWrapper<T> : IPlotWithViz
	{
		readonly IVizEngine<T> m_engine;
		readonly T m_data;
		readonly IPlot m_plot;

		public PlotVizEngineWrapper(IPlot plot, T data, IVizEngine<T> engine)
		{
			m_engine = engine;
			m_data = data;
			m_plot = plot;
		}

		public Rect DataBounds { get { return m_plot.OverrideBounds ?? m_engine.DataBounds(m_data); } }
		public Thickness Margin { get { return m_engine.Margin(m_data); } }
		public void DrawGraph(DrawingContext context) { m_engine.DrawGraph(m_data, context); }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { m_engine.SetTransform(m_data, boundsToDisplay, displayClip, forDpiX,  forDpiY); }
	}

	public static class PlotViz
	{
		public static IPlotWithViz Wrap<T>(IPlot plot, T data, IVizEngine<T> engine) { return new PlotVizEngineWrapper<T>(plot, data, engine); }
	}
}

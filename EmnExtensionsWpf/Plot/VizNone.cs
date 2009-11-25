using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public class VizNone : IPlotViz<object>
	{
		private VizNone() { }
		private static readonly VizNone singleton = new VizNone();
		public VizNone Singleton { get { return singleton; } }
		public Rect DataBounds { get { return Rect.Empty; } }
		public Thickness Margin { get { return new Thickness(0.0); } }
		public void DrawGraph(DrawingContext context) { }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip) { }
		public void DataChanged(object newData) { }
		public void SetOwner(IPlot<object> owner) { }
	}
}

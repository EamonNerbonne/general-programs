using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class VizNone : IVizEngine<object>
	{
		public Rect DataBounds(object data) { return Rect.Empty; }
		public Thickness Margin(object data) { return new Thickness(0.0); }
		public void DrawGraph(object data, DrawingContext context) { }
		public void SetTransform(object data, Matrix boundsToDisplay, Rect displayClip) { }
		public void DataChanged(object data) { }
		public IPlot Owner { get; set; }
		public void RenderOptionsChanged() { }
		public bool SupportsThickness { get { return false; } }
		public bool SupportsColor { get { return false; } }
	}
}

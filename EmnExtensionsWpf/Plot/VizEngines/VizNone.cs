using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public class VizNone : DispatcherObject, IVizEngine<object> {
		public Rect DataBounds { get { return Rect.Empty; } }
		public Thickness Margin { get { return new Thickness(0.0); } }
		public void DrawGraph(DrawingContext context) { }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { }
		public void ChangeData(object data) { }
		public IPlot Plot { get; set; }
		public void OnRenderOptionsChanged() { }
		public bool SupportsColor { get { return false; } }
		public virtual Drawing SampleDrawing { get { return null; } }
	}
}

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public sealed class VizMapped<TIn, TOut> : IVizEngine<TIn>, ITranformed<TOut> {
		readonly Func<TIn, TOut> map;
		IVizEngine<TOut> Implementation { get; set; }
		public void ChangeData(TIn newData) { Implementation.ChangeData(map(newData)); }
		public Rect DataBounds { get { return Implementation.DataBounds; } }
		public Thickness Margin { get { return Implementation.Margin; } }
		public void DrawGraph(DrawingContext context) { Implementation.DrawGraph(context); }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { Implementation.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY); }
		public void OnRenderOptionsChanged() { Implementation.OnRenderOptionsChanged(); }
		public IPlot Plot { get { return Implementation.Plot; } set { Implementation.Plot = value; } }
		public bool SupportsColor { get { return Implementation.SupportsColor; } }
		public Drawing SampleDrawing { get { return Implementation == null ? null : Implementation.SampleDrawing; } }
		public Dispatcher Dispatcher { get { return Implementation.Dispatcher; } }

		public VizMapped(IVizEngine<TOut> impl, Func<TIn, TOut> map) { this.map = map; Implementation = impl; }

		IVizEngine<TOut> ITranformed<TOut>.Implementation { get { return Implementation; } }
	}
}

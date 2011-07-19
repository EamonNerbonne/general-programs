using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public abstract class VizTransformed<TIn, TOut> : IVizEngine<TIn>,ITranformed<TOut> {
		protected abstract IVizEngine<TOut> Implementation { get; }
		IVizEngine<TOut> ITranformed<TOut>.Implementation { get { return Implementation; } }

		public abstract void ChangeData(TIn newData);
		public virtual Rect DataBounds { get { return Implementation.DataBounds; } }

		public Thickness Margin { get { return Implementation.Margin; } }
		public void DrawGraph(DrawingContext context) { Implementation.DrawGraph(context); }
		public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { Implementation.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY); }
		public void OnRenderOptionsChanged() { Implementation.OnRenderOptionsChanged(); }
		public IPlot Plot { get { return Implementation.Plot; } set { Implementation.Plot = value; } }
		public bool SupportsColor { get { return Implementation.SupportsColor; } }
		public Drawing SampleDrawing { get { return Implementation == null ? null : Implementation.SampleDrawing; } }
		public Dispatcher Dispatcher { get { return Implementation.Dispatcher; } }

	}
}

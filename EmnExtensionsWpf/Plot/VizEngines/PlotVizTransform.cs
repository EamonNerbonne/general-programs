using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public abstract class PlotVizTransform<TIn, TOut> : IVizEngine<TIn> {
		protected abstract TOut TransformedData(TIn inputData);
		protected abstract IVizEngine<TOut> Implementation { get; }
		public virtual Rect DataBounds { get { return Implementation.DataBounds; } }
		public virtual Thickness Margin { get { return Implementation.Margin; } }
		public virtual void DrawGraph(DrawingContext context) { Implementation.DrawGraph(context); }
		public virtual void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { Implementation.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY); }
		public abstract void DataChanged(TIn newData);
		public virtual void OnRenderOptionsChanged() { Implementation.OnRenderOptionsChanged(); }
		public IPlot Owner { get { return Implementation.Owner; } set { Implementation.Owner = value; } }

		public virtual bool SupportsColor { get { return Implementation.SupportsColor; } }



		public Drawing SampleDrawing { get { return Implementation == null ? null : Implementation.SampleDrawing; } }
	}
}

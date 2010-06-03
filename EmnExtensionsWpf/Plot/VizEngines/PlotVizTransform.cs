using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public abstract class PlotVizTransform<TIn, TOut> : IVizEngine<TIn>
	{
		protected abstract TOut TransformedData(TIn inputData);
		protected abstract IVizEngine<TOut> Implementation { get; }
		public virtual Rect DataBounds(TIn data) { return Implementation.DataBounds(TransformedData(data)); }
		public virtual Thickness Margin(TIn data) { return Implementation.Margin(TransformedData(data)); }
		public virtual void DrawGraph(TIn data, DrawingContext context) { Implementation.DrawGraph(TransformedData(data), context); }
		public virtual void SetTransform(TIn data, Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { Implementation.SetTransform(TransformedData(data), boundsToDisplay, displayClip,forDpiX,forDpiY); }
		public abstract void DataChanged(TIn newData);
		public virtual void RenderOptionsChanged() { Implementation.RenderOptionsChanged(); }
		public IPlot Owner { get { return Implementation.Owner; } set { Implementation.Owner = value; } }

		public virtual bool SupportsThickness { get { return Implementation.SupportsThickness; } }
		public virtual bool SupportsColor { get { return Implementation.SupportsColor; } }
	}
}

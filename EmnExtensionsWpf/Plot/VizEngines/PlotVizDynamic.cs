using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
	public class PlotVizDynamic<T> : IVizEngine<T>
	{
		Func<IPlot, T, IVizEngine<T>, IVizEngine<T>> implChooser;
		public PlotVizDynamic(Func<IPlot, T, IVizEngine<T>, IVizEngine<T>> engineSelector) { implChooser = engineSelector; }

		private IVizEngine<T> underlyingImpl = (IVizEngine<T>)new VizNone();
		public IVizEngine<T> UnderlyingPlotImpl { get { return underlyingImpl; } }
		public void SetPlotImpl(T data, IVizEngine<T> impl)
		{
			var owner = Owner;
			impl.Owner = owner;
			underlyingImpl.Owner = null;
			underlyingImpl = impl;
			owner.TriggerChange(GraphChange.Projection);
			owner.TriggerChange(GraphChange.Drawing);
		}

		public Rect DataBounds(T data) { return underlyingImpl.DataBounds( data); }
		public Thickness Margin(T data) { return underlyingImpl.Margin( data); }
		public void DrawGraph(T data, DrawingContext context) { underlyingImpl.DrawGraph( data, context); }
		public void SetTransform(T data, Matrix boundsToDisplay, Rect displayClip) { underlyingImpl.SetTransform( data, boundsToDisplay, displayClip); }
		public void DataChanged(T data) { ChooseImplementation( data); underlyingImpl.DataChanged( data); }
		void ChooseImplementation(T data)
		{
			var newImpl = implChooser(Owner, data, UnderlyingPlotImpl);
			if ((newImpl ?? UnderlyingPlotImpl) != newImpl)
				SetPlotImpl( data, newImpl);
		}

		public IPlot Owner { get { return underlyingImpl.Owner; } set { underlyingImpl.Owner = value; } }

	}
}

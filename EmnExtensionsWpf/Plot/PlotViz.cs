using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public interface IPlotViz
	{
		Rect DataBounds { get; }
		Thickness Margin { get; }
		void DrawGraph(DrawingContext context);
		void SetTransform(Matrix boundsToDisplay, Rect displayClip);
	}


	public interface IPlotViz<in T> : IPlotViz
	{
		void DataChanged(T newData);
		void SetOwner(IPlot<T> owner);
		//IPlot<T> Owner { get; }
	}
	//public interface IVizEngine<in T>
	//{
	//    Rect GetDataBounds(IPlot<T> owner);
	//    Thickness GetMargin(IPlot<T> owner);
	//    void DrawGraph(IPlot<T> owner, DrawingContext context);
	//    void SetTransform(IPlot<T> owner, Matrix boundsToDisplay, Rect displayClip);
	//    void DataChanged(IPlot<T> owner);
	//}


	public abstract class PlotViz<T> : IPlotViz<T>
	{
		Rect m_DataBounds = Rect.Empty;
		public Rect DataBounds
		{
			get { return m_owner.OverrideBounds ?? m_DataBounds; }
			protected set
			{
				bool boundsChanged = !m_owner.OverrideBounds.HasValue && m_DataBounds != value;
				m_DataBounds = value;
				if (boundsChanged) OnChange(GraphChange.Projection);
			}
		}
		protected Rect InternalDataBounds { get { return m_DataBounds; } }

		Thickness m_Margin;
		public Thickness Margin { get { return m_Margin; } protected set { if (m_Margin != value) { m_Margin = value; OnChange(GraphChange.Projection); } } }

		protected void OnChange(GraphChange changeType) { m_owner.TriggerChange(changeType); }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip);
		public abstract void DataChanged(T newData);

		IPlot<T> m_owner = null;
		protected IPlot<T> Owner { get { return m_owner; } }
		public void SetOwner(IPlot<T> owner) { if (owner != null)	throw new PlotVizException("Owner already set"); m_owner = owner; }
	}

	public abstract class DynamicPlotViz<T> : IPlotViz<T>
	{
		IPlot<T> m_owner = null;
		protected IPlot<T> Owner { get { return m_owner; } }

		private IPlotViz<T> underlyingImpl;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot.VizEngines {
	public abstract class PlotVizBase<T> : DispatcherObject, IVizEngine<T> {
		IPlot m_owner;
		public IPlot Plot { get { return m_owner; } set { m_owner = value; OnRenderOptionsChanged(); } }

		Rect m_DataBounds = Rect.Empty;
		public Rect DataBounds { get { return m_DataBounds; } }
		protected void SetDataBounds(Rect newBounds) {
			bool boundsChanged = m_owner != null && !m_owner.MetaData.OverrideBounds.HasValue && m_DataBounds != newBounds;
			m_DataBounds = newBounds;
			if (boundsChanged) TriggerChange(GraphChange.Projection);
		}

		protected void TriggerChange(GraphChange graphChange) { if (m_owner != null) m_owner.GraphChanged(graphChange); }

		protected Rect InternalDataBounds { get { return m_DataBounds; } }

		Thickness m_Margin;
		public Thickness Margin { get { return m_Margin; } }
		protected void SetMargin(Thickness newMargin) { if (m_Margin != newMargin) { m_Margin = newMargin; TriggerChange(GraphChange.Projection); } }

		public abstract void DrawGraph(DrawingContext context);
		public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
		public T Data { get; private set; }
		public void DataChanged(T data) {
			
			var oldData = Data;
			Data = data;
			OnDataChanged(oldData);
		}
		protected abstract void OnDataChanged(T oldData);
		public abstract void OnRenderOptionsChanged();
		public abstract bool SupportsColor { get; }

		public virtual Drawing SampleDrawing { get { return Plot == null || Plot.MetaData.RenderColor == null ? null : new GeometryDrawing(new SolidColorBrush(Plot.MetaData.RenderColor.Value).AsFrozen(), null, new RectangleGeometry(new Rect(0, 0, 10, 10))); } }
	}
}

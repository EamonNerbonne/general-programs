using System;
using System.Windows;
using System.Windows.Media;
using EmnExtensions.Wpf.WpfTools;

namespace EmnExtensions.Wpf.Plot.VizEngines
{
    public abstract class PlotVizBase<T> : IVizEngine<T>
    {
        protected PlotVizBase(IPlotMetaData owner)
        {
            if (owner == null) {
                throw new ArgumentNullException(nameof(owner));
            }

            MetaData = owner;
        }

        public IPlotMetaData MetaData { get; }
        Rect? m_DataBounds;
        public Rect DataBounds => m_DataBounds ?? (m_DataBounds = ComputeBounds()).Value;

        protected void InvalidateDataBounds()
        {
            m_DataBounds = null;
            var boundsChanged = !MetaData.OverrideBounds.HasValue;
            if (boundsChanged) {
                TriggerChange(GraphChange.Projection);
            }
        }

        protected abstract Rect ComputeBounds();

        protected void TriggerChange(GraphChange graphChange) => MetaData.TriggerChange(graphChange);

        Thickness m_Margin;

        public Thickness Margin => MetaData.OverrideMargin ?? m_Margin;

        protected void SetMargin(Thickness newMargin)
        {
            var oldMargin = Margin;
            m_Margin = newMargin;
            if (Margin != oldMargin) {
                TriggerChange(GraphChange.Projection);
            }
        }

        public abstract void DrawGraph(DrawingContext context);
        public abstract void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
        protected T Data { get; private set; }

        public void ChangeData(T data)
        {
            var oldData = Data;
            Data = data;
            OnDataChanged(oldData);
        }

        protected abstract void OnDataChanged(T oldData);
        public abstract void OnRenderOptionsChanged();
        public abstract bool SupportsColor { get; }

        public virtual Drawing SampleDrawing => MetaData.RenderColor == null ? null : new GeometryDrawing(new SolidColorBrush(MetaData.RenderColor.Value).AsFrozen(), null, new RectangleGeometry(new Rect(0, 0, 10, 10)));
    }
}

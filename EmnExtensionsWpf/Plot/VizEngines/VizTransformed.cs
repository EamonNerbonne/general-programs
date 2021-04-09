using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines
{
    public abstract class VizTransformed<TIn, TOut> : IVizEngine<TIn>, ITranformed<TOut>
    {
        protected abstract IVizEngine<TOut> Implementation { get; }
        IVizEngine<TOut> ITranformed<TOut>.Implementation => Implementation;

        public abstract void ChangeData(TIn newData);
        public virtual Rect DataBounds => Implementation.DataBounds;

        public Thickness Margin => Implementation.Margin;
        public void DrawGraph(DrawingContext context) => Implementation.DrawGraph(context);
        public virtual void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) => Implementation.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY);
        public void OnRenderOptionsChanged() => Implementation.OnRenderOptionsChanged();
        public IPlotMetaData MetaData => Implementation.MetaData;
        public bool SupportsColor => Implementation.SupportsColor;
        public Drawing SampleDrawing => Implementation == null ? null : Implementation.SampleDrawing;
    }
}

using System;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.VizEngines
{
    public sealed class VizMapped<TIn, TOut> : IVizEngine<TIn>, ITranformed<TOut>
    {
        readonly Func<TIn, TOut> map;
        IVizEngine<TOut> Implementation { get; set; }
        public void ChangeData(TIn newData) => Implementation.ChangeData(map(newData));
        public Rect DataBounds => Implementation.DataBounds;
        public Thickness Margin => Implementation.Margin;
        public void DrawGraph(DrawingContext context) => Implementation.DrawGraph(context);
        public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) => Implementation.SetTransform(boundsToDisplay, displayClip, forDpiX, forDpiY);
        public void OnRenderOptionsChanged() => Implementation.OnRenderOptionsChanged();
        public IPlotMetaData MetaData => Implementation.MetaData;
        public bool SupportsColor => Implementation.SupportsColor;
        public Drawing SampleDrawing => Implementation == null ? null : Implementation.SampleDrawing;

        public VizMapped(IVizEngine<TOut> impl, Func<TIn, TOut> map)
        {
            this.map = map;
            Implementation = impl;
        }

        IVizEngine<TOut> ITranformed<TOut>.Implementation => Implementation;
    }
}

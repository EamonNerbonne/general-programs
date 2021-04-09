﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.VizEngines
{
    public class VizNone : DispatcherObject, IVizEngine<object>
    {
        public Rect DataBounds => Rect.Empty;
        public Thickness Margin => new Thickness(0.0);
        public void DrawGraph(DrawingContext context) { }
        public void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY) { }
        public void ChangeData(object data) { }
        public IPlotMetaData MetaData { get; private set; }
        public void OnRenderOptionsChanged() { }
        public bool SupportsColor => false;
        public virtual Drawing SampleDrawing => null;
        public VizNone(IPlotMetaData metadata) { MetaData = metadata; }
    }
}

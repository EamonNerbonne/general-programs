using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf {
    public interface IVizEngine {
        Rect DataBounds { get; }
        Thickness Margin { get; }
        void DrawGraph(DrawingContext context);
        void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
        void OnRenderOptionsChanged();
        IPlotMetaData MetaData { get; } //this will always be set before any usage other of this interface
        bool SupportsColor { get; }
        Drawing SampleDrawing { get; }
    }

    public interface IDataSink<in T> {
        void ChangeData(T data);
    }

    public interface IVizEngine<in T> : IVizEngine, IDataSink<T> {}

    public interface ITranformed<in T> {
        IVizEngine<T> Implementation { get; }
    }
}

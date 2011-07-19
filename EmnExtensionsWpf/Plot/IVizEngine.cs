using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EmnExtensions.Wpf.Plot {
	public interface IVizEngine {
		Rect DataBounds { get; }
		Thickness Margin { get; }
		void DrawGraph(DrawingContext context);
		void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
		void OnRenderOptionsChanged();
		IPlot Plot { get; set; } //this will always be set before any usage other of this interface
		bool SupportsColor { get; }
		Drawing SampleDrawing { get; }
		Dispatcher Dispatcher { get; }
	}

	public interface IDataSink<in T> {
		Dispatcher Dispatcher { get; }
		void ChangeData(T data);
	}

	public interface IVizEngine<in T> : IVizEngine, IDataSink<T> {
		new Dispatcher Dispatcher { get; }
	}

	public interface ITranformed<in T> {
		IVizEngine<T> Implementation { get; }
	}
}

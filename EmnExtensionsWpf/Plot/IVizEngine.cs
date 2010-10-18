using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot {
	public interface IVizEngine {
		Rect DataBounds { get; }
		Thickness Margin { get; }
		void DrawGraph(DrawingContext context);
		void SetTransform(Matrix boundsToDisplay, Rect displayClip, double forDpiX, double forDpiY);
		void OnRenderOptionsChanged();
		IPlot Owner { get; set; } //this will always be set before any usage other of this interface
		bool SupportsColor { get; }
		Drawing SampleDrawing { get; }
	}

	public interface IVizEngine<in T> : IVizEngine {
		void DataChanged(T data);
	}
}

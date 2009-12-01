using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public interface IVizEngine<in T>
	{
		Rect DataBounds(T data);
		Thickness Margin(T data);
		void DrawGraph(T data, DrawingContext context);
		void SetTransform(T data, Matrix boundsToDisplay, Rect displayClip);
		void DataChanged(T data);
		IPlot Owner { get; set; } //this will always be set before any usage other of this interface
	}
}

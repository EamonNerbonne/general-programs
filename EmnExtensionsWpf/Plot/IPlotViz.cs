using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EmnExtensions.Wpf.Plot
{
	public interface IPlotWithViz
	{
		Rect DataBounds { get; }
		Thickness Margin { get; }
		void DrawGraph(DrawingContext context);
		void SetTransform(Matrix boundsToDisplay, Rect displayClip);
	}
}

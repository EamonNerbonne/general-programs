using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public enum PlotClass { Auto, PointCloud, Line }
	public interface IPlot : IPlotView
	{
		Rect? OverrideBounds { get; }
		PlotClass PlotClass { get; }
		void TriggerChange(GraphChange changeType);
	}
}

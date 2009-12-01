using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EmnExtensions.Wpf.Plot
{
	public interface IPlotControl<T> : IPlot
	{
		new string XUnitLabel { get; set; }
		new string YUnitLabel { get; set; }
		new string DataLabel { get; set; }
		new Rect? OverrideBounds { get; set; }
		new PlotClass PlotClass { get; set; }
		new object Tag { get; set; }
		T Data { get; set; }
	}
}

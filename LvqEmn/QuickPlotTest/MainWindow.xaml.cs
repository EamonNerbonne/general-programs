using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.MathHelpers;

namespace QuickPlotTest
{
	public static class QuickHelpers
	{
		public static T With<T>(this T item, Action<T> action) { action(item); return item; }
	}
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow() { InitializeComponent(); }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			Random r = new MersenneTwister(37);
			newPlotControl1.Graphs.Add(
				PlotData.Create(
						(from index in Enumerable.Range(0, 1000)
						 let x = index
						 select new Point(x, x *(double) x)).ToArray()
					).With(plot =>
					{
						plot.PlotClass = PlotClass.Line;
						plot.DataLabel = "parabola";
						plot.XUnitLabel = "x";
						plot.YUnitLabel = "x^2";
					}
					)
				);

			newPlotControl1.Graphs.Add(
				PlotData.Create(
						(from index in Enumerable.Range(0, 1000)
						 from tries in Enumerable.Range(0, 500)
						 let x = index + r.NextNorm()
						 let y = (x + r.NextNorm()*5) * (x + r.NextNorm()*5)
						 select new Point(x, y)).ToArray()
					).With(plot =>
					{
						plot.DataLabel = "parabola";
						plot.XUnitLabel = "x";
						plot.YUnitLabel = "x^2";
					})
				);
		}
	}
}

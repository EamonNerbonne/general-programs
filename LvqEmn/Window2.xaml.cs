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
using System.Windows.Shapes;
using EmnExtensions.Wpf.Plot;
using EmnExtensions.Wpf;
using EmnExtensions.MathHelpers;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for Window2.xaml
	/// </summary>
	public partial class Window2 : Window
	{
		public Window2() {
			InitializeComponent();
			//plotControl
			int pointsPerSet = 10000;
			double[] mean = CreateGaussianCloud.RandomMean(2, RndHelper.ThreadLocalRandom);
			double[,] trans = CreateGaussianCloud.RandomTransform(2, RndHelper.ThreadLocalRandom);
			double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, 2, trans, mean, RndHelper.ThreadLocalRandom);

			var pointsIter = Enumerable.Range(0, pointsPerSet)
				.Select(i => new Point(points[i, 0], points[i, 1]));

			Geometry pointCloud = GraphUtils.PointCloud(pointsIter);
			plotControl .AddPlot(new GraphableGeometry { Geometry = pointCloud });

		}
	}
}

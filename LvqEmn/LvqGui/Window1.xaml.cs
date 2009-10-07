//#define USEGEOMPLOT
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
using EmnExtensions.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using EmnExtensions.DebugTools;
using System.Threading;
//using System.Threading.Tasks;
using EmnExtensions.Wpf.OldGraph;
using EmnExtensions.Wpf.Plot;
using System.Windows.Threading;
using EmnExtensions;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window// Window
	{
		public MainWindow()
		{
			//this.WindowStyle = WindowStyle.None;
			//this.AllowsTransparency = true; 
			BorderBrush = Brushes.White;

			InitializeComponent();
			this.Background = Brushes.White;
		}

		private void textBoxNumberOfSets_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }

		private void textBoxPointsPerSet_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }

		public int? NumberOfSets { get { return textBoxNumberOfSets.Text.ParseAsInt32(); } }
		public int? PointsPerSet { get { return textBoxPointsPerSet.Text.ParseAsInt32(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e)
		{
			//plotControl.Graphs.Clear();
			NiceTimer timer = new NiceTimer(); timer.TimeMark("making point clouds");
			if (!NumberOfSets.HasValue || !PointsPerSet.HasValue)
			{
				Console.WriteLine("Invalid initialization values");
				return;
			}

			object sync = new object();
			int done = 0;
			int numSets = NumberOfSets.Value;
			int pointsPerSet = PointsPerSet.Value;


			MersenneTwister rndG = new MersenneTwister(123);

			for (int si = 0; si < numSets; si++)
			{//create each point-set
				ThreadPool.QueueUserWorkItem((index) =>
				{

					MersenneTwister rnd;
					lock (rndG)
						rnd = new MersenneTwister(rndG.Next());
					double[] mean = CreateGaussianCloud.RandomMean(2, rnd);
					double[,] trans = CreateGaussianCloud.RandomTransform(2, rnd);
					double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, 2, trans, mean, rnd);



					Dispatcher.BeginInvoke((Action)(() =>
					{
						var pointsIter = Enumerable.Range(0, pointsPerSet)
							.Select(i => new Point(points[i, 0], points[i, 1]));

#if USEGEOMPLOT
						Geometry pointCloud = GraphUtils.PointCloud(pointsIter);
						Pen pen = new Pen {
						    Brush = GraphRandomPen.RandomGraphBrush(),
						    //EndLineCap = PenLineCap.Round,	StartLineCap = PenLineCap.Round,
						    EndLineCap = PenLineCap.Square,
						    StartLineCap = PenLineCap.Square,
						    Thickness = 2.0,
						};
						pen.Freeze();
						plotControl.AddPlot(new GraphableGeometry { Geometry = pointCloud, Pen = pen, XUnitLabel = "X axis", YUnitLabel = "Y axis" });
#else
						plotControl.AddPlot(
							new GraphablePixelScatterPlot
							{
								PointColor = F.Create<Color, Color>((c) => { c.ScA = 0.3f; return c; })(GraphRandomPen.RandomGraphColor()),
								XUnitLabel = "X axis",
								YUnitLabel = "Y axis",
								DpiX = 192.0,
								DpiY = 192.0,
								BitmapScalingMode = BitmapScalingMode.NearestNeighbor,
								CoverageRatio = 0.99,
								 UseDiamondPoints = false,
								Points = pointsIter.ToArray(),

							});
#endif
						lock (sync)
						{
							done++;
							if (done == numSets)
							{
								timer.TimeMark(null);
								renderCount = 0;
								//Dispatcher.BeginInvoke((Action)DoSizingTest, DispatcherPriority.Loaded);
							}
						}
					}));

				}, si);
			}
		}
		NiceTimer overall;
		protected override void OnInitialized(EventArgs e)
		{
#if USEGEOMPLOT || DEBUG
			textBoxPointsPerSet.Text = 1000.ToString();
#else
			textBoxPointsPerSet.Text = 10000.ToString();
#endif
			base.OnInitialized(e);
			//buttonGeneratePointClouds_Click(null, null);
			//Dispatcher.Invoke((Action)(() => {
			//}), DispatcherPriority.ApplicationIdle);
		//	ThreadPool.QueueUserWorkItem((ignore) => { MatSpeedTest.Test(); });

		}

		volatile int renderCount = 0;
		bool completedTest = false;

		void DoSizingTest()
		{
			if (overall == null)
			{
				overall = new NiceTimer();
				overall.TimeMark("Sizing");
			}
			if (Width + Height > 2000)
			{
				if (!completedTest)
				{
					completedTest = true;
					overall.TimeMark(null);
					//Close();
				}
			}
			else
			{
				renderCount++;
				if (renderCount % 2 == 0)
				{
					if (Width > Height)
						Height = Height + 30;
					else
						Width = Width + 30;
				}
				Dispatcher.BeginInvoke((Action)DoSizingTest);
			}
		}

		GeometryGroup smallerGeom;
		private void buttonAddPoints_Click(object sender, RoutedEventArgs e)
		{
			if (smallerGeom == null)
			{
				smallerGeom = new GeometryGroup();
			}
			int pointsPerSet = PointsPerSet.Value / 100;

			ThreadPool.QueueUserWorkItem((index) =>
			{

				double[] mean = CreateGaussianCloud.RandomMean(2, RndHelper.ThreadLocalRandom);
				double[,] trans = CreateGaussianCloud.RandomTransform(2, RndHelper.ThreadLocalRandom);
				double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, 2, trans, mean, RndHelper.ThreadLocalRandom);


				var pointsIter = Enumerable.Range(0, pointsPerSet)
					.Select(i => new Point(points[i, 0], points[i, 1]))
					.ToArray();

				/*
								Dispatcher.BeginInvoke((Action)(() => {
									Geometry pointCloud = GraphUtils.PointCloud(pointsIter);
									smallerGeom.Children.Add(pointCloud);
									//pointCloud.Freeze();
									GraphGeometryControl control =
										plotControl.Graphs
										.Where(gc => gc is GraphGeometryControl)
										.Cast<GraphGeometryControl>()
										.Where(gc => gc.GraphGeometry == smallerGeom)
										.FirstOrDefault();
									if (control == null) {
										Pen pen = new Pen {
											Brush = GraphRandomPen.RandomGraphColor(),
											EndLineCap = PenLineCap.Square,
											StartLineCap = PenLineCap.Square,
											Thickness = 1.5,
										};
										plotControl.Graphs.Add(new GraphGeometryControl() {
											GraphGeometry =pointCloud,// smallerGeom,
											Name = "smallerGeom",
											GraphPen = pen
										});
									}
								}));*/

			});
		}

	}
}

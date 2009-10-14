#define USEGEOMPLOT
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
using LVQCppCli;
using System.Diagnostics;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window// Window
	{
		const int DIMS = 50;


		public MainWindow() {
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

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e) {
//			plotControl.Clear();
			NiceTimer timer = new NiceTimer(); timer.TimeMark("making point clouds");
			
			if (!NumberOfSets.HasValue || !PointsPerSet.HasValue) {
				Console.WriteLine("Invalid initialization values");
				return;
			}

			object sync = new object();
			int done = 0;
			int numSets = NumberOfSets.Value;
			int pointsPerSet = PointsPerSet.Value;
			SetupDisplay(numSets);

			MersenneTwister rndG = new MersenneTwister(123);
			List<double[,]> pointClouds = new List<double[,]>();

			for (int si = 0; si < numSets; si++) {//create each point-set
				ThreadPool.QueueUserWorkItem((index) => {

					MersenneTwister rnd;
					lock (rndG)
						rnd = new MersenneTwister(rndG.Next());
					double[] mean = CreateGaussianCloud.RandomMean(DIMS, rnd);
					double[,] trans = CreateGaussianCloud.RandomTransform(DIMS, rnd);
					double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, DIMS, trans, mean, rnd);
					lock (rndG)
						pointClouds.Add(points);

					lock (sync) {
						done++;
						if (done == numSets) {
							timer.TimeMark(null);
							renderCount = 0;
							new Thread(() => { StartLvq(pointClouds); }) {
								IsBackground = true,
							}.Start();
						}
					}
				}, si);
			}
		}

		private void StartLvq(List<double[,]> pointClouds) {
			double[,] allpoints = new double[ pointClouds.Sum(pc => pc.GetLength(0)), DIMS];
			int[] pointLabels = new int[allpoints.GetLength(0)];
			List<int> classBoundaries = new List<int> { 0 };
			int pointI = 0;
			int classLabel = 0;
			foreach (var pointCloud in pointClouds) {
				for (int i = 0; i < pointCloud.GetLength(0); i++) {
					for (int j = 0; j < DIMS; j++)
						allpoints[pointI,j] = pointCloud[i, j];
					pointLabels[pointI] = classLabel;
					pointI++;
				}
				classBoundaries.Add(pointI);
				classLabel++;
			}
			Debug.Assert(pointI == allpoints.GetLength(0));
			Debug.Assert(pointClouds[0].GetLength(1) == allpoints.GetLength(1));
			this.classBoundaries = classBoundaries.ToArray();
			lvqImpl = new LvqWrapper(allpoints, pointLabels, classLabel, 1);
			UpdateDisplay();
		}

		private void UpdateDisplay() {
			double[,] currPoints = lvqImpl.CurrentProjection();
			Dispatcher.BeginInvoke((Action)(() => {
				for (int i = 0; i < classBoundaries.Length - 1; i++) {

					var pointsIter = Enumerable.Range(classBoundaries[i], classBoundaries[i + 1])
												.Select(pi => new Point(currPoints[pi, 0], currPoints[pi, 1]));

#if USEGEOMPLOT
					((GraphableGeometry)plotControl.GetPlot(i)).Geometry = GraphUtils.PointCloud(pointsIter);
#else
						plotControl.AddPlot(
							new GraphablePixelScatterPlot
							{
								PointColor = F.Create<Color, Color>((c) => { c.ScA = 0.3f; return c; })(GraphRandomPen.RandomGraphColor()),
								XUnitLabel = "X axis",
								YUnitLabel = "Y axis",
								DpiX = 96.0,
								DpiY = 96.0,
								BitmapScalingMode = BitmapScalingMode.NearestNeighbor,
								CoverageRatio = 0.99,
								UseDiamondPoints = false,
								Points = pointsIter.ToArray(),

							});
#endif
				}
			}));
		}

		private void SetupDisplay(int numClasses) {
			Dispatcher.BeginInvoke((Action)(() => {
				plotControl.Clear();

#if USEGEOMPLOT
				Pen pen = new Pen {
					Brush = GraphRandomPen.RandomGraphBrush(),
					//EndLineCap = PenLineCap.Round,	StartLineCap = PenLineCap.Round,
					EndLineCap = PenLineCap.Square,
					StartLineCap = PenLineCap.Square,
					Thickness = 1.5,
				};
				pen.Freeze();
				for (int i = 0; i < numClasses; i++)
					plotControl.AddPlot(new GraphableGeometry { Geometry = GraphUtils.PointCloud(Enumerable.Empty<Point>()), Pen = pen, XUnitLabel = "X axis", YUnitLabel = "Y axis" });
#else
						plotControl.AddPlot(
							new GraphablePixelScatterPlot
							{
								PointColor = F.Create<Color, Color>((c) => { c.ScA = 0.3f; return c; })(GraphRandomPen.RandomGraphColor()),
								XUnitLabel = "X axis",
								YUnitLabel = "Y axis",
								DpiX = 96.0,
								DpiY = 96.0,
								BitmapScalingMode = BitmapScalingMode.NearestNeighbor,
								CoverageRatio = 0.99,
								UseDiamondPoints = false,
								Points = pointsIter.ToArray(),

							});
#endif
			}));
		}

		NiceTimer overall;
		int[] classBoundaries;
		LvqWrapper lvqImpl;
		protected override void OnInitialized(EventArgs e) {
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

		void DoSizingTest() {
			if (overall == null) {
				overall = new NiceTimer();
				overall.TimeMark("Sizing");
			}
			if (Width + Height > 2000) {
				if (!completedTest) {
					completedTest = true;
					overall.TimeMark(null);
					//Close();
				}
			} else {
				renderCount++;
				if (renderCount % 2 == 0) {
					if (Width > Height)
						Height = Height + 30;
					else
						Width = Width + 30;
				}
				Dispatcher.BeginInvoke((Action)DoSizingTest);
			}
		}

		private void checkBox1Changed(object sender, RoutedEventArgs e) {
			plotControl.ShowGridLines = checkBox1.IsChecked ?? plotControl.ShowGridLines;
		}

	}
}

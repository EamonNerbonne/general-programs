#define USEGEOMPLOT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using EmnExtensions.DebugTools;
using EmnExtensions.MathHelpers;
using EmnExtensions.Text;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.OldGraph;
using EmnExtensions.Wpf.Plot;
using LVQCppCli;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window// Window
	{



		public MainWindow() {
			//this.WindowStyle = WindowStyle.None;
			//this.AllowsTransparency = true; 
			BorderBrush = Brushes.White;

			InitializeComponent();
			this.Background = Brushes.White;
			plotControl.AttemptBorderTicks = false;
		}

		private void textBoxNumberOfSets_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		private void textBoxPointsPerSet_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		private void textBoxDims_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, s => DataVerifiers.IsInt32Positive(s) && Int32.Parse(s) > 2); }
		private void textBoxEpochs_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		private void textBoxStddevMeans_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsDoublePositive); }


		public int? NumberOfSets { get { return textBoxNumberOfSets.Text.ParseAsInt32(); } }
		public int? PointsPerSet { get { return textBoxPointsPerSet.Text.ParseAsInt32(); } }
		public int? Dimensions { get { return textBoxDims.Text.ParseAsInt32(); } }
		public int? EpochsPerClick { get { return textBoxEpochs.Text.ParseAsInt32(); } }
		public double? StddevMeans { get { return textBoxStddevMeans.Text.ParseAsDouble(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e) {
			try {
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
				int DIMS = Dimensions.Value;
				double stddevmeans = StddevMeans.Value;
				SetupDisplay(numSets);

				MersenneTwister rndG = RndHelper.ThreadLocalRandom;
				List<double[,]> pointClouds = new List<double[,]>();

				for (int si = 0; si < numSets; si++) {//create each point-set
					ThreadPool.QueueUserWorkItem((index) => {

						MersenneTwister rnd;
						lock (rndG)
							rnd = new MersenneTwister(rndG.Next());
						double[] mean = CreateGaussianCloud.RandomMean(DIMS, rnd, stddevmeans);
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
			} catch (Exception ex) {
				Console.WriteLine("Error occured!");
				Console.WriteLine(ex);
				Console.WriteLine("\nerror ignored.");
			}
		}

		private void StartLvq(List<double[,]> pointClouds) {
			int DIMS = pointClouds[0].GetLength(1);
			double[,] allpoints = new double[pointClouds.Sum(pc => pc.GetLength(0)), DIMS];
			int[] pointLabels = new int[allpoints.GetLength(0)];
			List<int> classBoundaries = new List<int> { 0 };
			int pointI = 0;
			int classLabel = 0;
			foreach (var pointCloud in pointClouds) {
				for (int i = 0; i < pointCloud.GetLength(0); i++) {
					for (int j = 0; j < DIMS; j++)
						allpoints[pointI, j] = pointCloud[i, j];
					pointLabels[pointI] = classLabel;
					pointI++;
				}
				classBoundaries.Add(pointI);
				classLabel++;
			}
			Debug.Assert(pointI == allpoints.GetLength(0));
			Debug.Assert(pointClouds[0].GetLength(1) == allpoints.GetLength(1));
			this.classBoundaries = classBoundaries.ToArray();
			Debug.Assert(this.classBoundaries.Length == classLabel + 1);
			lock (lvqSync) {
				lvqImpl = new LvqWrapper(allpoints, pointLabels, classLabel, 3);
				needUpdate = true;
			}
			UpdateDisplay();
		}

		private void UpdateDisplay() {
			double[,] currPoints = null;
			lock (lvqSync) {
				if (!needUpdate) return;
				currPoints = lvqImpl.CurrentProjection();
			}
			Dispatcher.BeginInvoke((Action)(() => {
				lock (lvqSync)
					needUpdate = false;
				for (int i = 0; i < classBoundaries.Length - 1; i++) {

					var pointsIter = Enumerable.Range(classBoundaries[i], classBoundaries[i + 1] - classBoundaries[i])
												.Select(pi => new Point(currPoints[pi, 0], currPoints[pi, 1]));

					//Console.WriteLine("Points in graph " + i + ": " + pointsIter.Count());

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
				Color[] plotcolors = GraphRandomPen.MakeDistributedColors(numClasses);
				for (int i = 0; i < numClasses; i++) {
					Pen pen = new Pen {
						Brush = new SolidColorBrush(plotcolors[i]),
						EndLineCap = PenLineCap.Round,
						StartLineCap = PenLineCap.Round,
						//EndLineCap = PenLineCap.Square,						StartLineCap = PenLineCap.Square,
						Thickness = 4.0,
					};
					pen.Freeze();
					plotControl.AddPlot(new GraphableGeometry { Geometry = GraphUtils.PointCloud(Enumerable.Empty<Point>()), Pen = pen, XUnitLabel = "X axis", YUnitLabel = "Y axis" });
				}
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
		object lvqSync = new object();
		int[] classBoundaries;
		bool needUpdate = false;
		LvqWrapper lvqImpl;

		protected override void OnInitialized(EventArgs e) {
#if  DEBUG
			textBoxPointsPerSet.Text = 20.ToString();
			textBoxDims.Text = 10.ToString();
#endif
			base.OnInitialized(e);
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

		private void doEpochButton_Click(object sender, RoutedEventArgs e) {
			int epochsTodo = EpochsPerClick ?? 1;
			ThreadPool.QueueUserWorkItem((index) => {
				lock (lvqSync) {
					using (new DTimer("Training " + epochsTodo + " epochs"))
						lvqImpl.TrainEpoch(epochsTodo);
					needUpdate = true;
				}
				UpdateDisplay();
			});
		}

		private void checkBox2_Checked(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			this.WindowStyle = WindowStyle.None;
			this.Topmost = true;
			this.WindowState = WindowState.Maximized;
		}

		private void checkBox2_Unchecked(object sender, RoutedEventArgs e) {
			this.Topmost = false;
			this.WindowStyle = WindowStyle.SingleBorderWindow;
			this.WindowState = WindowState.Normal;
		}

	}
}

//#define USEGEOMPLOT
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
using EmnExtensions;
using LVQCppCli;

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
			plotControl.AttemptBorderTicks = false;
		}

		private void textBoxNumberOfSets_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		public int? NumberOfSets { get { return textBoxNumberOfSets.Text.ParseAsInt32(); } }

		private void textBoxPointsPerSet_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		public int? PointsPerSet { get { return textBoxPointsPerSet.Text.ParseAsInt32(); } }

		private void textBoxDims_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, s => DataVerifiers.IsInt32Positive(s) && Int32.Parse(s) > 2); }
		public int? Dimensions { get { return textBoxDims.Text.ParseAsInt32(); } }

		private void textBoxEpochs_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		public int? EpochsPerClick { get { return textBoxEpochs.Text.ParseAsInt32(); } }

		private void textBoxStddevMeans_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsDoublePositive); }
		public double? StddevMeans { get { return textBoxStddevMeans.Text.ParseAsDouble(); } }

		private void textBoxProtoCount_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }
		public int? ProtoCount { get { return textBoxProtoCount.Text.ParseAsInt32(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e)
		{
			try
			{
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
				int DIMS = Dimensions.Value;
				int protoCount = ProtoCount.Value;
				double stddevmeans = StddevMeans.Value;
				SetupDisplay(numSets, pointsPerSet);

				MersenneTwister rndG = RndHelper.ThreadLocalRandom;
				List<double[,]> pointClouds = new List<double[,]>();

				for (int si = 0; si < numSets; si++)
				{//create each point-set
					ThreadPool.QueueUserWorkItem((index) =>
					{

						MersenneTwister rnd;
						lock (rndG)
							rnd = new MersenneTwister(rndG.Next());
						double[] mean = CreateGaussianCloud.RandomMean(DIMS, rnd, stddevmeans);
						double[,] trans = CreateGaussianCloud.RandomTransform(DIMS, rnd);
						double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, DIMS, trans, mean, rnd);
						lock (rndG)
							pointClouds.Add(points);

						lock (sync)
						{
							done++;
							if (done == numSets)
							{
								timer.TimeMark(null);
								new Thread(() => { StartLvq(pointClouds, protoCount); })
								{
									IsBackground = true,
								}.Start();
							}
						}
					}, si);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured!");
				Console.WriteLine(ex);
				Console.WriteLine("\nerror ignored.");
			}
		}

		private void StartLvq(List<double[,]> pointClouds, int protoCount)
		{
			int DIMS = pointClouds[0].GetLength(1);
			double[,] allpoints = new double[pointClouds.Sum(pc => pc.GetLength(0)), DIMS];
			int[] pointLabels = new int[allpoints.GetLength(0)];
			List<int> classBoundaries = new List<int> { 0 };
			int pointI = 0;
			int classLabel = 0;
			foreach (var pointCloud in pointClouds)
			{
				for (int i = 0; i < pointCloud.GetLength(0); i++)
				{
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
			lock (lvqSync)
			{
				lvqImpl = new LvqWrapper(allpoints, pointLabels, classLabel, protoCount);
				needUpdate = true;
			}
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			double[,] currPoints = null;
			int[,] closestClass = null;
			lock (lvqSync)
			{
				if (!needUpdate) return;
				currPoints = lvqImpl.CurrentProjection();
			}
			Dispatcher.BeginInvoke((Action)(() =>
			{
				lock (lvqSync)
					needUpdate = false;
				Rect bounds = Rect.Empty;
				for (int i = 0; i < classBoundaries.Length - 1; i++)
				{

					var pointsIter = Enumerable.Range(classBoundaries[i], classBoundaries[i + 1] - classBoundaries[i])
												.Select(pi => new Point(currPoints[pi, 0], currPoints[pi, 1]));

					//Console.WriteLine("Points in graph " + i + ": " + pointsIter.Count());

					if (plotControl.Graphs[i] is GraphableGeometry)
						((GraphableGeometry)plotControl.Graphs[i]).Geometry = GraphUtils.PointCloud(pointsIter);
					else
						((GraphablePixelScatterPlot)plotControl.Graphs[i]).Points = pointsIter.ToArray();
					bounds = Rect.Union(plotControl.Graphs[i].DataBounds, bounds);
				}
				lock (lvqSync)
				{
					double width = plotControl.ActualWidth, height = plotControl.ActualHeight;
					closestClass = lvqImpl.ClassBoundaries(bounds.Left, bounds.Right, bounds.Top, bounds.Bottom, (int)Math.Ceiling(width), (int)Math.Ceiling(height));
					if (plotControl.Graphs.Count > classBoundaries.Length - 1)
					{
						plotControl.Graphs.RemoveAt(classBoundaries.Length - 1);
					}
					
					Color[] colors = Enumerable.Range(0, plotControl.Graphs.Count)
						.Select(i => plotControl.Graphs[i])
						.Select(graph => graph is GraphablePixelScatterPlot ? ((GraphablePixelScatterPlot)graph).PointColor : ((SolidColorBrush)((GraphableGeometry)graph).Pen.Brush).Color)
						.Select(c => { c.ScA = 0.1f; return c; })
						.Concat(Enumerable.Repeat(Color.FromRgb(0, 0, 0), 1))
						.ToArray();
					var edges = new List<Tuple<int, int>>();
					for (int y = 1; y < closestClass.GetLength(0) - 1; y++)
						for (int x = 1; x < closestClass.GetLength(1) - 1; x++)
						{
							if (false
								//								closestClass[y, x] != closestClass[y + 1, x + 1]
								|| closestClass[y, x] != closestClass[y + 1, x]
								//							|| closestClass[y, x] != closestClass[y + 1, x - 1]
								|| closestClass[y, x] != closestClass[y, x + 1]
								|| closestClass[y, x] != closestClass[y, x - 1]
								//						|| closestClass[y, x] != closestClass[y - 1, x + 1]
								|| closestClass[y, x] != closestClass[y - 1, x]
								//					|| closestClass[y, x] != closestClass[y - 1, x - 1]
								)
								edges.Add(Tuple.Create(y, x));
						}
					foreach (var coord in edges)
						closestClass[coord.Item1, coord.Item2] = colors.Length - 1;

					plotControl.Graphs.Add(
						new GraphableBitmap
						{
							Bitmap = GraphUtils.MakeColormappedBitmap(closestClass, cLabel => colors[cLabel]),
							InnerDataBounds = bounds,
							XUnitLabel = "X axis",
							YUnitLabel = "Y axis",
						}
						);
				}
			}));
		}

		private void SetupDisplay(int numClasses, int pointsPerSetEstimate)
		{
			double thickness = 40.0 / (1 + Math.Log(Math.Max(pointsPerSetEstimate * numClasses, 1)));
			var linecap = thickness > 3 ? PenLineCap.Round : PenLineCap.Square;
			if (thickness <= 3) thickness *= 0.75;
			bool useGeom = numClasses * pointsPerSetEstimate < 20000;
			Dispatcher.BeginInvoke((Action)(() =>
			{
				plotControl.Graphs.Clear();
				Color[] plotcolors = GraphRandomPen.MakeDistributedColors(numClasses);
				for (int i = 0; i < numClasses; i++)
				{
					if (useGeom)
					{
						Pen pen = new Pen
						{
							Brush = new SolidColorBrush(plotcolors[i]),
							EndLineCap = linecap,
							StartLineCap = linecap,
							Thickness = thickness,
						};
						pen.Freeze();
						plotControl.Graphs.Add(new GraphableGeometry { Geometry = GraphUtils.PointCloud(Enumerable.Empty<Point>()), Pen = pen, XUnitLabel = "X axis", YUnitLabel = "Y axis" });
					}
					else
					{
						plotControl.Graphs.Add(
							new GraphablePixelScatterPlot
							{
								PointColor = F.Create<Color, Color>((c) => { c.ScA = 0.7f; return c; })(plotcolors[i]),
								XUnitLabel = "X axis",
								YUnitLabel = "Y axis",
								DpiX = 96.0,
								DpiY = 96.0,
								BitmapScalingMode = BitmapScalingMode.NearestNeighbor,
								CoverageRatio = 0.999,
								UseDiamondPoints = true,
								Points = new Point[] { },
							});
					}
				}

			}));
		}

		object lvqSync = new object();
		int[] classBoundaries;
		bool needUpdate = false;
		LvqWrapper lvqImpl;

		protected override void OnInitialized(EventArgs e)
		{
#if  DEBUG
			textBoxPointsPerSet.Text = 20.ToString();
			textBoxDims.Text = 10.ToString();
#endif
			base.OnInitialized(e);
		}

		private void doEpochButton_Click(object sender, RoutedEventArgs e)
		{
			int epochsTodo = EpochsPerClick ?? 1;
			ThreadPool.QueueUserWorkItem((index) =>
			{
				lock (lvqSync)
				{
					using (new DTimer("Training " + epochsTodo + " epochs"))
						lvqImpl.TrainEpoch(epochsTodo);
					needUpdate = true;
				}
				UpdateDisplay();
			});
		}

		private void checkBoxShowGridChanged(object o, RoutedEventArgs e) { plotControl.ShowGridLines = checkBoxShowGrid.IsChecked ?? plotControl.ShowGridLines; }

		private void checkBoxFullScreen_Checked(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Normal;
			this.WindowStyle = WindowStyle.None;
			this.Topmost = true;
			this.WindowState = WindowState.Maximized;
		}

		private void checkBoxFullScreen_Unchecked(object sender, RoutedEventArgs e)
		{
			this.Topmost = false;
			this.WindowStyle = WindowStyle.SingleBorderWindow;
			this.WindowState = WindowState.Normal;
		}
	}
}

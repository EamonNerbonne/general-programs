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
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;

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

		private void textBoxStarTailCount_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, s => DataVerifiers.IsInt32Positive(s) && Int32.Parse(s) > 1); }
		public int? StarTailCount { get { return textBoxStarTailCount.Text.ParseAsInt32(); } }

		private void textBoxStarRelDistance_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsDoublePositive); }
		public double? StarRelDistance { get { return textBoxStarRelDistance.Text.ParseAsDouble(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e) {
			try {
				NiceTimer timer = new NiceTimer();
				timer.TimeMark("making point clouds");

				if (!NumberOfSets.HasValue || !PointsPerSet.HasValue) {
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
				bool useGsm = checkBoxLvqGsm.IsChecked ?? false;

				SetupDisplay(numSets);

				MersenneTwister rndG = RndHelper.ThreadLocalRandom;
				List<double[,]> pointClouds = new List<double[,]>();
				transformBeforeLvq = null;
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
								new Thread(() => { StartLvq(pointClouds, protoCount, useGsm); }) {
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

		double[,] transformBeforeLvq = null;

		private void buttonGenerateStar_Click(object sender, RoutedEventArgs e) {
			try {
				NiceTimer timer = new NiceTimer();
				timer.TimeMark("making star clouds");

				if (!NumberOfSets.HasValue || !PointsPerSet.HasValue) {
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
				double starRelDist = StarRelDistance.Value;
				int starTailCount = StarTailCount.Value;
				bool useGsm = checkBoxLvqGsm.IsChecked ?? false;

				SetupDisplay(numSets);

				MersenneTwister rndG = RndHelper.ThreadLocalRandom;
				List<double[,]> pointClouds = new List<double[,]>();

				double[][,] transformMatrices;
				double[][] means;
				CreateGaussianCloud.InitStarSettings(starTailCount, DIMS, stddevmeans * starRelDist, rndG, out transformMatrices, out means, out transformBeforeLvq);


				for (int si = 0; si < numSets; si++) {//create each point-set
					ThreadPool.QueueUserWorkItem((index) => {
						MersenneTwister rnd;
						lock (rndG)
							rnd = new MersenneTwister(rndG.Next());

						double[,] points = CreateGaussianCloud.RandomStar(pointsPerSet, DIMS, transformMatrices, means, stddevmeans, rnd);

						lock (rndG)
							pointClouds.Add(points);

						lock (sync) {
							done++;
							if (done == numSets) {
								timer.TimeMark(null);
								new Thread(() => { StartLvq(pointClouds, protoCount, useGsm); }) {
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

		private void buttonLoadData_Click(object sender, RoutedEventArgs e) {
			int protoCount = ProtoCount.Value;
			bool useGsm = checkBoxLvqGsm.IsChecked ?? false;

			var fileOpenThread = new Thread(() => {
				OpenFileDialog dataFileOpenDialog = new OpenFileDialog();
				//dataFileOpenDialog.Filter = "*.data";

				if (dataFileOpenDialog.ShowDialog() == true) {
					FileInfo selectedFile = new FileInfo(dataFileOpenDialog.FileName);
					FileInfo labelFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".label");
					FileInfo dataFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".data");
					if (dataFile.Exists && labelFile.Exists) {
						var pointclouds = DataSetLoader.LoadDataset(dataFile, labelFile);
						Dispatcher.Invoke((Action)(() => {
							SetupDisplay(pointclouds.Count);
						}));
						StartLvq(pointclouds, protoCount, useGsm);
					}
				}
			});
			fileOpenThread.SetApartmentState(ApartmentState.STA);
			fileOpenThread.IsBackground = true;
			fileOpenThread.Start();
		}




		private void StartLvq(List<double[,]> pointClouds, int protoCount, bool useGsm, List<double[,]> pointTestClouds = null) {
			int DIMS = pointClouds[0].GetLength(1);
			double[,] allpoints = new double[pointClouds.Sum(pc => pc.GetLength(0)), DIMS];
			int[] pointLabels = new int[allpoints.GetLength(0)];
			int pointI = 0;
			int classLabel = 0;
			foreach (var pointCloud in pointClouds) {
				for (int i = 0; i < pointCloud.GetLength(0); i++) {
					for (int j = 0; j < DIMS; j++)
						allpoints[pointI, j] = pointCloud[i, j];
					pointLabels[pointI] = classLabel;
					pointI++;
				}
				classLabel++;
			}
			Debug.Assert(pointI == allpoints.GetLength(0));
			Debug.Assert(pointClouds[0].GetLength(1) == allpoints.GetLength(1));
			LvqDataSet = LvqDataSetCli.ConstructFromArray(allpoints, pointLabels, classLabel);
			LvqModel = new LvqWrapper(LvqDataSet, protoCount, useGsm);
			needUpdate = true;
			UpdateDisplay();
		}

		private void UpdateDisplay() {
			if (!needUpdate)
				return;
			double[,] currPoints = LvqModel.CurrentProjection();
			int[] labels = LvqModel.TrainingSet.ClassLabels();
			Dictionary<int, Point[]> projectedPointsByLabel =
				labels
				.Select((label, i) => new { Label = label, Point = new Point(currPoints[i, 0], currPoints[i, 1]) })
				.GroupBy(labelledPoint => labelledPoint.Label, labelledPoint => labelledPoint.Point)
				.ToDictionary(group => group.Key, group => group.ToArray());
			Dispatcher.BeginInvoke((Action)(() => {
				needUpdate = false;
				foreach (var pointGroup in projectedPointsByLabel)
					((IPlotWriteable<Point[]>)plotControl.Graphs[pointGroup.Key]).Data = pointGroup.Value;
				((IPlotWriteable<LvqWrapper>)plotControl.Graphs[LvqModel.TrainingSet.ClassCount]).TriggerDataChanged();
			}));
		}

		int currentClassCount = 0;
		struct ClassTag { public int Label; public ClassTag(int label) { Label = label; } }
		private void SetupDisplay(int numClasses) {
			Dispatcher.BeginInvoke((Action)(() => {
				currentClassCount = numClasses;
				plotControl.Graphs.Clear();
				for (int i = 0; i < numClasses; i++) {
					var plot = PlotData.Create(new Point[] { });
					plot.Tag = new ClassTag(i);
					plotControl.Graphs.Add(plot);
				}
				plotControl.Graphs.Add(
					PlotData.Create(LvqModel, UpdateClassBoundaries));
				plotControl.AutoPickColors();
			}));
		}

		void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, LvqWrapper ignore) {
#if DEBUG
			int renderwidth = (width + 7) / 8;
			int renderheight = (height + 7) / 8;
#else
			int renderwidth = width;
			int renderheight = height;
#endif
			if (LvqModel == null)
				return;
			Matrix bmpToData = dataToBmp;
			bmpToData.Invert();
			Point topLeft = bmpToData.Transform(new Point(0.0, 0.0));
			Point botRight = bmpToData.Transform(new Point(width, height));
			int[,] closestClass = LvqModel.ClassBoundaries(topLeft.X, botRight.X, topLeft.Y, botRight.Y, renderwidth, renderheight);

			uint[] nativeColor = (
				from graph in plotControl.Graphs.Cast<IPlotWithSettings>()
				where graph.Tag is ClassTag && graph.VizSupportsColor
				let label = ((ClassTag)graph.Tag).Label
				orderby label
				select graph.RenderColor ?? Colors.Black
				)
				.Select(c => { c.ScA = 0.1f; return c; })
				.Concat(Enumerable.Repeat(Color.FromRgb(0, 0, 0), 1))
				.Select(c => c.ToNativeColor())
				.ToArray();

			var edges = new List<Tuple<int, int>>();
			for (int y = 1; y < closestClass.GetLength(0) - 1; y++)
				for (int x = 1; x < closestClass.GetLength(1) - 1; x++) {
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
				closestClass[coord.Item1, coord.Item2] = nativeColor.Length - 1;
			uint[] classboundaries = new uint[width * height];
			int px = 0;
			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
					classboundaries[px++] = nativeColor[closestClass[y * renderheight / height, x * renderwidth / width]];
			bmp.WritePixels(new Int32Rect(0, 0, width, height), classboundaries, width * 4, 0);
			//return BitmapSource.Create(w, h, 96.0, 96.0, PixelFormats.Bgra32, null, inlinearray, w * 4);
		}

		//object lvqSync = new object();
		volatile bool needUpdate = false;
		LvqWrapper LvqModel;

		LvqDataSetCli LvqDataSet;



		protected override void OnInitialized(EventArgs e) {
#if  DEBUG
			textBoxPointsPerSet.Text = 20.ToString();
			textBoxDims.Text = 10.ToString();
#endif
			base.OnInitialized(e);
		}

		private void doEpochButton_Click(object sender, RoutedEventArgs e) {

			int epochsTodo = EpochsPerClick ?? 1;
			ThreadPool.QueueUserWorkItem((index) => {
				lock (LvqModel.UpdateSyncObject)
					using (new DTimer("Training " + epochsTodo + " epochs"))
						LvqModel.TrainEpoch(epochsTodo);
				needUpdate = true;
				UpdateDisplay();
			});
		}

		private void checkBoxShowGridChanged(object o, RoutedEventArgs e) { plotControl.ShowGridLines = checkBoxShowGrid.IsChecked ?? plotControl.ShowGridLines; }

		private void checkBoxFullScreen_Checked(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Normal;
			this.WindowStyle = WindowStyle.None;
			this.Topmost = true;
			this.WindowState = WindowState.Maximized;
		}

		private void checkBoxFullScreen_Unchecked(object sender, RoutedEventArgs e) {
			this.Topmost = false;
			this.WindowStyle = WindowStyle.SingleBorderWindow;
			this.WindowState = WindowState.Normal;
		}

	}
}

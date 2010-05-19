//#define USEGEOMPLOT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EmnExtensions.DebugTools;
using EmnExtensions.MathHelpers;
using EmnExtensions.Text;
using EmnExtensions.Wpf;
using EmnExtensions.Wpf.Plot;
using LvqLibCli;
using Microsoft.Win32;

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

		private void textBoxStarTailCount_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, s => DataVerifiers.IsInt32Positive(s) && Int32.Parse(s) > 1); }
		public int? StarTailCount { get { return textBoxStarTailCount.Text.ParseAsInt32(); } }

		private void textBoxStarRelDistance_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsDoublePositive); }
		public double? StarRelDistance { get { return textBoxStarRelDistance.Text.ParseAsDouble(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				if (!NumberOfSets.HasValue || !PointsPerSet.HasValue)
				{
					Console.WriteLine("Invalid initialization values");
					return;
				}

				int numSets = NumberOfSets.Value;
				int pointsPerSet = PointsPerSet.Value;
				int dims = Dimensions.Value;
				int protoCount = ProtoCount.Value;
				double stddevmeans = StddevMeans.Value;
				bool useGsm = checkBoxLvqGsm.IsChecked ?? false;
				SetupDisplay(numSets);
				ThreadPool.QueueUserWorkItem((ignore) =>
				{
					LvqDataSetCli dataset = DTimer.TimeFunc(() => LvqDataSetCli.ConstructGaussianClouds("oldstyle-dataset",
						RndHelper.MakeSecureUInt, dims, numSets, pointsPerSet, stddevmeans), "Constructing dataset");

					Console.WriteLine("RngUsed: " + RndHelper.usages);
					StartLvq(dataset, protoCount, useGsm);
				}, 0);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured!");
				Console.WriteLine(ex);
				Console.WriteLine("\nerror ignored.");
			}
		}


		private void buttonGenerateStar_Click(object sender, RoutedEventArgs e)
		{
			try
			{

				if (!NumberOfSets.HasValue || !PointsPerSet.HasValue)
				{
					Console.WriteLine("Invalid initialization values");
					return;
				}

				int numSets = NumberOfSets.Value;
				int pointsPerSet = PointsPerSet.Value;
				int dims = Dimensions.Value;
				int protoCount = ProtoCount.Value;
				double stddevmeans = StddevMeans.Value;
				double starRelDist = StarRelDistance.Value;
				int starTailCount = StarTailCount.Value;
				bool useGsm = checkBoxLvqGsm.IsChecked ?? false;

				SetupDisplay(numSets);

				ThreadPool.QueueUserWorkItem((ignore) =>
				{

					LvqDataSetCli dataset =
						DTimer.TimeFunc(() => LvqDataSetCli.ConstructStarDataset("oldstyle-dataset",
						RndHelper.MakeSecureUInt, dims, 2, starTailCount, numSets, pointsPerSet, stddevmeans * starRelDist, 1.0 / starRelDist), "making star clouds");

					StartLvq(dataset, protoCount, useGsm);
				}, 0);

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error occured!");
				Console.WriteLine(ex);
				Console.WriteLine("\nerror ignored.");
			}
		}

		private void buttonLoadData_Click(object sender, RoutedEventArgs e)
		{
			int protoCount = ProtoCount.Value;
			bool useGsm = checkBoxLvqGsm.IsChecked ?? false;

			var fileOpenThread = new Thread(() =>
			{
				OpenFileDialog dataFileOpenDialog = new OpenFileDialog();
				//dataFileOpenDialog.Filter = "*.data";

				if (dataFileOpenDialog.ShowDialog() == true)
				{
					FileInfo selectedFile = new FileInfo(dataFileOpenDialog.FileName);
					FileInfo labelFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".label");
					FileInfo dataFile = new FileInfo(selectedFile.Directory + @"\" + Path.GetFileNameWithoutExtension(selectedFile.Name) + ".data");
					if (dataFile.Exists && labelFile.Exists)
					{
						try
						{
							var pointclouds = DataSetLoader.LoadDataset(dataFile, labelFile);
							SetupDisplay(pointclouds.Item3);
							var dataset = LvqDataSetCli.ConstructFromArray(dataFile.Name, pointclouds.Item1, pointclouds.Item2, pointclouds.Item3);
							StartLvq(dataset, protoCount, useGsm);
						}
						catch (FileFormatException fe)
						{
							Console.WriteLine("Can't load file: {0}", fe.ToString());
						}
					}
				}
			});
			fileOpenThread.SetApartmentState(ApartmentState.STA);
			fileOpenThread.IsBackground = true;
			fileOpenThread.Start();
		}

		private void StartLvq(LvqDataSetCli newDataset, int protosPerClass, bool useGsm, LvqDataSetCli testDataset = null)
		{
			LvqDataSet = newDataset;
			LvqModel = new LvqModelCli(RndHelper.MakeSecureUInt, RndHelper.MakeSecureUInt, newDataset.Dimensions, newDataset.ClassCount, protosPerClass, useGsm ? LvqModelCli.GSM_TYPE : LvqModelCli.G2M_TYPE);
			//LvqModel
			needUpdate = true;
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			if (!needUpdate)
				return;
			double[,] currPoints = LvqModel.CurrentProjectionOf(LvqDataSet);
			if (currPoints == null) return;//model not initialized

			int[] labels = LvqDataSet.ClassLabels();
			Dictionary<int, Point[]> projectedPointsByLabel =
				labels
				.Select((label, i) => new { Label = label, Point = new Point(currPoints[i, 0], currPoints[i, 1]) })
				.GroupBy(labelledPoint => labelledPoint.Label, labelledPoint => labelledPoint.Point)
				.ToDictionary(group => group.Key, group => group.ToArray());

			var prototypePositionsRaw = LvqModel.PrototypePositions().Item1;
			var prototypePositions =
				Enumerable.Range(0, prototypePositionsRaw.GetLength(0))
				.Select(i => new Point(prototypePositionsRaw[i, 0], prototypePositionsRaw[i, 1]))
				.ToArray();


			Dispatcher.BeginInvoke((Action)(() =>
			{
				needUpdate = false;
				foreach (var pointGroup in projectedPointsByLabel)
					((IPlotWriteable<Point[]>)plotControl.Graphs[pointGroup.Key]).Data = pointGroup.Value;
				((IPlotWriteable<Point[]>)plotControl.Graphs[LvqDataSet.ClassCount + 1]).Data = prototypePositions;
				((IPlotWriteable<LvqModelCli>)plotControl.Graphs[LvqDataSet.ClassCount]).TriggerDataChanged();
			}));
		}

		struct ClassTag { public int Label; public ClassTag(int label) { Label = label; } }
		private void SetupDisplay(int numClasses)
		{
			Dispatcher.BeginInvoke((Action)(() =>
			{
				plotControl.Graphs.Clear();
				for (int i = 0; i < numClasses; i++)
				{
					var plot = PlotData.Create(new Point[] { });
					plot.Tag = new ClassTag(i);
					plotControl.Graphs.Add(plot);
				}
				plotControl.AutoPickColors();

				plotControl.Graphs.Add(PlotData.Create(default(LvqModelCli), UpdateClassBoundaries));
				var prototypePositions = PlotData.Create(new Point[] { });
				prototypePositions.ZIndex = 1;
				var prototypePositionsVisualizer = new EmnExtensions.Wpf.Plot.VizEngines.VizPixelScatterGeom();
				prototypePositionsVisualizer.OverridePointCountEstimate = 100;
				prototypePositions.Visualizer = prototypePositionsVisualizer;
				plotControl.Graphs.Add(prototypePositions); //prototype centers.
			}));
		}

		void UpdateClassBoundaries(WriteableBitmap bmp, Matrix dataToBmp, int width, int height, LvqModelCli ignore)
		{
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
		LvqModelCli LvqModel;

		LvqDataSetCli LvqDataSet;



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
				lock (LvqModel.UpdateSyncObject)
					using (new DTimer("Training " + epochsTodo + " epochs"))
						LvqModel.Train(epochsTodo, LvqDataSet);
				needUpdate = true;
				UpdateDisplay();
			});
		}

		private WindowState lastState = WindowState.Normal;
		private void checkBoxFullScreen_Checked(object sender, RoutedEventArgs e)
		{
			lastState = this.WindowState;
			this.WindowState = WindowState.Normal;
			this.WindowStyle = WindowStyle.None;
			this.Topmost = true;
			this.WindowState = WindowState.Maximized;
		}

		private void checkBoxFullScreen_Unchecked(object sender, RoutedEventArgs e)
		{
			this.Topmost = false;
			this.WindowStyle = WindowStyle.SingleBorderWindow;
			this.WindowState = lastState;
		}
	}
}

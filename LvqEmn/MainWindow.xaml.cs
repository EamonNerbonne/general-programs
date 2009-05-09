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
using System.Threading.Tasks;

namespace LVQeamon
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow() {
			InitializeComponent();
		}

		private void textBoxNumberOfSets_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }

		private void textBoxPointsPerSet_TextChanged(object sender, TextChangedEventArgs e) { DataVerifiers.VerifyTextBox((TextBox)sender, DataVerifiers.IsInt32Positive); }

		public int? NumberOfSets { get { return textBoxNumberOfSets.Text.ParseAsInt32(); } }
		public int? PointsPerSet { get { return textBoxPointsPerSet.Text.ParseAsInt32(); } }

		private void buttonGeneratePointClouds_Click(object sender, RoutedEventArgs e) {
			plotControl.Graphs.Clear();
			NiceTimer timer = new NiceTimer(); timer.TimeMark("making point clouds");
			if (!NumberOfSets.HasValue || !PointsPerSet.HasValue) {
				Console.WriteLine("Invalid initialization values");
				return;
			}

			object sync = new object();
			int done = 0;
			int numSets = NumberOfSets.Value;
			int pointsPerSet = PointsPerSet.Value;



			for (int si = 0; si < numSets; si++) {//create each point-set
				ThreadPool.QueueUserWorkItem((index) => {

					double[] mean = CreateGaussianCloud.RandomMean(2, RndHelper.ThreadLocalRandom);
					double[,] trans = CreateGaussianCloud.RandomTransform(2, RndHelper.ThreadLocalRandom);
					double[,] points = CreateGaussianCloud.GaussianCloud(pointsPerSet, 2, trans, mean, RndHelper.ThreadLocalRandom);



					Dispatcher.BeginInvoke((Action)(() => {
						Geometry pointCloud = GraphUtils.PointCloud(
						Enumerable.Range(0, pointsPerSet)
							.Select(i => new Point(points[i, 0], points[i, 1]))
							);
						//pointCloud.Transform = new MatrixTransform( GraphUtils.TransformShape(pointCloud.Bounds, new Rect(0,0,500, 500), true));
						plotControl.Graphs.Add(new GraphGeometryControl() { GraphGeometry = pointCloud, Name = "g" + si, PenThickness = 2.0 });
						lock (sync) {
							done++;
							if (done == numSets) {
								timer.TimeMark(null);
								renderCount = 0;
							}
						}
					}));

				}, si);
			}
		}
		NiceTimer overall;
		protected override void OnInitialized(EventArgs e) {
			textBoxPointsPerSet.Text = 100000.ToString();
			overall = new NiceTimer();
			overall.TimeMark("Sizing");
			base.OnInitialized(e);
			buttonGeneratePointClouds_Click(null, null);
			Dispatcher.BeginInvoke((Action)DoSizingTest);
		}

		volatile int renderCount = 0;
		bool completedTest = false;

		void DoSizingTest() {
			if (Width + Height > 1600) {
				if (!completedTest) {
					completedTest = true;
					overall.TimeMark(null);
					Close();
				}
			}
			else {
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

	}
}

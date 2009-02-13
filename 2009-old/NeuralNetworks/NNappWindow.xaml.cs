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
using EmnExtensions.MathHelpers;
using System.Threading;
using EmnExtensions.DebugTools;
using EmnExtensions;
using System.IO;
using EmnExtensions.Wpf;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace NeuralNetworks
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class NNappWindow : Window
	{

		const int points = 1000;
		static void calcMV(Random r, double[] mean, double[] vars) {
			double xSum = 0.0, x2Sum = 0.0;
			for (int i = 0; i < mean.Length; i++) {
				double newVal = r.NextNorm();
				xSum += newVal;
				x2Sum += newVal * newVal;
				mean[i] = xSum / (i + 1);
				vars[i] = i == 0 ? 0 : (x2Sum / (i + 1) - mean[i] * mean[i]) * (i + 1) / i;
			}
		}
		const int iters = 1000;
		const int parLev = 8;
		public NNappWindow() {
			InitializeComponent();
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
		}

		[ThreadStatic]
		static MersenneTwister randomImpl;
		static Random Random { get { if (randomImpl == null) randomImpl = new MersenneTwister(); return randomImpl; } }

		void MakeSuccessPlot(int N, bool useCoM) {
#if DEBUG
			int epochMax = 1000;
			int nD = 100;
#else
			int epochMax = 100000;
			int nD = 5000;
#endif
			double ComputeExtent = Math.Sqrt(30.0 * N);
			int stepSize = Math.Max((int)(ComputeExtent/10),1);



			var plotLine = (
				from P in Enumerable.Range(1, 6*N).Select(p=>p*stepSize).TakeWhile(p=>p<2*N+ComputeExtent).AsParallel(4)
				let ratio = DataSet.FractionManageable(N, P, nD, epochMax, useCoM, ()=>Random)
				let alpha = P / (double)N
				orderby alpha ascending
				select new Point(alpha, ratio)
				).ToArray();

			Dispatcher.Invoke((Action)(() => {
				var g = plotControl.NewGraph("PerceptronStorage", plotLine);
				g.GraphBounds = new Rect(new Point(0.8, 1.0001), new Point(3.2, 0.0));
				plotControl.ShowGraph(g);
				g.XLabel = "α = P/N for N = " + N;
				g.YLabel = "successful storage ratio";
				string fileName = "PerceptronStorage_N" + N + "_eM" + epochMax + "_nD" + nD + ".xps";
				using (var writestream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
					plotControl.Print(g, writestream);
			}));
		}


		void MakeMinOverPlot(int N, bool useCoM) {
			int epochMax = 3000;
			int nD = 500;

			var plotLine = F.Create(() => (
				from P in Enumerable.Range(8, 23).Select(p => p * N / 10).AsParallel(8)
				let stability = DataSet.AverageStability(N, P, nD, epochMax, useCoM, Random)
				let alpha = P / (double)N
				orderby alpha ascending
				select new { Point = new Point(alpha, stability.val), Err = stability.err }
				).ToArray()
				).Time(timespan => {
					Console.WriteLine("Computation took {0}.", timespan);
				});

			Dispatcher.Invoke((Action)(() => {
				var g = new GraphControl();
				g.Name = "StabilityPlot";
				g.NewLine(
					GraphControl.LineWithErrorBars(
					  plotLine.Select(pe => pe.Point).ToArray(),
					  plotLine.Select(pe => pe.Err).ToArray()));
				g.XLabel = "α = P/N for N = " + N;
				g.YLabel = "expected stability";
				plotControl.Graphs.Add(g);

				//g.GraphBounds = new Rect(new Point(0.5, 1.01), new Point(3.0, 0.0));
				plotControl.ShowGraph(g);
				string fileName = "MinOverStability_N" + N + "_eM" + epochMax + "_nD" + nD + ".xps";
				using (var writestream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
					plotControl.Print(g, writestream);
			}));
		}



		void RandomTester() { //must execute on UI thread.
			Random r1 = new MersenneTwister();
			var meanSumVec = new double[points];
			var varSumVec = new double[points];

			Parallel.Invoke(Enumerable.Repeat<Action>(() => {
				Random r;
				lock (meanSumVec) {
					r = new MersenneTwister(r1.Next());
				}
				var meanSumVecI = new double[points];
				var varSumVecI = new double[points];
				var cM = new double[points];
				var cV = new double[points];
				for (int i = 0; i < iters; i++) {
					calcMV(r, cM, cV);
					meanSumVecI.AddTo(cM);
					varSumVecI.AddTo(cV);
				}

				meanSumVecI.ScaleTo(1.0 / iters);
				varSumVecI.ScaleTo(1.0 / iters);
				lock (meanSumVec) {
					meanSumVec.AddTo(meanSumVecI);
					varSumVec.AddTo(varSumVecI);
				}
			}, parLev).ToArray());

			meanSumVec.ScaleTo(1.0 / parLev);
			varSumVec.ScaleTo(1.0 / parLev);
			var meanPlot = plotControl.NewGraph("mean", meanSumVec.Select((meanV, i) => new Point(i + 1, meanV)));
			var meanDev = 2 * (Math.Abs(meanSumVec[points / 4] - 0) + Math.Abs(meanSumVec[points / 2] - 0) + Math.Abs(meanSumVec[points / 3] - 0));
			var meanBounds = meanPlot.GraphBounds;
			meanPlot.GraphBounds = new Rect(0, 0 - meanDev, points, 2 * meanDev);

			var varPlot = plotControl.NewGraph("var", varSumVec.Select((varV, i) => new Point(i + 1, varV)));
			var varDev = 2 * (Math.Abs(varSumVec[points / 4] - 1) + Math.Abs(varSumVec[points / 2] - 1) + Math.Abs(varSumVec[points / 3] - 1));
			var varBounds = varPlot.GraphBounds;
			varPlot.GraphBounds = new Rect(0, 1 - varDev, points, 2 * varDev);

			plotControl.ShowGraph(meanPlot);
			plotControl.ShowGraph(varPlot);
		}

		private void AverageStability_Click(object sender, RoutedEventArgs e) {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread((ThreadStart)(() => {
				Parallel.ForEach(new[] { 20, 50, 80, 120 }, N => {
					MakeMinOverPlot(N, useCoM);
				});
			})) {
				IsBackground = true
			}.Start();
		}

		private void FractionManagable_Click(object sender, RoutedEventArgs e) {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread((ThreadStart)(() => {
				NiceTimer.Time("FracManagable", () => {
					foreach(int N in new[] { 10, 20, 50, 80, 120, 200 })
						MakeSuccessPlot(N, useCoM);
				});
			})) {
				IsBackground = true
			}.Start();
		}

		static int errGcnt = 0;
		private void LearnOne_Click(object sender, RoutedEventArgs e) {
			//GraphControl errGraph = new GraphControl();
			//errGraph.XLabel = "Epoch";
			//errGraph.YLabel = "errRate";
			//errGraph.Name = "errGraph" + (errGcnt++);
			//plotControl.Graphs.Add(errGraph);
			//plotControl.ShowGraph(errGraph);

			new Thread(() => {
				NiceTimer.Time("NN:", () => {
					int fp = 0;
					int fn = 0;
					object synroot = new object();

					long stoppedAt = 0;
					long doneCnt = 0;
					long completedAt = 0;
					long complCnt = 0;
					Parallel.For(0, 1000, iterI => {
						int epochMax = 1000000;
						int N = 50;
						int P = 120;
						DataSet D = new DataSet(N, P, Random);
						SimplePerceptron w = new SimplePerceptron(D.ComputCenterOfMass());
						//List<Point> errP = new List<Point>();
						double lastErrN = double.MinValue;
						int dipCnt = 0;
						int firstHit = 0;
						int lastHit = 0;
						int epochToConverge = w.DoTraining(D, epochMax, (epochN, errN) => {
							if (errN < lastErrN)
								dipCnt++;
							lastErrN = errN;

							if (epochN > 10 && (dipCnt / (double)epochN) > 0.40) {
								lastHit = epochN;
								if (firstHit == 0)
									firstHit = epochN;
								if ((dipCnt / (double)epochN) > 0.45)
									return true;
							}
							return false;
						});
						lock (synroot) {
							if (epochToConverge <= 0) {
								if (firstHit == 0) {//did not converge but was predicted to.
									fp++;
									Console.WriteLine("{4} ??? <{0},{1}:{2}>,{3}", firstHit, lastHit, dipCnt, -epochToConverge, -dipCnt / (double)epochToConverge);
								}
							} else {
								if (firstHit > 0) {//did converge but wasn't predicted to.
									fn++;
									Console.WriteLine("{4} ::: <{0},{1}:{2}>,{3}", firstHit, lastHit, dipCnt, epochToConverge, dipCnt / (double)epochToConverge);
								}
							}
							if (epochToConverge <= 0) {
								stoppedAt += firstHit;
								doneCnt++;
								if (doneCnt % 100 == 0) {
									Console.WriteLine("meanStop:{0}", stoppedAt / (double)doneCnt);
								}
							} else {
								completedAt += epochToConverge;
								complCnt++;
								if (complCnt % 100 == 0) {
									Console.WriteLine("meanCompl:{0}", completedAt / (double)complCnt);
								}
							}
						}


						//int pointN = 1000;
						//List<Point> errGP = new List<Point>();
						//if (errP.Count <= pointN)
						//    errGP = errP;
						//else
						//    for (int i = 0; i < pointN; i++) {
						//        int eS = i * errP.Count / pointN;
						//        int eF = (i + 1) * errP.Count / pointN;
						//        double xs = 0, ys = 0;
						//        for (int p = eS; p < eF; p++) {
						//            xs += errP[p].X;
						//            ys += errP[p].Y;
						//        }
						//        errGP.Add(new Point(xs / (eF - eS), ys / (eF - eS)));
						//    }


						//Dispatcher.Invoke((Action)(() => {
						//    foreach (var point in errGP)
						//        errGraph.AddPoint(point);
						//    var bounds = errGraph.GraphBounds;
						//    bounds.Union(new Point(0, 0));
						//    //bounds.Union(new Point(1, 1));
						//    errGraph.GraphBounds = bounds;

						//}));
					});
					Console.WriteLine("meanStop:{0}", stoppedAt / (double)doneCnt);
					Console.WriteLine("meanCompl:{0}", completedAt / (double)complCnt);

					Console.WriteLine("Convergence; {0} false positives, {1} false negatives", fp, fn);
				});
			}) { IsBackground = true }.Start();
		}

		static Regex analConvRegex = new Regex(
			@"^(
              (?<fail> \(\d+\:\d+\) )
             |(?<suc> \[\d+\:\d+\] )
             |(?<fp> \<\d+\:\d+\>[^=]+==\d+)
             |\s
             )+$", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture
			);
		private void AnalyzeConvergenceLog(string log) {


		}
	}
}

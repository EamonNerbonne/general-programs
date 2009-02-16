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
			TrainingSettings settings = new TrainingSettings {
#if DEBUG
				MaxEpoch = 10,
				TrialRuns = 3,
#else
				MaxEpoch= 100000,
				TrialRuns = 5000,
#endif
				N = N,
				UseCenterOfMass = useCoM,
			};

			var plotLine = (
				from Psettings in settings.SettingsWithReasonableP.AsParallel(4)
				let ratio = DataSet.FractionManageable(Psettings, () => Random)
				let alpha = Psettings.P / (double)N
				orderby alpha ascending
				select new Point(alpha, ratio)
				).ToArray();

			Dispatcher.Invoke((Action)(() => {
				var g = plotControl.NewGraph("PerceptronStorage", plotLine);
				g.GraphBounds = new Rect(new Point(0.8, 1.0001), new Point(3.2, 0.0));
				plotControl.ShowGraph(g);
				g.XLabel = "α = P/N for N = " + N;
				g.YLabel = "successful storage ratio";
				string fileName = "PerceptronStorage_N" + N + "_eM" + settings.MaxEpoch + "_nD" + settings.TrialRuns + ".xps";
				using (var writestream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
					plotControl.Print(g, writestream);
			}));
		}


		void MakeMinOverPlot(int N, bool useCoM) {
			TrainingSettings settings = new TrainingSettings {
				MaxEpoch = 4000,
#if DEBUG
				TrialRuns = 3,
#else
				TrialRuns = 2000,
#endif
				N = N,
				UseCenterOfMass = useCoM,
			};

/*			List<Point> plotLine2 = new List<Point>();
			List<double> err = new List<double>();
			foreach(var Psettings in settings.SettingsWithReasonableP) {
				var stability = DataSet.AverageStability(Psettings, () => Random);
				var alpha = Psettings.P / (double)Psettings.N;
				lock (plotLine2) {
					plotLine2.Add(new Point(alpha, stability.val));
					err.Add(stability.err);
				}
			}*/
			
				
			var plotLine =(
				from Psettings in settings.SettingsWithReasonableP
//#if !DEBUG
					.Reverse().AsParallel()
//#endif
				let stability = DataSet.AverageStability(Psettings, ()=> Random)
				let alpha = Psettings.P / (double)Psettings.N
			//	orderby alpha ascending
				select new { Point = new Point(alpha, stability.val), Err = stability.err }
				).ToArray();
			/*
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
				string fileName = "MinOverStability_N" + N + "_eM" + settings.MaxEpoch + "_nD" + settings.TrialRuns + ".xps";
				using (var writestream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
					plotControl.Print(g, writestream);
			}));*/
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
				NiceTimer.Time("AvgStab", () => {
					Parallel.ForEach(new[] { 20, 50, 80, 120 }.Reverse(), N => {
						MakeMinOverPlot(N, useCoM);
					});
				});
			})) {
				IsBackground = true
			}.Start();
		}

		private void FractionManagable_Click(object sender, RoutedEventArgs e) {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread((ThreadStart)(() => {
				NiceTimer.Time("FracManagable", () => {
					foreach (int N in new[] { 10, 20, 50, 80, 120 })
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
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread(() => {
				NiceTimer.Time("NN:", () => {
					int fp = 0;
					int fn = 0;
					object synroot = new object();

					long stoppedAt = 0;
					long doneCnt = 0;
					long completedAt = 0;
					long complCnt = 0;
					TrainingSettings settings = new TrainingSettings {
						MaxEpoch = 1000000,
						N = 50,
						P = 120,
						TrialRuns = 1000,
						UseCenterOfMass = useCoM
					};

					Parallel.For(0, 1000, iterI => {
						DataSet D = new DataSet(settings, Random);
						SimplePerceptron w = D.InitializeNewPerceptron(settings.UseCenterOfMass);
						//List<Point> errP = new List<Point>();
						double lastErrN = double.MinValue;
						int dipCnt = 0;
						int firstHit = 0;
						int lastHit = 0;
						int epochToConverge = w.DoTraining(D, settings.MaxEpoch, (epochN, errN) => {
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

		private void saveAsXPS_Click(object sender, RoutedEventArgs e) {
			plotControl.PrintThis();
		}
	}
}

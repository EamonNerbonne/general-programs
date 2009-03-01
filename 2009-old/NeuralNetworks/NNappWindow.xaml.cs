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
using EmnExtensions.Filesystem;
using System.IO;
using EmnExtensions.Wpf;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

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
			//commandChooser.ItemsSource = Actions;
			//RealWorldGradientDescent();
		}

		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);
			//Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
		}

		[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
		sealed class MakeButtonAttribute : Attribute
		{
			public MakeButtonAttribute(string label) { Label = label; }
			public MakeButtonAttribute() { }
			public string Label { get; set; }
		}
		public class LabelledAction { public string Label { get; set; } public Action Action { get; set; } }

		public IEnumerable<LabelledAction> Actions {
			get {
				return
					from mInfo in GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
					let attr = (MakeButtonAttribute)mInfo.GetCustomAttributes(typeof(MakeButtonAttribute), true).FirstOrDefault()
					where attr != null
					let label = attr.Label ?? mInfo.Name
					select new LabelledAction {
						Action = (Action)Delegate.CreateDelegate(typeof(Action), this, mInfo),
						Label = label,
					};
			}
		}

		private void ExecuteButton_Click(object sender, RoutedEventArgs e) {
			var button = sender as Button;
			if (button == null) return;
			var action = button.DataContext as LabelledAction;
			if (action == null) return;
			action.Action();
		}

		void MakeSuccessPlot(int N, bool useCoM) {
			TrainingSettings settings = new TrainingSettings {
#if DEBUG
				MaxEpoch = 10,
				TrialRuns = 3,
#else
				MaxEpoch = 100000,
				TrialRuns = 5000,
#endif
				N = N,
				UseCenterOfMass = useCoM,
			};

			var plotLine = (
				from Psettings in settings.SettingsWithReasonableP.AsParallel(4)
				let ratio = DataSet.FractionManageable(Psettings, RndHelper.GetThreadLocalRandom)
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


			var plotLine = (
				from Psettings in settings.SettingsWithReasonableP
					//#if !DEBUG
					.Reverse().AsParallel()
				//#endif
				let stability = DataSet.AverageStability(Psettings, RndHelper.GetThreadLocalRandom)
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

		[MakeButton]
		void SaveAsXPS() { plotControl.PrintThis(); }


		[MakeButton]
		private void AverageStability() {
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

		[MakeButton]
		private void FractionManagable() {
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

		//static int errGcnt = 0;
		[MakeButton]
		private void LearnOne() {
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
						DataSet D = new DataSet(settings, RndHelper.ThreadLocalRandom);
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


		static Regex mologNameRegex = new Regex(@"^N_(?<N>\d+)_P_(?<P>\d+)_E_(?<E>\d+)\.molog$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		[MakeButton]
		private void MakeStabilityGraphs() {
			var q =
				from file in new DirectoryInfo(".").GetFiles("*.molog")
				let nameMatch = mologNameRegex.Match(file.Name)
				where nameMatch.Success
				let N = int.Parse(nameMatch.Groups["N"].Value)
				let P = int.Parse(nameMatch.Groups["P"].Value)
				let eM = int.Parse(nameMatch.Groups["E"].Value)
				let stabilities = (
					from mologRow in file.GetLines()
					where mologRow.Trim().Length > 0
					let vals = mologRow.Split('&')
					select new {
						finalStability = double.Parse(vals[0].Trim()),
						bestStability = double.Parse(vals[1].Trim())
					}
					).ToArray()
				let runs = stabilities.Length
				let corrFactor = runs / (double)(runs - 1)
				let finalMean = stabilities.Average(s => s.finalStability)
				let finalMedian = stabilities.Select(s => s.finalStability).OrderBy(s => s).Skip((runs - 1) / 2).Take(2 - (runs % 2)).Average()
				let finalVar = corrFactor * (stabilities.Average(s => s.finalStability * s.finalStability) - finalMean * finalMean)
				let finalSEM = Math.Sqrt(finalVar / runs)
				let bestMean = stabilities.Average(s => s.bestStability)
				let bestMedian = stabilities.Select(s => s.bestStability).OrderBy(s => s).Skip((runs - 1) / 2).Take(2 - (runs % 2)).Average()
				let bestVar = corrFactor * (stabilities.Average(s => s.bestStability * s.bestStability) - bestMean * bestMean)
				let bestSEM = Math.Sqrt(bestVar / runs)
				group new {
					N = N,
					eM = eM,
					alpha = P / (double)N,
					runs = runs,
					finalMean = finalMean,
					finalMedian = finalMedian,
					finalSEM = finalSEM,
					bestMean = bestMean,
					bestSEM = bestSEM,
					bestMedian = bestMedian,
				} by new { N = N, eM = eM } into graph
				let orderedGraph = graph.OrderBy(point => point.alpha).ToArray()
				let finalPoints = orderedGraph.Select(p => new Point(p.alpha, p.finalMean)).ToArray()
				let finalMedianPoints = orderedGraph.Select(p => new Point(p.alpha, p.finalMedian)).ToArray()
				let finalSEMs = orderedGraph.Select(p => p.finalSEM).ToArray()
				let bestPoints = orderedGraph.Select(p => new Point(p.alpha, p.bestMean)).ToArray()
				let bestMedianPoints = orderedGraph.Select(p => new Point(p.alpha, p.bestMedian)).ToArray()
				let bestSEMs = orderedGraph.Select(p => p.bestSEM).ToArray()
				let finalWithErrorBars = GraphUtils.Line(finalPoints)
				let bestWithErrorBars = GraphUtils.Line(bestPoints)
				let finalMedian = GraphUtils.Line(finalMedianPoints)
				let bestMedian = GraphUtils.Line(bestMedianPoints)
				let bounds = Rect.Union(finalWithErrorBars.Bounds, bestWithErrorBars.Bounds)
				let fGraphC = new GraphGeometryControl {
					Name = "MinOverStability_N" + graph.Key.N + "_eM" + graph.Key.eM + "_nD" + orderedGraph[0].runs,
					XLabel = "α = P/N for N = " + graph.Key.N,
					YLabel = "mean final stability",
					GraphGeometry = finalWithErrorBars,
					GraphBounds = bounds,
				}
				let bGraphC = new GraphGeometryControl {
					Name = "MinOverBestStability_N" + graph.Key.N + "_eM" + graph.Key.eM + "_nD" + orderedGraph[0].runs,
					XLabel = "α = P/N for N = " + graph.Key.N,
					YLabel = "mean best stability",
					GraphGeometry = bestWithErrorBars,
					GraphBounds = bounds,
				}
				let fMGraphC = new GraphGeometryControl {
					Name = "MinOverMedianStability_N" + graph.Key.N + "_eM" + graph.Key.eM + "_nD" + orderedGraph[0].runs,
					XLabel = "α = P/N for N = " + graph.Key.N,
					YLabel = "median final stability",
					GraphGeometry = finalMedian,
					GraphBounds = bounds,
					GraphPen = F.Create<Pen, Pen>(pen => { pen.DashStyle = DashStyles.Dot; return pen; })(fGraphC.GraphPen.Clone())
				}
				let bMGraphC = new GraphGeometryControl {
					Name = "MinOverMedianBestStability_N" + graph.Key.N + "_eM" + graph.Key.eM + "_nD" + orderedGraph[0].runs,
					XLabel = "α = P/N for N = " + graph.Key.N,
					YLabel = "median best stability",
					GraphGeometry = bestMedian,
					GraphBounds = bounds,
					GraphPen = F.Create<Pen, Pen>(pen => { pen.DashStyle = DashStyles.Dot; return pen; })(bGraphC.GraphPen.Clone())
				}
				from graphControl in new[] { fMGraphC, bMGraphC, fGraphC, bGraphC }
				select graphControl;

			foreach (var graphControl in q) {
				plotControl.Graphs.Add(graphControl);
				plotControl.ShowGraph(graphControl);
			}
		}

		[MakeButton]
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

		[MakeButton]
		void RealWorldConvergence() {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread(() => {
				const int maxEpoch = 1000;
				const int nD = 100;
				var data = DataSet.LoadSamples(DataSet.Ass2File);
				DataSet test, train;
				DataSet.SplitSamples(data, 0.2, out train, out test);//0.2 means with 20% as test.
				Console.WriteLine("Data Loaded");

				//sink for online (no storage) mean/variance calculations
				MeanVarCalc[] trainError = new MeanVarCalc[maxEpoch];
				MeanVarCalc[] testError = new MeanVarCalc[maxEpoch];
				//doing offline mean/var calcs would mean storing all error rates for all runs,
				//that's maxEpoch*nD*2*sizeof(double) ~ 1.6GB mem usage, just for the  mean/variance!

				object syncMutex = new object(); //mutex for accessing the MeanVarCalc shared variables.
				Parallel.For(0, nD, iterI => {
					DataSet D = train.ShuffledCopy();
					SimplePerceptron w = D.InitializeNewPerceptron(useCoM);
					var trainErrThisRun = new double[maxEpoch]; //this runs error rate cache.
					var testErrThisRun = new double[maxEpoch];
					int epochToConverge = w.DoTraining(D, maxEpoch, (epochN, errN) => {
						trainErrThisRun[epochN] = w.ErrorRate(D);
						testErrThisRun[epochN] = w.ErrorRate(test);
						return false;
					});
					lock (syncMutex) { //for reduced lock contention, send error rates all at once.
						for (int i = 0; i < maxEpoch; i++) {
							trainError[i].Add(trainErrThisRun[i]);
							testError[i].Add(testErrThisRun[i]);
						}
					}
				});

				Console.WriteLine("FinalTrain:{0}", trainError[maxEpoch - 1]);
				Console.WriteLine("FinalTest:{0}", testError[maxEpoch - 1]);

				//Graph construction function:
				Func<IEnumerable<double>, string, GraphControl> errors2Graph = (vec, name) => new GraphGeometryControl {
					GraphGeometry = GraphUtils.Line(vec.Select((e, i) => new Point(i, e)).ToArray()),
					Name = name,
					XLabel = "Epoch",
					YLabel = name + " Rate",
				};
				//Color fading function:
				Func<Brush, Brush> fadeColorBrush = (brush) => new SolidColorBrush(Color.Multiply(
						Color.Add(Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f), ((SolidColorBrush)brush).Color), 0.5f));

				//display code must run on the UI thread, hence the Dispatcher:
				Dispatcher.Invoke((Action)(() => {
					var trainG = errors2Graph(trainError.Select(err => err.Mean), "TrainingError");
					var trainLower = errors2Graph(trainError.Select(err => err.Mean - err.StdDev), "TrainingErrorLower");
					var trainUpper = errors2Graph(trainError.Select(err => err.Mean + err.StdDev), "TrainingErrorUpper");
					trainG.GraphLineColor = Brushes.DarkBlue;
					trainLower.GraphLineColor = fadeColorBrush(trainG.GraphLineColor);
					trainUpper.GraphLineColor = fadeColorBrush(trainG.GraphLineColor);

					var testG = errors2Graph(testError.Select(err => err.Mean), "TestError");
					var testLower = errors2Graph(testError.Select(err => err.Mean - err.StdDev), "TestErrorLower");
					var testUpper = errors2Graph(testError.Select(err => err.Mean + err.StdDev), "TestErrorUpper");
					testG.GraphLineColor = Brushes.DarkRed;
					testLower.GraphLineColor = fadeColorBrush(testG.GraphLineColor);
					testUpper.GraphLineColor = fadeColorBrush(testG.GraphLineColor);

					var graphs = new[] { trainLower, trainUpper, trainG, testLower, testUpper, testG, };
					var bounds = Rect.Empty;
					foreach (var graph in graphs)
						bounds.Union(graph.GraphBounds);
					bounds.Union(new Point(0, 0));
					bounds.Union(new Point(0, 0.281));

					foreach (var graph in graphs) {
						graph.GraphBounds = bounds;
						plotControl.Graphs.Add(graph);
						graph.Visibility = Visibility.Visible;
					}
					plotControl.ShowGraph(trainG);
					plotControl.ShowGraph(testG);
				}));

				Console.WriteLine("Done with RealWorldConvergence");
			}) { IsBackground = true }.Start();
		}

		[MakeButton]
		void VaryingTraingSamples() {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread(() => {
				const int maxEpoch = 1000;
				const int nD = 10000;
				const int topP = 400;
				const int botP = 0;
				const int stepP = 4;
				const int numP = (topP - botP) / stepP; //  P = topP - i * stepP
				var data = DataSet.LoadSamples(DataSet.Ass2File);
				DataSet test, train;
				DataSet.SplitSamples(data, 0.2, out train, out test);//0.2 means with 20% as test.
				Console.WriteLine("Data Loaded");

				//sink for online (no storage) mean/variance calculations
				MeanVarCalc[] trainError = new MeanVarCalc[numP];
				MeanVarCalc[] testError = new MeanVarCalc[numP];

				object syncMutex = new object(); //mutex for accessing the MeanVarCalc shared variables.
				Parallel.For(0, nD, iterI => {
					DataSet D = train.ShuffledCopy();
					double[] trainErr = new double[numP];
					double[] testErr = new double[numP];
					for (int i = 0; i < numP; i++) {
						int P = topP - i * stepP;
						DataSet subset = new DataSet(D.samples.Take(P).ToArray());
						SimplePerceptron w = subset.InitializeNewPerceptron(useCoM);
						int epochToConverge = w.DoTraining(subset, maxEpoch, (epochN, errN) => false);
						trainErr[i] = w.ErrorRate(subset);
						testErr[i] = w.ErrorRate(test);

					}
					lock (syncMutex) {
						for (int i = 0; i < numP; i++) {
							trainError[i].Add(trainErr[i]);
							testError[i].Add(testErr[i]);
						}
					}
				});


				//Graph construction function:
				Func<IEnumerable<double>, string, GraphControl> errors2Graph = (vec, name) => new GraphGeometryControl {
					GraphGeometry = GraphUtils.Line(vec.Select((e, i) => new Point(topP - i * stepP, e)).ToArray()),
					Name = name,
					XLabel = "P",
					YLabel = name + " Rate",
				};
				//Color fading function:
				Func<Brush, Brush> fadeColorBrush = (brush) => new SolidColorBrush(Color.Multiply(
						Color.Add(Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f), ((SolidColorBrush)brush).Color), 0.5f));

				//display code must run on the UI thread, hence the Dispatcher:
				Dispatcher.Invoke((Action)(() => {
					var trainG = errors2Graph(trainError.Select(err => err.Mean), "TrainingError");
					var trainLower = errors2Graph(trainError.Select(err => err.Mean - err.StdDev), "TrainingErrorLower");
					var trainUpper = errors2Graph(trainError.Select(err => err.Mean + err.StdDev), "TrainingErrorUpper");
					trainG.GraphLineColor = Brushes.DarkBlue;
					trainLower.GraphLineColor = fadeColorBrush(trainG.GraphLineColor);
					trainUpper.GraphLineColor = fadeColorBrush(trainG.GraphLineColor);

					var testG = errors2Graph(testError.Select(err => err.Mean), "TestError");
					var testLower = errors2Graph(testError.Select(err => err.Mean - err.StdDev), "TestErrorLower");
					var testUpper = errors2Graph(testError.Select(err => err.Mean + err.StdDev), "TestErrorUpper");
					testG.GraphLineColor = Brushes.DarkRed;
					testLower.GraphLineColor = fadeColorBrush(testG.GraphLineColor);
					testUpper.GraphLineColor = fadeColorBrush(testG.GraphLineColor);

					var graphs = new[] { trainLower, trainUpper, trainG, testLower, testUpper, testG, };
					var bounds = Rect.Empty;
					foreach (var graph in graphs)
						bounds.Union(graph.GraphBounds);
					bounds.Union(new Point(0, 0));
					//bounds.Union(new Point(0, 0.281));

					foreach (var graph in graphs) {
						graph.GraphBounds = bounds;
						plotControl.Graphs.Add(graph);
						graph.Visibility = Visibility.Visible;
					}
					plotControl.ShowGraph(trainG);
					plotControl.ShowGraph(testG);
				}));

				Console.WriteLine("Done with P variation");
			}) { IsBackground = true }.Start();
		}

		[MakeButton]
		void RealWorldGradientDescent() {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread(() => {
				NiceTimer.Time("DoGD", () => {
					const int maxEpoch = 10000;
					const int nD = 50;
					const double learnRate = 0.01;
					const double labelScale = 0.1;
					var data = DataSet.LoadSamples(DataSet.Ass2File)
						.Select(sample => new LabelledSample {
							Label = labelScale * sample.Label,
							Sample = sample.Sample
						})
						.ToArray();
					DataSet test, train;
					DataSet.SplitSamples(data, 0.2, out train, out test);//0.2 means with 20% as test.
					Console.WriteLine("Data Loaded");

					//sink for online (no storage) mean/variance calculations
					MeanVarCalc[] trainError = new MeanVarCalc[maxEpoch];
					MeanVarCalc[] testError = new MeanVarCalc[maxEpoch];
					MeanVarCalc[] trainCost = new MeanVarCalc[maxEpoch];
					MeanVarCalc[] testCost = new MeanVarCalc[maxEpoch];

					object syncMutex = new object(); //mutex for accessing the MeanVarCalc shared variables.
					Parallel.For(0, nD, iterI => {
						DataSet D = train.ShuffledCopy();
						SimplePerceptron w = D.InitializeNewPerceptron(useCoM);
						var trainErrThisRun = new double[maxEpoch]; //this runs error rate cache.
						var testErrThisRun = new double[maxEpoch];
						var trainCostThisRun = new double[maxEpoch]; //this runs error rate cache.
						var testCostThisRun = new double[maxEpoch];
						w.GradientDescent(D, learnRate, maxEpoch, RndHelper.ThreadLocalRandom, null);/*(epochN) => {
						trainErrThisRun[epochN] = w.ErrorRate(D);
						testErrThisRun[epochN] = w.ErrorRate(test);
						trainCostThisRun[epochN] = w.TotalCost(D)/D.P/labelScale/labelScale;
						testCostThisRun[epochN] = w.TotalCost(test) / test.P / labelScale / labelScale;
					});
					lock (syncMutex) { //for reduced lock contention, send error rates all at once.
						for (int i = 0; i < maxEpoch; i++) {
							trainError[i].Add(trainErrThisRun[i]);
							testError[i].Add(testErrThisRun[i]);
							trainCost[i].Add(trainCostThisRun[i]);
							testCost[i].Add(testCostThisRun[i]);
						}
					}*/
					});
				});
				//				Console.WriteLine("done.");
				//				Dispatcher.Invoke((Action)Close);
				/*
				Console.WriteLine("FinalTrain:{0}", trainError[maxEpoch - 1]);
				Console.WriteLine("FinalTest:{0}", testError[maxEpoch - 1]);
				Console.WriteLine("FinalTrainCost:{0}", trainCost[maxEpoch - 1]);
				Console.WriteLine("FinalTestCost:{0}", testCost[maxEpoch - 1]);

				//Graph construction function:
				Func<IEnumerable<double>, string, GraphControl> errors2Graph = (vec, name) => new GraphControl {
					LineGeometry = GraphControl.Line(vec.Select((e, i) => new Point(i+1, e)).ToArray()),
					Name = name,
					XLabel = "Epoch",
					YLabel = name + " Rate",
				};
				//Color fading function:
				Func<Brush, Brush> fadeColorBrush = (brush) => new SolidColorBrush(Color.Multiply(
						Color.Add(Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f), ((SolidColorBrush)brush).Color), 0.5f));

				Func<MeanVarCalc[], string, SolidColorBrush, IEnumerable<GraphControl>> makeGraphs = (graphdata,label,brush) => {
					GraphControl datG = errors2Graph(graphdata.Select(p => p.Mean), label),
						datGU = errors2Graph(graphdata.Select(p => p.Mean + p.StdDev), label + "Upper"),
						datGL = errors2Graph(graphdata.Select(p => p.Mean - p.StdDev), label + "Lower");
					datG.GraphLineColor = brush;
					datGU.GraphLineColor = datGL.GraphLineColor = fadeColorBrush(brush);
					return new[] { datG, datGL, datGU };
				};

				//display code must run on the UI thread, hence the Dispatcher:
				Dispatcher.Invoke((Action)(() => {
					var graphsA =
						makeGraphs(trainError, "TrainingError", Brushes.DarkBlue).Concat(
						makeGraphs(testError, "TestError", Brushes.DarkRed)).ToArray();
					var graphsB =
						makeGraphs(trainCost, "TrainingCost", Brushes.DarkCyan).Concat(
						makeGraphs(testCost, "TestCost", Brushes.DarkOrange)).ToArray();
					
					var bounds = Rect.Empty;
					foreach (var graph in graphsA)
						bounds.Union(graph.GraphBounds);
					bounds.Union(new Point(0, 0));

					foreach (var graph in graphsA) {
						graph.GraphBounds = bounds;
						plotControl.Graphs.Add(graph);
						graph.Visibility = Visibility.Hidden;
					}
					
					var boundsB = Rect.Empty;
					foreach (var graph in graphsB)
						boundsB.Union(graph.GraphBounds);
					boundsB.Union(new Point(0, 0));

					foreach (var graph in graphsB) {
						graph.GraphBounds = boundsB;
						plotControl.Graphs.Add(graph);
						graph.Visibility = Visibility.Hidden;
					}
					plotControl.ShowGraph(graphsA[0]);
					plotControl.ShowGraph(graphsA[3]);

				}));

				Console.WriteLine("Done with RealWorldConvergence");
				 */
			}) { IsBackground = true }.Start();
		}
		const int res = 51;
		static double idxToRate(int i) { return Math.Pow(10, i * (-4 / (double)(res - 1))); }
		static double idxToLog10Rate(int i) { return i * (-4 / (double)(res - 1)); }
		[MakeButton]
		void GradientDescentPicture() {
			bool useCoM = UseCenterOfMass.IsChecked == true;
			new Thread(() => {
				while (true) {
					const int maxEpoch = 10000;
					const int nD = 10;
					var data = DataSet.LoadSamples(DataSet.Ass2File);
					DataSet test, train;
					DataSet.SplitSamples(data, 0.2, out train, out test);//0.2 means with 20% as test.



					// 10 to the power: -4 -> 0 in 50 steps, learning rate and labelScale.
					//Func<int, double> idxToRate = i => Math.Pow(10, i * (-4 / (double)(res - 1)));

					var combos = (from learnRateIdx in Enumerable.Range(0, res)
								  from labelScaleIdx in Enumerable.Range(0, res)
								  select new { LRidx = learnRateIdx, LSidx = labelScaleIdx }).ToArray();
					object syncroot = new object();//guards the next counter...
					int nextComboCounter = 0;
					const int maxThreads = 4;
					Semaphore stopSem = new Semaphore(0, maxThreads + 1);

					MeanVarCalc[,] trainError = new MeanVarCalc[res, res];
					MeanVarCalc[,] testError = new MeanVarCalc[res, res];
					MeanVarCalc[,] trainCost = new MeanVarCalc[res, res];
					MeanVarCalc[,] testCost = new MeanVarCalc[res, res];


					ThreadStart workerAction = () => {
						while (true) {
							int nextWork = 0;
							lock (syncroot) {
								nextWork = nextComboCounter;
								nextComboCounter++;
							}
							if (nextWork >= combos.Length) break;
							int LRidx = combos[nextWork].LRidx;
							int LSidx = combos[nextWork].LSidx;
							double learnRate = idxToRate(LRidx);
							double labelScale = idxToRate(LSidx);
							var testW = test.WithScaledLabels(labelScale);
							var trainW = train.WithScaledLabels(labelScale);
							MeanVarCalc trainErrorW = new MeanVarCalc();
							MeanVarCalc testErrorW = new MeanVarCalc();
							MeanVarCalc trainCostW = new MeanVarCalc();
							MeanVarCalc testCostW = new MeanVarCalc();

							for (int i = 0; i < nD; i++) {
								SimplePerceptron w = trainW.InitializeNewPerceptron(useCoM);
								w.GradientDescent(trainW, learnRate, maxEpoch, RndHelper.ThreadLocalRandom, null);
								trainErrorW.Add(w.ErrorRate(trainW));
								testErrorW.Add(w.ErrorRate(testW));
								trainCostW.Add(w.TotalCost(trainW) / trainW.P / labelScale / labelScale);
								testCostW.Add(w.TotalCost(testW) / testW.P / labelScale / labelScale);
							}

							trainError[LRidx, LSidx] = trainErrorW;
							testError[LRidx, LSidx] = testErrorW;
							trainCost[LRidx, LSidx] = trainCostW;
							testCost[LRidx, LSidx] = testCostW;
							/*Console.WriteLine("[" + LRidx + "," + LSidx + "]: " +
									"TRE<" + trainError[LRidx, LSidx] + "> " +
									"TEE<" + testError[LRidx, LSidx] + "> " +
									"TRC<" + trainCost[LRidx, LSidx] + "> " +
									"TEC<" + testCost[LRidx, LSidx] + ">");*/
						}

						stopSem.Release();
					};

					for (int i = 0; i < maxThreads; i++)
						new Thread(workerAction) { IsBackground = true }.Start();

					for (int i = 0; i < maxThreads; i++)
						stopSem.WaitOne();
					string saveLogName = "errorsSave.log";
					using (var stream = File.Open(saveLogName, FileMode.Append, FileAccess.Write))
					using (var writer = new StreamWriter(stream)) {
						for (int LRidx = 0; LRidx < res; LRidx++)
							for (int LSidx = 0; LSidx < res; LSidx++)
								writer.WriteLine("[" + LRidx + "," + LSidx + "]: " +
									"TRE<" + trainError[LRidx, LSidx] + "> " +
									"TEE<" + testError[LRidx, LSidx] + "> " +
									"TRC<" + trainCost[LRidx, LSidx] + "> " +
									"TEC<" + testCost[LRidx, LSidx] + ">");

					}
					Console.WriteLine("Did {0} more tests", nD);
				}
			}) { IsBackground = true }.Start();
		}

		public Regex errorsaveRegex = new Regex(
			@"^\[(?<lr>\d+),(?<ls>\d+)\]: TRE<(?<tre>[^ ]+) \+/- (?<treE>[^>]+)> TEE<(?<tee>[^ ]+) \+/- (?<teeE>[^>]+)> TRC<(?<trc>[^ ]+) \+/- (?<trcE>[^>]+)> TEC<(?<tec>[^ ]+) \+/- (?<tecE>[^>]+)>$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public static M[,] Map2D<T, M>(T[,] input, Func<int, int, T, M> mapFunc) {
			M[,] retval = new M[input.GetLength(0), input.GetLength(1)];
			for (int i = 0; i < input.GetLength(0); i++)
				for (int j = 0; j < input.GetLength(1); j++)
					retval[i, j] = mapFunc(i, j, input[i, j]);
			return retval;
		}

		public static IEnumerable<T> AsEnumerable<T>(T[,] input) {
			foreach (T item in input) yield return item;
		}

		[MakeButton]
		void LoadGradientDescentPicture() {


			new Thread(() => {
				const int res = 51;
				MeanVarCalc[,] trainError = new MeanVarCalc[res, res];
				MeanVarCalc[,] testError = new MeanVarCalc[res, res];
				MeanVarCalc[,] trainCost = new MeanVarCalc[res, res];
				MeanVarCalc[,] testCost = new MeanVarCalc[res, res];


				string saveLogName = "errorsSave.log";
				Func<Match, string, int> geti = (match, s) => int.Parse(match.Groups[s].Value);
				Func<Match, string, double> getd = (match, s) => double.Parse(match.Groups[s].Value);
				Action<Match, string, MeanVarCalc[,]> proc = (match, s, sink) => {
					var mean = getd(match, s);
					var stddev = getd(match, s + "E");
					int count = 10;
					sink[geti(match, "lr"), geti(match, "ls")].Add(count, mean * count, stddev * stddev * (count - 1) + mean * mean * count);
				};
				foreach (string line in new FileInfo(saveLogName).GetLines()) {
					if (line.Trim() == "") continue;
					var match = errorsaveRegex.Match(line);
					proc(match, "tre", trainError);
					proc(match, "trc", trainCost);
					proc(match, "tee", testError);
					proc(match, "tec", testCost);
				}

				var errs = AsEnumerable(trainError).Concat(AsEnumerable(testError))
					.Select(mv => mv.Mean)
					.OrderBy(mean => mean)
					.ToArray();
				var costs = AsEnumerable(trainCost).Concat(AsEnumerable(testCost))
					.Select(mv => mv.Mean)
					.OrderBy(mean => mean)
					.ToArray();
				double errMin = errs.First(),
					errMax = errs.Skip((int)(errs.Length * 0.90)).First(),
					costMin = costs.First(),
					costMax = costs.Skip((int)(costs.Length * 0.90)).First();


				Func<MeanVarCalc[,], double, double, Drawing> makeBmp = (mvs, min, max) => {
					return GraphUtils.MakeBitmapDrawing(GraphUtils.MakeGreyBitmap(Map2D(mvs, (i, j, mv) => (byte)(256 * Math.Min((mv.Mean - min) / (max - min), 0.99999)))),
						idxToLog10Rate(0), idxToLog10Rate(res - 1), idxToLog10Rate(0), idxToLog10Rate(res - 1));
				};
				Dispatcher.Invoke((Action)(() => {
					plotControl.Graphs.Add(new GraphDrawingControl {
						GraphDrawing = makeBmp(trainError, errMin, errMax),
						YLabel = "log10(learning rate)",
						XLabel = "log10(label scale)",
						Name = "TrainError",
					});
					plotControl.Graphs.Add(new GraphDrawingControl {
						GraphDrawing = makeBmp(testError, errMin, errMax),
						YLabel = "log10(learning rate)",
						XLabel = "log10(label scale)",
						Name = "TestError",
					});
					plotControl.Graphs.Add(new GraphDrawingControl {
						GraphDrawing = makeBmp(trainCost, costMin, costMax),
						YLabel = "log10(learning rate)",
						XLabel = "log10(label scale)",
						Name = "TrainCost",
					});
					plotControl.Graphs.Add(new GraphDrawingControl {
						GraphDrawing = makeBmp(testCost, costMin, costMax),
						YLabel = "log10(learning rate)",
						XLabel = "log10(label scale)",
						Name = "TestCost",
					});
				}));
			}) { IsBackground = true }.Start();
		}

	}
}

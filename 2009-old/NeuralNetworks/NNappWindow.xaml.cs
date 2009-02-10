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

namespace NeuralNetworks
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NNappWindow : Window
    {

        const int points = 1000;
        static void calcMV(Random r,double[] mean,  double[] vars) {
            double xSum = 0.0, x2Sum = 0.0;
            for (int i = 0; i < mean.Length; i++) {
                double newVal = r.NextNorm();
                xSum += newVal;
                x2Sum += newVal * newVal;
                mean[i] = xSum / (i + 1);
                vars[i] = i==0?0: (x2Sum / (i + 1) - mean[i] * mean[i])*(i+1)/i ;
            }
        }
        const int iters = 1000;
        const int parLev = 8;
        public NNappWindow() {
            InitializeComponent();
        }
        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
			new Thread(MakeMinOverPlots) {
                IsBackground = true,
            }
            .Start();
        }

        
        [ThreadStatic] static MersenneTwister randomImpl;
        static Random Random {get{if (randomImpl==null) randomImpl = new MersenneTwister(); return randomImpl;}}
		void MakeMinOverPlots() {
			MakeMinOverPlot(20);
			MakeMinOverPlot(50);
			MakeMinOverPlot(80);
			MakeMinOverPlot(120);
			MakeMinOverPlot(160);
			MakeMinOverPlot(200);
			//Dispatcher.Invoke((Action)this.Close);
		}

        void MakeSuccessPlots() {
			MakeSuccessPlot(20);
			MakeSuccessPlot(50);
			MakeSuccessPlot(80);
			MakeSuccessPlot(120);
            MakeSuccessPlot(160);
            MakeSuccessPlot(200);
        }

        void MakeSuccessPlot(int N) {
            int epochMax = 10000;
            int nD = 3000;
            
            var plotLine = F.Create(()=> (
                from P in Enumerable.Range(8,23).Select(p=>p*N /10).Where(p=>p-2*N<40 ).Reverse().AsParallel(4)
                let ratio=DataSet.FractionManageable(N, P, nD, epochMax, Random)
                let alpha = P / (double) N
                orderby alpha ascending
                select new Point(alpha,ratio)
                ).ToArray()
                ).Time(timespan=>{
                    Console.WriteLine("Computation took {0}.", timespan);
                });

            Dispatcher.Invoke((Action)(()=>{
                var g = plotControl.NewGraph("PerceptronStorage", plotLine);
                g.GraphBounds = new Rect(new Point(0.5, 1.01), new Point(3.0, 0.0));
                plotControl.ShowGraph(g);
                g.XLabel = "α = P/N for N = "+N;
                g.YLabel = "successful storage ratio";
                string fileName = "PerceptronStorage_N" + N + "_eM" + epochMax + "_nD" + nD + ".xps";
                using (var writestream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                    plotControl.Print(g, writestream);
            }));
        }


		void MakeMinOverPlot(int N) {
			int epochMax = 3000;
			int nD = 500;

			var plotLine = F.Create(() => (
				from P in Enumerable.Range(8, 23).Select(p => p * N / 10).AsParallel(4)
				let stability = DataSet.AverageStability(N, P, nD, epochMax, Random)
				let alpha = P / (double)N
				orderby alpha ascending
				select new { Point = new Point(alpha, stability.val), Err = stability.err }
				).ToArray()
				).Time(timespan => {
					Console.WriteLine("Computation took {0}.", timespan);
				});

			Dispatcher.Invoke((Action)(() => {
				var g=new GraphControl();
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
    }
}

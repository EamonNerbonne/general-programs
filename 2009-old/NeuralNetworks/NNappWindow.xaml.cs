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
        const int iters = 10000;
        const int parLev = 8;
        public NNappWindow() {
            InitializeComponent();
        }
        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            Random r1 = new MersenneTwister();
            var meanSumVec = new double[points];
            var varSumVec = new double[points];

            Parallel.Invoke(Enumerable.Repeat<Action>(() => {
                Random r;
                lock (meanSumVec) {
                    r = new Random(r1.Next());
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
            var meanPlot = plotControl.NewGraph("mean", meanSumVec.Select((meanV, i) => new Point(i, meanV)));
            var meanDev = 2 * (Math.Abs(meanSumVec[points / 4] - 0) + Math.Abs(meanSumVec[points / 2] - 0) + Math.Abs(meanSumVec[points / 3] - 0));
            var meanBounds = meanPlot.GraphBounds;
            meanPlot.GraphBounds = new Rect(meanBounds.X, 0 - meanDev, meanBounds.Width, 2 * meanDev);

            var varPlot = plotControl.NewGraph("var", varSumVec.Select((varV, i) => new Point(i, varV)));
            var varDev = 2 * (Math.Abs(varSumVec[points / 4] - 1) + Math.Abs(varSumVec[points / 2] - 1) + Math.Abs(varSumVec[points / 3] - 1));
            var varBounds = varPlot.GraphBounds;
            varPlot.GraphBounds = new Rect(varBounds.X, 1 - varDev, varBounds.Width, 2 * varDev);

            plotControl.ShowGraph(meanPlot);
            plotControl.ShowGraph(varPlot);

        }
    }
}

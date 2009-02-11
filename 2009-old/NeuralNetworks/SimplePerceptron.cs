using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.Collections;
namespace NeuralNetworks
{
	public class SimplePerceptron
	{
		public SimplePerceptron(int N) { w = new Vector(N); }
		public SimplePerceptron(Vector w) { this.w = w; }
		private int N { get { return w.N; } }

		public Vector w;
		public double LearningStep(Func<double, double> algorithm, LabelledSample example) {
			var directedSample = example.Sample * example.Label;
			var dotprod = w & directedSample;
			w += (1.0 / N * algorithm(dotprod)) * directedSample;
			return dotprod;
		}
		public static double Rosenblatt0(double E_mu_t) { return E_mu_t <= 0 ? 1 : 0; }

		public double FastRStep(LabelledSample example) {//avoid memory allocation; otherwise like LearningStep(Rosenblatt0,...)
			var sample = example.Sample.elems;
			var wA = w.elems;
			double dotprod = 0;
			for (int i = 0; i < wA.Length; i++)
				dotprod += sample[i] * example.Label * wA[i];
			if (dotprod <= 0) {
				var scaleFac = example.Label / (double)N;
				for (int i = 0; i < wA.Length; i++)
					wA[i] += scaleFac * sample[i];
			}
			return dotprod;
		}

		public int DoTraining(DataSet D, int maxEpochs, Action<int,double> EpochErrSink) {
			//returns 0 if no storage possible; otherwise number of epochs+1 - useful for tweaking params
			int unchangedCount = 0;// number of consecutively "correct" classifications, updated online.
			double epochDot = 0;
			for (int n = 0; n < maxEpochs; n++) {
				int epochErr = 0;
				double dotSum = 0.0;
				for (int i = 0; i < D.P; i++) {
					var dotprod=FastRStep(D.samples[i]);
					dotSum += dotprod;
					if (dotprod > 0) {//if (LearningStep(Rosenblatt0, D.samples[i]) > 0)
						unchangedCount++; //sample is OK and w not updated
					} else {
						unchangedCount = 0; //needed learning, reset.
						epochErr++;
					}
					if (unchangedCount >= D.P) { //all samples work!
						epochDot += (dotSum / D.P - epochDot) * (1.0 / 1024.0);
						if (EpochErrSink != null) EpochErrSink(n, epochDot);
						return n + 1;
					}
				}
				epochDot += (dotSum/D.P - epochDot) * (1.0 / 512.0);
				if(EpochErrSink!=null) EpochErrSink(n,epochDot);
			}
			return 0;
		}

		public double DoMinOver(DataSet D, int maxEpochs) {

			var dataPointOverlap = new double[D.P, D.P];//TriangularMatrix<double>();
			//dataPointOverlap.ElementCount = D.P;
			//dataPointOverlap.TrimCapacityToFit();


			for (int j = 0; j < D.P; j++)
				for (int i = 0; i < D.P; i++)
					dataPointOverlap[i, j] = (D.samples[i].Sample & D.samples[j].Sample) * (D.samples[i].Label * D.samples[j].Label) / N;

			double[] currentOverlap = new double[D.P];//unscaled!
			int resync = maxEpochs * D.P / 5;//resync to avoid numerical inaccuracies 

			int minI = -1;
			double min = double.MaxValue;

			for (int j = 0; j < D.P; j++) {
				currentOverlap[j] = (D.samples[j].Sample & w) * D.samples[j].Label;
				if (currentOverlap[j] < min) {
					minI = j;
					min = currentOverlap[minI];
				}
			}
			for (int n = 0; n < maxEpochs * D.P; n++) {


				var wA = w.elems;
				var sample = D.samples[minI].Sample.elems;
				var scaleFac = D.samples[minI].Label / (double)N;
				for (int i = 0; i < wA.Length; i++)
					wA[i] += scaleFac * sample[i];

				int curMinI = minI;
				minI = -1;
				min = double.MaxValue;
				if ((n + 1) % resync == 0) {
					for (int j = 0; j < D.P; j++) {
						currentOverlap[j] = (D.samples[j].Sample & w) * D.samples[j].Label;
						if (currentOverlap[j] < min) {
							minI = j;
							min = currentOverlap[minI];
						}
					}
				} else {
					for (int j = 0; j < D.P; j++) {
						currentOverlap[j] += dataPointOverlap[curMinI, j];
						if (currentOverlap[j] < min) {
							minI = j;
							min = currentOverlap[minI];
						}
					}
				}
			}

			return min / Math.Sqrt(w & w);
		}

	}
}

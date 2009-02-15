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
		const double c = 0.000001;//seems to help stopping heuristic.
		public static double Rosenblatt0(double E_mu_t) { return E_mu_t <= c ? 1 : 0; }
		public double FastRStep(LabelledSample example) {//avoid memory allocation; otherwise like LearningStep(Rosenblatt0,...)
			var sample = example.Sample.elems;
			var wA = w.elems;
			double dotprod = 0;
			for (int i = 0; i < wA.Length; i++)
				dotprod += sample[i] * wA[i];
			dotprod *= example.Label;
			if (dotprod <= c) {
				var scaleFac = example.Label / (double)N;
				for (int i = 0; i < wA.Length; i++)
					wA[i] += scaleFac * sample[i];
			}
			return dotprod;
		}

		public int DoTraining(DataSet D, int maxEpochs, Func<int, double, bool> StoppingHeuristic) {
			//returns number of epochs needed for convergence or negative number of epochs for heuristic stopping
			int unchangedCount = 0;// number of consecutively "correct" classifications, continually updated.
			double smoothedPotential = 0;
			double smoothingFactor = 1.0 / (10.0 + 2560.0 / D.N);
			for (int n = 0; n < maxEpochs; n++) {
				double dotSum = 0.0;
				for (int i = 0; i < D.P; i++) {
					//var dotprod = LearningStep(Rosenblatt0, D.samples[i]);
					var dotprod = FastRStep(D.samples[i]);
					dotSum += dotprod;
					unchangedCount = dotprod > c ? unchangedCount + 1 : 0; //was w changed?
					if (unchangedCount >= D.P) { //all samples work!
						smoothedPotential += (dotSum / D.P - smoothedPotential) * smoothingFactor;
						if (StoppingHeuristic != null) StoppingHeuristic(n, smoothedPotential);
						return n + 1;
					}
				}
				smoothedPotential += (dotSum / D.P - smoothedPotential) * smoothingFactor;
				if (StoppingHeuristic != null)
					if (StoppingHeuristic(n, smoothedPotential))
						return -(n + 1);
			}
			return -maxEpochs;
		}

		public struct MinOverRes { public double Stability, BestStability;}
		public MinOverRes DoMinOver(DataSet D, int maxEpochs) {
			var dataPointOverlap = new double[D.P, D.P];

			for (int j = 0; j < D.P; j++)
				for (int i = 0; i < D.P; i++)
					dataPointOverlap[i, j] = (D.samples[i].Sample & D.samples[j].Sample) 
						* (D.samples[i].Label * D.samples[j].Label) / N;


			int minI = -1;
			double min = double.MaxValue;

			double[] localPotential = new double[D.P];//unscaled!
			for (int j = 0; j < D.P; j++) { //initialize localPotential
				localPotential[j] = (D.samples[j].Sample & w) * D.samples[j].Label;
				if (localPotential[j] < min) {
					minI = j;
					min = localPotential[minI];
				}
			}
			int resync = maxEpochs * D.P / 3;//resync to avoid numerical inaccuracies 

			double wSqr = w & w;
			double smoothFactor = 1 / 10000.0;
			double smoothedStab = min/Math.Sqrt(wSqr);
			double bestStab = min / Math.Sqrt(wSqr);
			//double bestSqr = wSqr;
		//	int lastUpdate =0;
		//	int lastUpdateNG = 0;
		//	int notGood=0;

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
					for (int j = 0; j < localPotential.Length; j++) {
						localPotential[j] = (D.samples[j].Sample & w) * D.samples[j].Label;
						if (localPotential[j] < min) {
							minI = j;
							min = localPotential[minI];
						}
					}
					wSqr = w & w;
				} else {
					wSqr += (2.0 * localPotential[curMinI] + dataPointOverlap[curMinI, curMinI]) / N;
					for (int j = 0; j < localPotential.Length; j++) {
						localPotential[j] += dataPointOverlap[curMinI, j];
						if (localPotential[j] < min) {
							minI = j;
							min = localPotential[minI];
						}
					}
				}
				double oldSt = smoothedStab;
				double currentStab = min/Math.Sqrt(wSqr);
				//smoothedStab += smoothFactor * (min/Math.Sqrt(wSqr) - smoothedStab);
				//if (smoothedStab < oldSt) //not good, going the wrong way!
				//	notGood++;

				if (currentStab > bestStab) {
//					lastUpdate = n;
//					lastUpdateNG = notGood;
					bestStab = currentStab;
					//bestSqr = wSqr;
				}

			}
			return new MinOverRes {
				BestStability = bestStab,
				Stability = min / Math.Sqrt(w & w)
			};
		}

	}
}

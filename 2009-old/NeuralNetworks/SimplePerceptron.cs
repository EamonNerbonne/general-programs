using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.Collections;
using EmnExtensions;
namespace NeuralNetworks
{
	public class SimplePerceptron
	{
		public SimplePerceptron(int N) { w = F.AsEnumerable(() => RndHelper.ThreadLocalRandom.NextNorm() / 10000.0).Take(N).ToArray(); }// w = new Vector(N);
		public SimplePerceptron(Vector w) { this.w = w; }
		private int N { get { return w.N; } }

		public Vector w;
		public double LearningStep(Func<double, double> algorithm, LabelledSample example) {
			var directedSample = example.Sample * example.Label;
			var dotprod = w & directedSample;
			w += (1.0 / N * algorithm(dotprod)) * directedSample;
			return dotprod;
		}
		const double c = 10;//0.000001;//seems to help stopping heuristic.
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

		public static Func<int, double, bool> DefaultStoppingHeuristic {
			get {
				double lastErrN = double.MinValue;
				int dipCnt = 0;
				return (epochN, errN) => {
					if (errN < lastErrN)
						dipCnt++;
					lastErrN = errN;

					return epochN > 10 && (dipCnt / (double)epochN) > 0.41;//... then stop
				};
			}
		}

		public struct MinOverRes { public double Stability, BestStability;}
		public MinOverRes DoMinOver(DataSet D, int maxEpochs) {

			var dataPointOverlap = new double[D.P, D.P];
			for (int j = 0; j < D.P; j++)
				for (int i = 0; i < D.P; i++) //initialize lookup table $L$
					dataPointOverlap[i, j] = (D.samples[i].Sample & D.samples[j].Sample)
						* (D.samples[i].Label * D.samples[j].Label) / N;


			int minI = -1;               //the index of the currently minimally stable example
			double minE = double.MaxValue;  // the currently minimal local potential.

			double[] localPotential = new double[D.P]; //unscaled!
			for (int j = 0; j < D.P; j++) { //initialize localPotential...
				localPotential[j] = (D.samples[j].Sample & w) * D.samples[j].Label; //$E^\nu = w \cdot \xi^\nu S^\nu$
				if (localPotential[j] < minE) {
					minI = j;
					minE = localPotential[minI];
				}
			}
			int resync = maxEpochs * D.P / 3;//resync to avoid numerical inaccuracies 

			double wSqr = w & w;                   //square of weight vector.
			double bestStability = minE / Math.Sqrt(wSqr);  //maximal stability found so far

			for (int n = 0; n < maxEpochs * D.P; n++) {
				var wElems = w.elems;                                //$w(t)$
				var minExampleElems = D.samples[minI].Sample.elems;  //$\xi^{\mu(t)}$
				var scaleFac = D.samples[minI].Label / (double)N;    //$\frac{1}{N} S^{\mu(t)}$
				for (int i = 0; i < wElems.Length; i++)              //$w(t+1) = w(t) + \frac{1}{N} S^{\mu(t)} \xi^{\mu(t)} $
					wElems[i] += scaleFac * minExampleElems[i];

				int curMinI = minI;
				minI = -1;
				minE = double.MaxValue;
				if ((n + 1) % resync == 0) { //recompute wSqr and the local potentials to avoid numerical inaccuracy
					for (int j = 0; j < localPotential.Length; j++) {
						localPotential[j] = (D.samples[j].Sample & w) * D.samples[j].Label;
						if (localPotential[j] < minE) {
							minI = j;
							minE = localPotential[minI];
						}
					}
					wSqr = w & w;
				} else { //compute the new wSqr and new localPotentials incrementally, and find new minimum.
					//$w(t+1)^2 = w(t)^2 + \frac{2}{N}E^{\mu(t)} + \frac{1}{N}L(\mu(t),\mu(t))$
					wSqr += (2.0 * localPotential[curMinI] + dataPointOverlap[curMinI, curMinI]) / N;
					for (int j = 0; j < localPotential.Length; j++) {
						localPotential[j] += dataPointOverlap[curMinI, j];//$E^\nu(t+1) = E^\nu(t) +  L\left(\mu(t), \nu\right)$
						if (localPotential[j] < minE) {
							minI = j;
							minE = localPotential[minI];
						}
					}
				}

				double currentStability = minE / Math.Sqrt(wSqr);
				if (currentStability > bestStability)
					bestStability = currentStability;
			}

			return new MinOverRes {
				BestStability = bestStability,
				Stability = minE / Math.Sqrt(w & w)
			};
		}

		public void GradientDescent(DataSet D, double learnRate, int maxEpochs, MersenneTwister r, Action<int> postEpoch) {
			var wA = w.elems;
			double invLearnBase = 1 / learnRate;
			int t = 0;
			for (int epCnt = 0; epCnt < maxEpochs; epCnt++) {
				for (int i = 0; i < D.P; i++) {
					double curLearnRate = 1.0 / (t++ + invLearnBase);
					LabelledSample sample = D.samples[r.NextUInt32()%(uint)D.P]; //this is much faster than r.Next
					var sA = sample.Sample.elems;
					double sample_w = 0.0;
					for (int k = 0; k < wA.Length; k++)	sample_w += sA[k] * wA[k];

					double exp2_sample_w = Math.Exp(2 * sample_w);
					double exp2_sample_w1 = exp2_sample_w + 1;

					double sigmaMinTau = (exp2_sample_w - 1) / exp2_sample_w1 - sample.Label;
					double Dsigma = 4 * exp2_sample_w / (exp2_sample_w1 * exp2_sample_w1);

					double scaleFac = -curLearnRate * sigmaMinTau * Dsigma;

					for (int j = 0; j < wA.Length; j++)	wA[j] += scaleFac * sA[j];
				}
				if(postEpoch!=null) postEpoch(epCnt);
			}
		}


		/// <summary>
		/// Determines error rate of current perceptron with a given dataset.
		/// </summary>
		/// <param name="D">The DataSet to test.</param>
		public double ErrorRate(DataSet D) {
			int ErrCount = 0;
			foreach (var example in D.samples)
				if ((example.Sample & w) * example.Label <= 0)
					ErrCount++;
			return ErrCount / (double)D.P;
		}

		public double SigmoidActivation(Vector sample) {
			double exp2_sample_w = Math.Exp(2 * (sample & w));
			return (exp2_sample_w - 1) / (exp2_sample_w + 1);
		}

		public double TotalCost(DataSet D) {
			double costSum = 0.0;
			foreach (var example in D.samples) {
				double diff = SigmoidActivation(example.Sample) - example.Label;
				costSum += diff * diff;
			}
			return 0.5 * costSum;
		}
	}
}

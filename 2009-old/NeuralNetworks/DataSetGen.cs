using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions;

namespace NeuralNetworks
{
    public struct LabelledSample
    {
        public int Label;
        public Vector Sample;
    }
    public class DataSet
    {
        public LabelledSample[] samples;
        public readonly int N;
        public int P { get { return samples.Length; } }



        public DataSet(int N, int P, Random r) {
            this.N=N;
            samples = F
                .AsEnumerable(() => MakeRandomSample(N, r))
                .Take(P)
                .ToArray();
        }

        public static LabelledSample MakeRandomSample(int N, Random r) {
            return new LabelledSample {
                Label = r.Next(2) * 2 - 1,
                Sample = F.AsEnumerable(() => r.NextNorm()).Take(N).ToArray()
            };
        }

        public static double FractionManageable(int N, int P, int nD,int maxEpochs, Random r) {
            int managed = 0;
            int epSum = 0;
            for (int i = 0; i < nD; i++) {
                DataSet D = new DataSet(N, P, r);
                SimplePerceptron w = new SimplePerceptron(N);
                int numEpochsNeeded = w.DoTraining(D, maxEpochs);
                if (numEpochsNeeded > 0) {
                    managed++;
                    epSum += numEpochsNeeded;
                }
            }
            var ratio = managed / (double)nD;
            Console.WriteLine("{2}  ==[{0}: {1}]",P/(double)N,ratio, epSum/(double)managed);
            return ratio;
        }

		public struct ValErr { public double val, err;}
		public static ValErr AverageStability(int N, int P, int nD, int maxEpochs, Random r) {

			double stabilitySum=0.0;
			double stability2Sum=0.0;
			for (int i = 0; i < nD; i++) {
				DataSet D = new DataSet(N, P, r);
				SimplePerceptron w = new SimplePerceptron(N);
				var stability = w.DoMinOver(D, maxEpochs);
				stabilitySum += stability;
				stability2Sum += stability * stability;
			}
			double meanStability = stabilitySum / nD;
			var variance = (stability2Sum - meanStability * meanStability * nD) / (nD - 1);
			double stdErr = Math.Sqrt(variance / nD);
			Console.WriteLine("[{0}: {1} +/- {2}]", P / (double)N, meanStability, stdErr);
			return new ValErr {
				val = meanStability,
				err = stdErr
			};
		}
    }
}

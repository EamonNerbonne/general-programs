using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions;
using EmnExtensions.Filesystem;
using System.IO;
using System.Threading;

namespace NeuralNetworks
{
    public struct LabelledSample
    {
        public double Label;
        public Vector Sample;
    }
    public class DataSet
    {
        public LabelledSample[] samples;
        public readonly int N;
        public int P { get { return samples.Length; } }

		public Vector ComputCenterOfMass() {
			Vector result = new Vector(N);
			foreach (LabelledSample sample in samples) {
				for (int i = 0; i < sample.Sample.elems.Length; i++)
					result.elems[i] += sample.Label * sample.Sample.elems[i] / samples.Length;
			}
			return result;
		}

		public DataSet(int N, int P, Random r) {
            this.N=N;
            samples = F
                .AsEnumerable(() => MakeRandomSample(N, r))
                .Take(P)
                .ToArray();
        }
		public DataSet(LabelledSample[] samples) {
			this.samples = samples;
			this.N = samples[0].Sample.N;
			if (!samples.All(sample => sample.Sample.N == N))
				throw new ArgumentException("Inconsistent number of features");
		}

		public static void LoadDataSet(FileInfo srcFile, double testSize, out DataSet train, out DataSet test) {
			var samples =
				(from textline in srcFile.GetLines()
				 let fields = textline.Split(' ')
				 let label = double.Parse(fields[0])
				 let elems = fields.Skip(1).Select(s=>double.Parse(s)).ToArray()
				 select new LabelledSample {
					 Label = label,
					 Sample = elems
				 }
				).ToArray();
			int N = samples[0].Sample.N;
			if (!samples.All(sample => sample.Sample.N == N))
				throw new FileFormatException("various lines had different numbers of features");

			int testCnt = (int)(samples.Length * testSize + 0.5);
			int trainCnt = samples.Length - testCnt;
			train = new DataSet(samples.Take(trainCnt).ToArray());
			test = new DataSet(samples.Skip(trainCnt).ToArray());
		}

        public static LabelledSample MakeRandomSample(int N, Random r) {
            return new LabelledSample {
                Label = r.Next(2) * 2 - 1,
                Sample = F.AsEnumerable(() => r.NextNorm()).Take(N).ToArray()
            };
        }

        public static double FractionManageable(int N, int P, int nD,int maxEpochs,bool useCoM, Func<Random> r) {
            int managed = 0;
            int epSum = 0;
			int notManaged = 0;
			int epNSum = 0;
			object sync=new object();
			Parallel.For(0,nD,i=>
//            for (int i = 0; i < nD; i++) 
			{
                DataSet D = new DataSet(N, P, r());
                SimplePerceptron w = useCoM? new SimplePerceptron(D.ComputCenterOfMass()): new SimplePerceptron(N);

				double lastErrN = double.MinValue;
				int dipCnt = 0;
                int numEpochsNeeded = w.DoTraining(D, maxEpochs,(epochN, errN) => {
							if (errN < lastErrN)
								dipCnt++;
							lastErrN = errN;

							return epochN > 10 && (dipCnt / (double)epochN) > 0.41;//... then stop
						});
				lock(sync) {
				if (numEpochsNeeded > 0) {
					managed++;
					epSum += numEpochsNeeded;
				} else {
					notManaged++;
					epNSum -= numEpochsNeeded;
				}
				}
            });
            var ratio = managed / (double)nD;
            Console.WriteLine("{2}/{3}   [{0}: {1}]",P/(double)N,ratio, epSum/(double)managed, epNSum/(double)notManaged);
            return ratio;
        }

		public struct ValErr { public double val, err;}
		public static ValErr AverageStability(int N, int P, int nD, int maxEpochs,bool useCoM,Random r) {
			double stabilitySum=0.0;
			double stability2Sum=0.0;
			for (int i = 0; i < nD; i++) {
				DataSet D = new DataSet(N, P, r);
				SimplePerceptron w = useCoM ? new SimplePerceptron(D.ComputCenterOfMass()) : new SimplePerceptron(N);
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

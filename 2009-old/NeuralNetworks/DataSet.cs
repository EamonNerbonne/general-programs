using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions.Algorithms;
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
		//per agreement, all samples should not be changed after construction for thread safety.
		public readonly LabelledSample[] samples;
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

		public DataSet(TrainingSettings settings, Random r) {

			this.N = settings.N;
			samples = F
				.AsEnumerable(() => MakeRandomSample(N, r))
				.Take(settings.P)
				.ToArray();
		}
		public DataSet(LabelledSample[] samples) {
			this.samples = samples;
			this.N = samples[0].Sample.N;
			if (!samples.All(sample => sample.Sample.N == N))
				throw new ArgumentException("Inconsistent number of features");
		}

		public SimplePerceptron InitializeNewPerceptron(bool useCenterOfMass) {
			return useCenterOfMass ? new SimplePerceptron(ComputCenterOfMass()) : new SimplePerceptron(N);
		}

		public static FileInfo Ass2File {
			get {
				var assemblyDir = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory;
				return (from dir in assemblyDir.ParentDirs()
						from file in dir.TryGetFiles()
						where file.Name == "nndat.txt"
						select file).FirstOrDefault();
			}
		}

		public static LabelledSample[] LoadSamples(FileInfo srcFile) {
			var samples =
				(from textline in srcFile.GetLines()
				 let fields = textline.Split(' ')
				 let label = double.Parse(fields[0])
				 let elems = fields.Skip(1).Select(s => double.Parse(s)).ToArray()
				 select new LabelledSample {
					 Label = label,
					 Sample = elems
				 }
				).ToArray();
			int N = samples[0].Sample.N;
			if (!samples.All(sample => sample.Sample.N == N))
				throw new FileFormatException("various lines had different numbers of features");
			return samples;
		}

		public static void SplitSamples(LabelledSample[] samples, double testSize, out DataSet trainSet, out DataSet testSet) {
			int testCnt = (int)(samples.Length * testSize + 0.5);
			int trainCnt = samples.Length - testCnt;
			trainSet = new DataSet(samples.Take(trainCnt).ToArray());
			testSet = new DataSet(samples.Skip(trainCnt).ToArray());
		}


		public static LabelledSample MakeRandomSample(int N, Random r) {
			return new LabelledSample {
				Label = r.Next(2) * 2 - 1,
				Sample = F.AsEnumerable(() => r.NextNorm()).Take(N).ToArray()
			};
		}

		//rather that supply a random number generator, supply a random number generator generator
		// this avoid worrying about multithreading issues in RNG.
		public static double FractionManageable(TrainingSettings settings, Func<Random> r) {
			int managed = 0;
			int epSum = 0;
			int notManaged = 0;
			int epNSum = 0;
			object sync = new object();
			Enumerable.Range(0, settings.TrialRuns).AsParallel(4).Select(i =>
				//Parallel.For(0,nD,i=>
				//            for (int i = 0; i < nD; i++) 
			{
				DataSet D = new DataSet(settings, r());
				SimplePerceptron w = D.InitializeNewPerceptron(settings.UseCenterOfMass);

				int numEpochsNeeded = w.DoTraining(D, settings.MaxEpoch, SimplePerceptron.DefaultStoppingHeuristic);
				lock (sync) {
					if (numEpochsNeeded > 0) {
						managed++;
						epSum += numEpochsNeeded;
					} else {
						notManaged++;
						epNSum -= numEpochsNeeded;
					}
				}
				return true;
			}).AsUnordered().ToArray();
			var ratio = managed / (double)settings.TrialRuns;
			Console.WriteLine("{2}/{3}   [{0}: {1}]", settings.P / (double)settings.N, ratio, epSum / (double)managed, epNSum / (double)notManaged);
			return ratio;
		}

		public struct ValErr { public double val, err;}
		public static ValErr AverageStability(TrainingSettings settings, Func< Random> r) {
			List<NeuralNetworks.SimplePerceptron.MinOverRes> retval = new List<SimplePerceptron.MinOverRes>();
			double stabilitySum = 0.0;
			double stability2Sum = 0.0;
			for (int i = 0; i < settings.TrialRuns; i++) {
				DataSet D = new DataSet(settings, r());
				SimplePerceptron w = D.InitializeNewPerceptron(settings.UseCenterOfMass);
				var stability = w.DoMinOver(D, settings.MaxEpoch);
				retval.Add(stability);
				stabilitySum += stability.Stability;
				stability2Sum += stability.Stability * stability.Stability;
			}
			string saveLogName = "N_" + settings.N + "_P_" + settings.P + "_E_"+settings.MaxEpoch +".molog";
			using (var stream = File.Open(saveLogName,FileMode.Append,FileAccess.Write))
			using (var writer = new StreamWriter(stream))
				foreach (var s in retval)
					writer.WriteLine(s.Stability.ToString() + " & " + s.BestStability);

			double meanStability = stabilitySum / settings.TrialRuns;
			var variance = (stability2Sum - meanStability * meanStability * settings.TrialRuns) / (settings.TrialRuns - 1);
			double stdErr = Math.Sqrt(variance / settings.TrialRuns);
			Console.WriteLine("[{0}: {1} +/- {2}]", settings.P / (double)settings.N, meanStability, stdErr);
			return new ValErr {
				val = meanStability,
				err = stdErr
			};
		}

		public DataSet ShuffledCopy() {
			var samplesCopy = this.samples.ToArray();
			samplesCopy.Shuffle();
			return new DataSet(samplesCopy);
		}
	}
}

using System;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using LvqLibCli;

namespace LvqGui
{
	public struct LrAndError : IComparable<LrAndError>, IComparable {
		public LearningRates LR; public ErrorRates Errors;
		public int CompareTo(LrAndError other) { return Errors.CompareTo(other.Errors); }
		public override string ToString() { return LR + " @ " + Errors; }

		public int CompareTo(object obj) { return CompareTo((LrAndError)obj); }
	}
	public struct ErrorRates : IComparable<ErrorRates>, IComparable {
		public readonly double training, trainingStderr, test, testStderr, nn, nnStderr, cumLearningRate;
		public ErrorRates(double training, double trainingStderr, double test, double testStderr, double nn, double nnStderr, double cumLearningRate) {
			this.training = training;
			this.trainingStderr = trainingStderr;
			this.test = test;
			this.testStderr = testStderr;
			this.nn = nn;
			this.nnStderr = nnStderr;
			this.cumLearningRate = cumLearningRate;
		}
		public ErrorRates(LvqMultiModel.Statistic stats, int nnIdx) {
			training = stats.Value[LvqTrainingStatCli.TrainingErrorI];
			test = stats.Value[LvqTrainingStatCli.TestErrorI];
			nn = nnIdx == -1 ? double.NaN : stats.Value[nnIdx];
			trainingStderr = stats.StandardError[LvqTrainingStatCli.TrainingErrorI];
			testStderr = stats.StandardError[LvqTrainingStatCli.TestErrorI];
			nnStderr = nnIdx == -1 ? double.NaN : stats.StandardError[nnIdx];
			cumLearningRate = stats.Value[LvqTrainingStatCli.CumLearningRateI];
		}

		public double CanonicalError { get { return training * 0.9 + (nn.IsFinite() ? test * 0.05 + nn * 0.05 : test * 0.1); } }
		public override string ToString() {
			return Statistics.GetFormatted(training, trainingStderr, 1) + "; " +
				Statistics.GetFormatted(test, testStderr, 1) + "; " +
				Statistics.GetFormatted(nn, nnStderr, 1) + "; ";
		}

		public int CompareTo(ErrorRates other) { return CanonicalError.CompareTo(other.CanonicalError); }

		public int CompareTo(object obj) { return CompareTo((ErrorRates)obj); }
	}
	public struct LearningRates { public double Lr0, LrP, LrB; public override string ToString() { return Lr0 + " p" + LrP + " b" + LrB; } }

}
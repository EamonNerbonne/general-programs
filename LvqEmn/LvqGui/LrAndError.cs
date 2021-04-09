using System;
using System.Collections.Generic;
using System.Linq;
using EmnExtensions;
using EmnExtensions.MathHelpers;
using LvqLibCli;

namespace LvqGui
{
    public struct LrAndError : IComparable<LrAndError>, IComparable
    {
        public readonly LearningRates LR;
        public readonly ErrorRates Errors;
        public readonly double cumLearningRate;
        public int CompareTo(LrAndError other) => Errors.CompareTo(other.Errors);
        public override string ToString() => LR + " @ " + Errors;
        public string ToStorageString() => LR.Lr0.ToString("g4").PadRight(9) + "p" + LR.LrP.ToString("g4").PadRight(9) + "b" + LR.LrB.ToString("g4").PadRight(9) + ": "
                                + Errors + "[" + cumLearningRate + "]";
        public int CompareTo(object obj) => CompareTo((LrAndError)obj);

        public LrAndError(LvqModelSettingsCli settings, LvqMultiModel.Statistic stats, int nnIdx)
        {
            LR = new LearningRates(settings);
            Errors = new ErrorRates(stats, nnIdx);
            cumLearningRate = stats.Value[LvqTrainingStatCli.CumLearningRateI];
        }
        private LrAndError(LearningRates lr, ErrorRates errors, double cumulativeLr)
        {
            LR = lr;
            Errors = errors;
            cumLearningRate = cumulativeLr;
        }

        public static LrAndError ParseLine(string resultLine, double[] lr0range, double[] lrPrange, double[] lrBrange)
        {
            var resLrThenErr = resultLine.Split(':');
            var lrs = resLrThenErr[0].Split('p', 'b').Select(double.Parse).ToArray();

            var errsThenCumulLr0 = resLrThenErr[1].Split(';');

            var errs = errsThenCumulLr0.Take(3).Select(errStr => errStr.Split('~').Select(double.Parse).ToArray()).Select(errval => Tuple.Create(errval[0], errval.Skip(1).FirstOrDefault())).ToArray();
            return new LrAndError(
                 new LearningRates(ClosestMatch(lr0range, lrs[0]), ClosestMatch(lrPrange, lrs[1]), ClosestMatch(lrBrange, lrs[2])),
                 new ErrorRates(errs[0].Item1, errs[0].Item2, errs[1].Item1, errs[1].Item2, errs[2].Item1, errs[2].Item2),
                 double.Parse(errsThenCumulLr0[3].Trim(' ', '[', ']'))
            );
        }
        static double ClosestMatch(IEnumerable<double> haystack, double needle) => haystack.Aggregate(new { Err = double.PositiveInfinity, Val = needle },
                (best, option) => Math.Abs(option - needle) < best.Err ? new { Err = Math.Abs(option - needle), Val = option } : best).Val;
    }
    public struct ErrorRates : IComparable<ErrorRates>, IComparable
    {
        public readonly double training, trainingStderr, test, testStderr, nn, nnStderr;
        public ErrorRates(double training, double trainingStderr, double test, double testStderr, double nn, double nnStderr)
        {
            this.training = training;
            this.trainingStderr = trainingStderr;
            this.test = test;
            this.testStderr = testStderr;
            this.nn = nn;
            this.nnStderr = nnStderr;
        }
        public ErrorRates(LvqMultiModel.Statistic stats, int nnIdx)
        {
            training = stats.Value[LvqTrainingStatCli.TrainingErrorI];
            test = stats.Value[LvqTrainingStatCli.TestErrorI];
            nn = nnIdx == -1 ? double.NaN : stats.Value[nnIdx];
            trainingStderr = stats.StandardError[LvqTrainingStatCli.TrainingErrorI];
            testStderr = stats.StandardError[LvqTrainingStatCli.TestErrorI];
            nnStderr = nnIdx == -1 ? double.NaN : stats.StandardError[nnIdx];
        }

        public double CanonicalError => training * 0.9 + (nn.IsFinite() ? test * 0.05 + nn * 0.05 : test * 0.1);
        public override string ToString() => Statistics.GetFormatted(training, trainingStderr, 1) + "; " +
                Statistics.GetFormatted(test, testStderr, 1) + "; " +
                Statistics.GetFormatted(nn, nnStderr, 1) + "; ";

        public int CompareTo(ErrorRates other) => CanonicalError.CompareTo(other.CanonicalError);

        public int CompareTo(object obj) => CompareTo((ErrorRates)obj);
    }
    public struct LearningRates
    {
        public readonly double Lr0, LrP, LrB;
        public override string ToString() => Lr0 + " p" + LrP + " b" + LrB;
        public LearningRates(double lr0, double lrP, double lrB) { Lr0 = lr0; LrP = lrP; LrB = lrB; }
        public LearningRates(LvqModelSettingsCli settings)
        {
            Lr0 = settings.LR0;
            LrP = settings.LrScaleP;
            LrB = settings.LrScaleB;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{

    /// <summary>
    /// This isn't a good implementation, but heck, it's already implemented...
    /// </summary>
    public class Statistics
    {
        public static float CentralMoment(IEnumerable<float> list, float average, int moment) {
            return (float)list.Average(x => Math.Pow(x - average, moment));
        }
        public static float Covariance(Statistics A, Statistics B) {
            return (float)(A.Seq.Cast<double>().ZipWith(B.Seq.Cast<double>(), (a, b) => (a - A.Mean) * (b - B.Mean)).Average() / Math.Sqrt(A.Var) / Math.Sqrt(B.Var));
        }
        public float Mean, Var, Skew, Kurtosis;
        public int Count;
        public IEnumerable<float> Seq;


        public Statistics(IEnumerable<float> seq) {
            Mean = seq.Average();
            Count = seq.Count();
            Var = CentralMoment(seq, Mean, 2);
            Skew = CentralMoment(seq, Mean, 3);
            Kurtosis = CentralMoment(seq, Mean, 4);
            Seq = seq;
        }

        public override string ToString() {
            return string.Format("Mean = {0}, Var = {1}, Skew = {2}, Kurtosis = {3}, Count = {4}", Mean, Var, Skew, Kurtosis, Count);
        }
    }
}

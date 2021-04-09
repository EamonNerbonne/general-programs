using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EmnExtensions.MathHelpers
{

    /// <summary>
    /// This isn't a good implementation, but heck, it's already implemented...
    /// </summary>
    public class Statistics
    {
        public static string GetFormatted(double mean, double stderr, double extraprecision = 0, bool suppressStderr = false)
        {
            string numF, errF;
            if (stderr == 0 || double.IsNaN(stderr) || double.IsInfinity(stderr)) {
                numF = "g";
                errF = stderr == 0 ? "~ 0" : "";
            } else if (Math.Abs(mean) > 0 && Math.Abs(Math.Log10(Math.Abs(mean))) < 5) {
                //use fixed-point:
                var errOOM = Math.Max(0, (int)(1.5 - Math.Log10(stderr) + extraprecision));
                numF = "f" + errOOM;
                errF = " ~ {1:f" + errOOM + "}";
            } else {
                var digitEstimate = Math.Abs(mean) <= stderr
                                ? 1.0
                                : (Math.Log10(Math.Abs(mean) / stderr) + 1.5);
                var digits = (int)(digitEstimate + extraprecision);
                numF = "g" + digits;
                errF = " ~ {1:g2}";
            }
            return string.Format("{0:" + numF + "}" + (suppressStderr ? "" : errF), mean, stderr);
        }


        public static float CentralMoment(IEnumerable<float> list, float average, int moment)
        {
            return (float)list.Average(x => Math.Pow(x - average, moment));
        }
        public static float Covariance(Statistics A, Statistics B)
        {
            return (float)(A.Seq.Cast<double>().ZipWith(B.Seq.Cast<double>(), (a, b) => (a - A.Mean) * (b - B.Mean)).Average() / Math.Sqrt(A.Var) / Math.Sqrt(B.Var));
        }
        public float Mean, Var, Skew, Kurtosis;
        public int Count;
        public IEnumerable<float> Seq;


        public Statistics(IEnumerable<float> seq)
        {
            Mean = seq.Average();
            Count = seq.Count();
            Var = CentralMoment(seq, Mean, 2);
            Skew = CentralMoment(seq, Mean, 3);
            Kurtosis = CentralMoment(seq, Mean, 4);
            Seq = seq;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Mean = {0}, Var = {1}, Skew = {2}, Kurtosis = {3}, Count = {4}", Mean, Var, Skew, Kurtosis, Count);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using EmnExtensions.DebugTools;
using EmnExtensions.Collections;
using EmnExtensions.MathHelpers;
namespace ScratchProject
{

    static class Program
    {



        static double[] V = MeanCenter(new[] { 0.0, 1.0, 2.0, 6.0, 6.1, 9.0, 10.0 });
        static int N = V.Length;
        static double[] D = CompD(9.5);
        static double[] Du = CompDu();

        static double eigval;

        static SymmetricDistanceMatrixGen<double> dists = new SymmetricDistanceMatrixGen<double>();

        static double DistAvg() {
            return dists.Values.Select(d=>Sqr(d)).Sum()*2 / (N*N);
        }
        static double DistAvg(int a) {
            return Enumerable.Range(0, V.Length).Select(i => Sqr(dists.GetDist(a, i))).Sum() / V.Length;
        }
        static double DistAvg(int a, int b) {
            return Sqr(dists.GetDist(a, b));
        }
        static double BnF(int a, int b) {
            return 0.5 * (DistAvg(a) + DistAvg(b) - DistAvg(a, b) - DistAvg());
        }
        static Matrix Bn, BnA, Hn,Dn;
        static double[] bk() {
            return Enumerable.Range(0, V.Length)
                .Select(n => V.Select((e, i) => e * BnF(n, i)).Sum())
                .ToArray();
        }

        static Program() {
            dists.ElementCount = V.Length;

            for (int i = 0; i < V.Length; i++)
                for (int j = 0; j < i; j++) {
                    dists[i, j] = Dist(V[i], V[j]);
                }
            double eig_sum_temp_num = 0.0;
            double eig_sum_temp_denum = 0.0;
            for (int i = 0; i < V.Length; i++) {
                eig_sum_temp_denum += Sqr(V[i]);
                for (int j = 0; j < V.Length; j++) {
                    eig_sum_temp_num += V[i] * V[j] * Sqr(dists.GetDist(i, j));
                }
            }
            eigval = -0.5 * eig_sum_temp_num / eig_sum_temp_denum;

            Dn = new Matrix(N, N, (i, j) => Sqr(dists.GetDist(i, j)));
            Bn = new Matrix(N, N, BnF);
            Hn = new Matrix(N,N,(i,j)=> (i == j ? 1 : 0) - 1.0 / N);
            BnA = -0.5* Hn*Dn*Hn;
        }



        static double Dist(double a, double b) {
            return Math.Abs(a - b);
        }
        static double GenEnergy(double x) {
            return (1.0 / V.Length) *
                V.Select(v => (x - v) * (x - v)).Sum();
        }
        static double[] CompD(double from) {
            return V.Select(v => Math.Abs(from - v)).ToArray();
        }
        static double[] MeanCenter(double[] vec) {
            // return vec;
            var mean = vec.Sum() / vec.Length;
            return vec.Select(v => v - mean).ToArray();
        }
        static double[] CompDu() {
            return V.Select(v => CompD(v).Select(val => Sqr(val)).ToArray()) //Arr of sqr-dists to V
                .Aggregate((vec, vec2) => vec.Select((val, i) => vec2[i] + val).ToArray()) //sum of sqr-dists
                .Select((val) => val / V.Length)
                .ToArray();
        }

        static double SpecCost(double x) {
            double sum = 0;
            for (int i = 0; i < V.Length; i++) {
                double v = V[i];
                double d = D[i];
                sum += Sqr((x - v) / d);
            }
            return sum / D.Select(d => Sqr(1 / d)).Sum();
        }
        static double Est2(double[] distSqrFromV) {
            var netDiff = distSqrFromV.Select((v, i) => v - Du[i]).ToArray();
            return V.Select((v, i) => v * netDiff[i]).Sum() * (-0.5);
        }

        static double MinGuess() {
            return V.Select((v, i) => v / Sqr(D[i])).Sum() / V.Select((v, i) => 1 / Sqr(D[i])).Sum();
        }
        static double Sqr(double x) { return x * x; }

        static double[] nums = Enumerable.Range(-5, 25).Select(i => i / 5.0).ToArray();

        static void MinMax(Func<double, double> f, IEnumerable<double> range) {
            double minx = double.NaN, min = double.PositiveInfinity;
            double maxx = double.NaN, max = double.NegativeInfinity;
            foreach (var x in range) {
                var fx = f(x);
                if (fx < min) { min = fx; minx = x; }
                if (fx > max) { max = fx; maxx = x; }
                //   Console.WriteLine("f({0}) = {1}", x, fx);
            }
            Console.WriteLine("min: f({0}) = {1}", minx, min);
            Console.WriteLine("min: f({0}) = {1}", maxx, max);
        }

        static void Main(string[] args) {
            MinMax(x => SpecCost(x), Enumerable.Range(-10000, 20001).Select(i => i / 100.0));
            Console.WriteLine("minguess:" + MinGuess());
            Console.WriteLine("Speccost@9.5:" + SpecCost(9.5));
            Console.WriteLine("Est2:" + Est2(D.Select(d => Sqr(d)).ToArray()) / eigval);
            Console.WriteLine("Eigval:" + eigval);
            //            Console.WriteLine("Est2:" + Est2(D.Select(d => Sqr(d)).ToArray()) / -1460.5972039801);
            Console.WriteLine("\nBk"); bk().PrintAllDebug();
            Console.WriteLine("\nV"); V.PrintAllDebug();
            Console.WriteLine("\nV*eigval"); V.Select(v => v * eigval).PrintAllDebug();

        }



    }
}

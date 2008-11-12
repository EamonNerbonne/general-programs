using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using EmnExtensions.DebugTools;
using EmnExtensions.Collections;
namespace ScratchProject
{
    static class Overloads
    {

        public static IEnumerable<int> To(this int from, int to) {
            return Enumerable.Range(from, to - from);
        }
        public static int Rows(this double[,] mat) { return mat.GetLength(0); }
        public static int Cols(this double[,] mat) { return mat.GetLength(1); }

    }


    public class Matrix
    {
        public readonly double[,] store;
        Matrix(double[,] a) {
            store = a;
        }
        public Matrix(int rows, int cols) {
            store = new double[rows, cols];
        }
        public int Rows { get { return store.GetLength(0); } }
        public int Cols { get { return store.GetLength(1); } }

        public Matrix(int rows, int cols, Func<int, int, double> initWith) {
            store = new double[rows, cols];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    store[i, j] = initWith(i, j);
        }

        public static Matrix operator *(double a, Matrix B) { return new Matrix(B.Rows, B.Cols, (i, j) => a * B[i, j]); }
        public static Matrix operator *(Matrix B, double a) { return new Matrix(B.Rows, B.Cols, (i, j) => a * B[i, j]); }
        public static Matrix operator *(Matrix A, Matrix B) {
            if (A.Cols != B.Rows) throw new Exception("Matrix mismatch");
            return new Matrix(A.Rows, B.Cols, (i, j) => 0.To(A.Rows).Select(k => A[i, k] * B[k, j]).Sum());
        }
        public static Matrix operator +(Matrix A, Matrix B) {
            if (A.Rows != B.Rows || A.Cols != B.Cols) throw new Exception("Matrixes don't match");
            return new Matrix(A.Rows, A.Cols, (i, j) => A[i, j] + B[i, j]);
        }
        public static Matrix operator -(Matrix A, Matrix B) {
            if (A.Rows != B.Rows || A.Cols != B.Cols) throw new Exception("Matrixes don't match");
            return new Matrix(A.Rows, A.Cols, (i, j) => A[i, j] - B[i, j]);
        }
        public static Matrix operator -(Matrix A) { return -1.0 * A; }

        public double this[int row, int col] { get { return store[row, col]; } set { store[row, col] = value; } }
        public override string ToString() {       return      this.ToString("g3");        }
        public string ToString(string format) {
            string[,] rep = new string[Rows,Cols];
            int[] colwidth = new int[Cols];
            for (int i = 0; i < Rows; i++) {
                for (int j = 0; j < Cols; j++) {
                    rep[i, j] = this[i, j].ToString(format);
                    colwidth[i] = Math.Max(colwidth[i], rep[i, j].Length);
                }
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Rows; i++) {
                sb.Append('(');
                for (int j = 0; j < Cols; j++) {
                    sb.Append(rep[i, j]);
                    sb.Append(' ',colwidth[j] + (j==Cols-1?0:2) - rep[i, j].Length);
                }
                sb.AppendLine(")");
            }
            return sb.ToString();
        }
    }


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
            Console.WriteLine("Est2:" + Est2(D.Select(d => Sqr(d)).ToArray()) / 93.0942857142857);
            Console.WriteLine("Eigval:" + eigval);
            //            Console.WriteLine("Est2:" + Est2(D.Select(d => Sqr(d)).ToArray()) / -1460.5972039801);
            Console.WriteLine("\nBk"); bk().PrintAllDebug();
            Console.WriteLine("\nV"); V.PrintAllDebug();
            Console.WriteLine("\nV*eigval"); V.Select(v => v * eigval).PrintAllDebug();

        }



    }
}

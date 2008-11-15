using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
    public static class Overloads
    {

        public static IEnumerable<int> To(this int from, int to) {
            return Enumerable.Range(from, to - from);
        }

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
        public override string ToString() { return this.ToString("g3"); }
        public string ToString(string format) {
            string[,] rep = new string[Rows, Cols];
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
                    sb.Append(' ', colwidth[j] + (j == Cols - 1 ? 0 : 2) - rep[i, j].Length);
                }
                sb.AppendLine(")");
            }
            return sb.ToString();
        }
    }
}

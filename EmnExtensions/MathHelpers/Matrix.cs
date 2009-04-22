using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EmnExtensions.MathHelpers
{
	public static class Overloads
	{

		public static IEnumerable<int> To(this int from, int to) {
			return Enumerable.Range(from, to - from);
		}

	}

	public struct Matrix
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		public readonly double[,] store;
		Matrix(double[,] a) { store = a; }

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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "0#")]
		public static implicit operator Matrix(double[,] m) { return new Matrix(m); }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return")]
		public static implicit operator double[,](Matrix m) { return m.store; }

		public static Matrix Identity(int rowsAndCols) {
			Matrix retval = new Matrix(rowsAndCols, rowsAndCols);
			for (int i = 0; i < rowsAndCols; i++)
				retval[i, i] = 1;
			return retval;
		}


		public static Matrix operator *(double a, Matrix B) { return new Matrix(B.Rows, B.Cols, (i, j) => a * B[i, j]); }
		public static Matrix operator *(Matrix B, double a) { return new Matrix(B.Rows, B.Cols, (i, j) => a * B[i, j]); }
		public static Matrix operator *(Matrix A, Matrix B) {
			if (A.Cols != B.Rows) throw new MatrixMismatchException("Matrix mismatch: [" + A.Rows + "," + A.Cols + "] * [" + B.Rows + "," + B.Cols + "]");
			return new Matrix(A.Rows, B.Cols, (i, j) => 0.To(A.Rows).Select(k => A[i, k] * B[k, j]).Sum());
		}
		public static Vector operator *(Matrix A, Vector v) {
			if (A.Cols != v.N) throw new MatrixMismatchException("Matrix mismatch: [" + A.Rows + "," + A.Cols + "] * [" + v.N + "]");

			Vector retval = new Vector(A.Rows);
			for (int row = 0; row < A.Rows; row++) {
				double sum = 0.0;
				for (int col = 0; col < A.Cols; col++)
					sum += A[row, col] * v[col];
				retval[row] = sum;
			}
			return retval;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
		public static Matrix operator +(Matrix A, Matrix B) {
			if (A.Rows != B.Rows || A.Cols != B.Cols) throw new MatrixMismatchException("Matrix mismatch: [" + A.Rows + "," + A.Cols + "] + [" + B.Rows + "," + B.Cols + "]");
			return new Matrix(A.Rows, A.Cols, (i, j) => A[i, j] + B[i, j]);
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
		public static Matrix operator -(Matrix A, Matrix B) {
			if (A.Rows != B.Rows || A.Cols != B.Cols) throw new MatrixMismatchException("Matrix mismatch: [" + A.Rows + "," + A.Cols + "] + [" + B.Rows + "," + B.Cols + "]");
			return new Matrix(A.Rows, A.Cols, (i, j) => A[i, j] - B[i, j]);
		}
		public static Matrix operator -(Matrix A) { return -1.0 * A; }

		public double this[int row, int col] { get { return store[row, col]; } set { store[row, col] = value; } }
		public override string ToString() { return this.ToString("g3", CultureInfo.InvariantCulture); }
		public string ToString(string format, IFormatProvider formatProvider) {
			string[,] rep = new string[Rows, Cols];
			int[] colwidth = new int[Cols];
			for (int i = 0; i < Rows; i++) {
				for (int j = 0; j < Cols; j++) {
					rep[i, j] = this[i, j].ToString(format, formatProvider);
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

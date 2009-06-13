using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EmnExtensions.MathHelpers
{

	public struct QMatrixSquareFactory : IMatrixFactory<QMatrixSquare>
	{
		public QMatrixSquare NewMatrix(int rows, int cols) { return new QMatrixSquare(new double[rows, cols]); }
	}


	public struct QMatrixSquare : IMatrix<QMatrixSquare>
	{


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		internal double[,] store;
		internal QMatrixSquare(double[,] a) { store = a; }

		public int Rows { get { return store.GetLength(0); } }
		public int Cols { get { return store.GetLength(1); } }


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "0#")]
		public static implicit operator QMatrixSquare(double[,] m) { return new QMatrixSquare(m); }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return")]
		public static implicit operator double[,](QMatrixSquare m) { return m.store; }

		public static QMatrixSquare operator *(double a, QMatrixSquare B) { return Factory.NewMatrix(B.Rows, B.Cols).InitializeFrom((i, j) => a * B[i, j]); }
		public static QMatrixSquare operator *(QMatrixSquare B, double a) { return Factory.NewMatrix(B.Rows, B.Cols).InitializeFrom((i, j) => a * B[i, j]); }
		public static QMatrixSquare operator *(QMatrixSquare A, QMatrixSquare B) {
			if (A.Cols != B.Rows) throw new MatrixMismatchException("QMatrix mismatch: [" + A.Rows + "," + A.Cols + "] * [" + B.Rows + "," + B.Cols + "]");
			return Factory.NewMatrix(A.Rows, B.Cols).InitializeFrom((i, j) => 0.To(A.Rows).Select(k => A[i, k] * B[k, j]).Sum());
		}
		public static Vector operator *(QMatrixSquare A, Vector v) {
			if (A.Cols != v.N) throw new MatrixMismatchException("QMatrix mismatch: [" + A.Rows + "," + A.Cols + "] * [" + v.N + "]");

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
		public static QMatrixSquare operator +(QMatrixSquare A, QMatrixSquare B) {
			if (A.Rows != B.Rows || A.Cols != B.Cols) throw new MatrixMismatchException("QMatrix mismatch: [" + A.Rows + "," + A.Cols + "] + [" + B.Rows + "," + B.Cols + "]");
			return Factory.NewMatrix(A.Rows, A.Cols).InitializeFrom((i, j) => A[i, j] + B[i, j]);
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
		public static QMatrixSquare operator -(QMatrixSquare A, QMatrixSquare B) {
			if (A.Rows != B.Rows || A.Cols != B.Cols) throw new MatrixMismatchException("QMatrix mismatch: [" + A.Rows + "," + A.Cols + "] + [" + B.Rows + "," + B.Cols + "]");
			return Factory.NewMatrix(A.Rows, A.Cols).InitializeFrom((i, j) => A[i, j] - B[i, j]);
		}
		public static QMatrixSquare operator -(QMatrixSquare A) { return -1.0 * A; }

		public double this[int row, int col] { get { return store[row, col]; } set { store[row, col] = value; } }

		public QMatrixSquare Copy() { QMatrixSquare copy; copy.store = (double[,])store.Clone(); return copy; }
		public static readonly QMatrixSquareFactory Factory;
		public QMatrixSquareFactory GetFactory() { return Factory; }

		public QMatrixSquare NewMatrix(int rows, int cols) { return Factory.NewMatrix(rows, cols); }
		public override string ToString() { return this.StringRep("g3", CultureInfo.InvariantCulture); }

	}
}

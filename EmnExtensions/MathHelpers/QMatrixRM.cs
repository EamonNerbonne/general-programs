using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
	public struct QMatrixRMFactory : IMatrixFactory<QMatrixRM>
	{
		public QMatrixRM NewMatrix(int rows, int cols) { return new QMatrixRM(rows, cols); }
	}


	public struct QMatrixRM : IMatrix<QMatrixRM>
	{

		internal double[] data;
		internal int cols;
		public int Rows { get { return data.Length / cols; } }
		public int Cols { get { return cols; } }
		internal static int Pos(int row, int col, int cols) { return row * cols + col; }
		public double this[int row, int col] { get { return data[Pos(row, col, cols)]; } set { data[Pos(row, col, cols)] = value; } }
		public QMatrixCM TransposeView { get { return new QMatrixCM { data = this.data, rows = cols }; } }
		public QMatrixRM Copy() { return new QMatrixRM { data = (double[])data.Clone(), cols = cols }; }

		public QMatrixRM(int rows, int cols) { this.cols = cols; this.data = new double[rows * cols]; }
		public void InitializeAs(int rows, int cols) { this.cols = cols; this.data = new double[rows * cols]; }


		public QMatrixRM NewMatrix(int rows, int cols) { QMatrixRM retval; retval.data = new double[rows * cols]; retval.cols = cols; return retval; }

		public static readonly QMatrixRMFactory Factory;
		public QMatrixRMFactory GetFactory() { return Factory; }
	}
}

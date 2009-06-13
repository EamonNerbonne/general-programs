using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{


	public struct QMatrixCMFactory : IMatrixFactory<QMatrixCM>
	{
		public QMatrixCM NewMatrix(int rows, int cols) { return new QMatrixCM(rows, cols); }
	}


	public struct QMatrixCM : IMatrix<QMatrixCM>
	{
		internal double[] data;
		internal int rows;
		public QMatrixCM(int rows, int cols) { this.rows = rows; this.data = new double[rows * cols]; }
		public void InitializeAs(int rows, int cols) { this.rows = rows; this.data = new double[rows * cols]; }
		public int Rows { get { return rows; } }
		public int Cols { get { return data.Length / rows; } }
		internal static int Pos(int row, int col, int rows) { return col * rows + row; }
		public double this[int row, int col] { get { return data[Pos(row, col, rows)]; } set { data[Pos(row, col, rows)] = value; } }
		public QMatrixRM TransposeView { get { return new QMatrixRM { data = this.data, cols = rows }; } }
		public QMatrixCM Copy() { return new QMatrixCM { data = (double[])data.Clone(), rows = rows }; }

		public QMatrixCM NewMatrix(int rows, int cols) { return new QMatrixCM(rows, cols); }

		public readonly static QMatrixCMFactory Factory;
		public QMatrixCMFactory GetFactory() { return Factory; }
	}

}

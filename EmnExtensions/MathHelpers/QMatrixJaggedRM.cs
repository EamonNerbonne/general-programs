using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
	public struct QMatrixJaggedRM : IMatrix<QMatrixJaggedRM>
	{
		internal double[][] data;
		public static implicit operator QMatrixJaggedRM(double[][] data) { int cols = QMatrixHelper.GetJaggedCols(data); return new QMatrixJaggedRM { data = data }; }
		public int Rows { get { return data.Length; } }
		public int Cols { get { return data[0].Length; } }
		public double this[int row, int col] { get { return data[row][col]; } set { data[row][col] = value; } }
		public QMatrixJaggedRM Copy() {
			QMatrixJaggedRM retval = new QMatrixJaggedRM();
			retval.data = new double[data.Length][];
			for (int row = 0; row < data.Length; row++) retval.data[row] = (double[])data[row].Clone();
			return retval;
		}


		public static readonly QMatrixJaggedRMFactory Factory;
		public QMatrixJaggedRMFactory GetFactory() { return Factory; }
		public QMatrixJaggedRM NewMatrix(int rows, int cols) { return Factory.NewMatrix(rows, cols); }
	}

	public struct QMatrixJaggedRMFactory : IMatrixFactory<QMatrixJaggedRM>
	{
		public QMatrixJaggedRM NewMatrix(int rows, int cols) {
			QMatrixJaggedRM retval;
			retval.data = new double[rows][];
			for (int row = 0; row < rows; row++)
				retval.data[row] = new double[cols];
			return retval;
		}
	}
}

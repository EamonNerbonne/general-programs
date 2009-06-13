using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
	public interface IMatrix
	{
		double this[int row, int col] { get; set; }
		int Rows { get; }
		int Cols { get; }
	}
	public interface IMatrix<T> : IMatrix,IMatrixFactory<T> where T : IMatrix<T>
	{
		//T NewMatrix(int rows, int cols);
//		T Copy();
		//	T InitializeFrom(Func<int, int, double> initialVal);
	}

	public interface IMatrixFactory<T> where T:IMatrix<T>
	{
		T NewMatrix(int rows, int cols);
	}
}

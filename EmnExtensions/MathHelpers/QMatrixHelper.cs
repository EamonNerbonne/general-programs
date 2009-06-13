using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.DebugTools;
using System.Globalization;

namespace EmnExtensions.MathHelpers
{
	public static class QMatrixHelper
	{
		public static IEnumerable<int> To(this int from, int to) {
			return Enumerable.Range(from, to - from);
		}

		public static TMat NewIdentityMatrix<TMat>(this IMatrixFactory<TMat> matCalc, int rowsAndCols)
			where TMat : IMatrix<TMat> {
			TMat matrix = matCalc.NewMatrix(rowsAndCols, rowsAndCols);

			for (int i = 0; i < rowsAndCols; i++)
				matrix[i, i] = 1;
			return matrix;
		}

		public static QMatrixCM MultCM(QMatrixRM A, QMatrixCM B) {
			if (A.Cols != B.Rows) throw new MatrixMismatchException();
			int rows = A.Rows, cols = B.Cols, ks = A.Cols;
			QMatrixCM CM = new QMatrixCM(rows, cols);
			int pos = 0;
			for (int col = 0; col < cols; col++)
				for (int row = 0; row < rows; row++) {
					int posA = row * A.cols;
					int posB = col * B.rows;
					for (int k = 0; k < ks; k++)
						CM.data[pos] += A.data[posA + k] * B.data[posB + k];

					pos++;
				}
			return CM;
		}

		public static QMatrixRM MultRM(QMatrixRM A, QMatrixCM B) {
			if (A.Cols != B.Rows) throw new MatrixMismatchException();
			int rows = A.Rows, cols = B.Cols, ks = A.Cols;
			QMatrixRM RM = new QMatrixRM(rows, cols);
			int pos = 0;
			for (int row = 0; row < rows; row++)
				for (int col = 0; col < cols; col++) {
					int posA = row * A.cols;
					int posB = col * B.rows;
					for (int k = 0; k < ks; k++)
						RM.data[pos] += A.data[posA + k] * B.data[posB + k];

					pos++;
				}
			return RM;
		}
		public static QMatrixRM MultRM(QMatrixRM A, QMatrixRM B) {
			if (A.Cols != B.Rows) throw new MatrixMismatchException();
			int rows = A.Rows, cols = B.Cols, ks = A.Cols;
			QMatrixRM RM = new QMatrixRM(rows, cols);
			int pos = 0;
			for (int row = 0; row < rows; row++)
				for (int col = 0; col < cols; col++) {
					int posA = row * A.cols;
					for (int k = 0; k < ks; k++)
						RM.data[pos] += A.data[posA + k] * B.data[col + k*B.cols];

					pos++;
				}
			return RM;
		}
		public static QMatrixRM Mult(QMatrixRM A, QMatrixCM B, QMatrixRM sample) { return MultRM(A, B); }
		public static QMatrixRM Mult(QMatrixRM A, QMatrixRM B, QMatrixRM sample) { return MultRM(A, B); }

		public static QMatrixCM Mult(QMatrixRM A, QMatrixCM B, QMatrixCM sample) { return MultCM(A, B); }

		internal static int GetJaggedCols(double[][] mat) {
			int cols = mat[0].Length;
			if (mat.FirstOrDefault(row => row.Length != cols) != null)
				throw new MatrixMismatchException("Jagged array has different length rows");
			return cols;
		}

		public static TMat InitializeFrom<TMat, TMat2>(this IMatrixFactory<TMat> factory, TMat2 matrixToCopy)
			where TMat : IMatrix<TMat>
			where TMat2 : IMatrix<TMat2> {
			TMat toInit = factory.NewMatrix(matrixToCopy.Rows, matrixToCopy.Cols);
			for (int row = 0; row < matrixToCopy.Rows; row++)
				for (int col = 0; col < matrixToCopy.Cols; col++)
					toInit[row, col] = matrixToCopy[row, col];
			return toInit;
		}

		public static TMat InitializeFrom<TMat>(this TMat matrix, Func<int, int, double> initializer)
			where TMat : IMatrix {
			int rows = matrix.Rows, cols = matrix.Cols;
			for (int row = 0; row < rows; row++)
				for (int col = 0; col < cols; col++)
					matrix[row, col] = initializer(row, col);
			return matrix;
		}

		//public static TMat New<TMat>(this TMat matrix, int rows, int cols) where TMat:IMatrix<TMat,TMatCalc> {
		//    return matrix;
		//}
		public static string StringRep(this IMatrix matrix) { return matrix.StringRep("g3", CultureInfo.InvariantCulture); }
		public static string StringRep(this IMatrix matrix, string format, IFormatProvider formatProvider) {
			string[,] rep = new string[matrix.Rows, matrix.Cols];
			int[] colwidth = new int[matrix.Cols];
			for (int i = 0; i < matrix.Rows; i++) {
				for (int j = 0; j < matrix.Cols; j++) {
					rep[i, j] = matrix[i, j].ToString(format, formatProvider);
					colwidth[i] = Math.Max(colwidth[i], rep[i, j].Length);
				}
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < matrix.Rows; i++) {
				sb.Append('(');
				for (int j = 0; j < matrix.Cols; j++) {
					sb.Append(rep[i, j]);
					sb.Append(' ', colwidth[j] + (j == matrix.Cols - 1 ? 0 : 2) - rep[i, j].Length);
				}
				sb.AppendLine(")");
			}
			return sb.ToString();
		}

	}

	public static class QMatrixComputations
	{
		//public static TMat2 ConvertTo<TMat2, TMat>(this IMatrixFactory<TMat2> sample,TMat other) 
		//    where TMat2:IMatrix<TMat2>
		//    where TMat : IMatrix<TMat>
		//{
		//    return sample.NewMatrix(other.Rows, other.Cols);
		//}
		public static TMat2 ConvertTo<TMat, TMat2Factory, TMat2>(this TMat matrix, TMat2Factory format)
			where TMat2Factory : IMatrixFactory<TMat2>
			where TMat2 : IMatrix<TMat2>
			where TMat : IMatrix<TMat> {
			var retval = format.NewMatrix(matrix.Rows, matrix.Cols);
			//for (int row = 0; row < retval.Rows; row++)
			//    for (int col = 0; col < retval.Cols; col++)
			//        retval[row, col] = matrix[row, col];
			return retval;
		}
		public static TMat2 ConvertTo<TMat, TMat2>(this TMat matrix, IMatrixFactory<TMat2> format)
			where TMat2 : IMatrix<TMat2>
			where TMat : IMatrix<TMat> {
			var retval = format.NewMatrix(matrix.Rows, matrix.Cols);
			//for (int row = 0; row < retval.Rows; row++)
			//    for (int col = 0; col < retval.Cols; col++)
			//        retval[row, col] = matrix[row, col];
			return retval;
		}
		public static void ConvertTo<TMat, TMat2>(this TMat matrix, out TMat2 target)
			where TMat2 : IMatrix<TMat2>, new()
			where TMat : IMatrix<TMat> {
			target = new TMat2().NewMatrix(matrix.Rows, matrix.Cols);
			//for (int row = 0; row < retval.Rows; row++)
			//    for (int col = 0; col < retval.Cols; col++)
			//        retval[row, col] = matrix[row, col];
		}
	}
}

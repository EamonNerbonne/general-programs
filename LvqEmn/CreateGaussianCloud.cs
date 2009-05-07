using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;

namespace LVQeamon
{
	public static class CreateGaussianCloud
	{
		/// <summary>
		/// Generates a Normally distributed point cloud transformed by "transformMatrix" translated to "mean".
		/// All matrices are in mat[col,row] indexing form.
		/// </summary>
		/// <param name="numPoints">The number of points in the cloud to generate.  retval.GetLength(0) == numPoints</param>
		/// <param name="numDimensions">The number of output dimensions for the point cloud.  retval.GetLength(1) == transformMatrix.GetLength(1) == numDimensions</param>
		/// <param name="transformMatrix">The matrix to transform with in mat[col,row] form.</param>
		/// <param name="mean">The mean of the resultant point cloud</param>
		/// <param name="rand">The random seed used to generate the piont cloud.</param>
		/// <returns>A normally distributed set of points as retval[pointIndex,dimensionIndex]</returns>
		public static double[,] GaussianCloud(int numPoints, int numDimensions, double[,] transformMatrix, double[] mean, MersenneTwister rand) {
			int preTransDimensions = transformMatrix.GetLength(0); //TODO more efficient: store transformMatrix in mat[row,col] form.
			if (numDimensions != transformMatrix.GetLength(1)) throw new MatrixMismatchException("numDimensions!=transformMatrix.GetLength(1)");
			if (numDimensions != mean.Length) throw new MatrixMismatchException("numDimensions!=mean.Length");

			double[,] points = new double[numPoints, numDimensions];
			double[] intermed = new double[preTransDimensions];
			for (int pI = 0; pI < numPoints; pI++) {

				for (int k = 0; k < preTransDimensions; k++)
					intermed[k] = rand.NextNormal();

				for (int dI = 0; dI < numDimensions; dI++) {
					points[pI, dI] = mean[dI];
					for (int k = 0; k < preTransDimensions; k++)
						points[pI, dI] += transformMatrix[k, dI] * intermed[k];
				}
			}
			return points;
		}

		public static double[,] RandomTransform(int numDimensions,MersenneTwister rand) {
			double[,] points = new double[numDimensions, numDimensions];
			for (int i = 0; i < numDimensions; i++) {
				for (int j = 0; j < numDimensions; j++) {
					points[i, j] = i==j? rand.NextDouble0To1()+0.2: rand.NextNormal();
				}
			}
			return points;
		}

		public static double[] RandomMean(int numDimensions) {

		}
	}
}

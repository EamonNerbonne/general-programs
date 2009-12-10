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
		public static double[,] GaussianCloud(int numPoints, int numDimensions, double[,] transformMatrix, double[] mean, MersenneTwister rand)
		{
			int preTransDimensions = transformMatrix.GetLength(0); //TODO more efficient: store transformMatrix in mat[row,col] form.
			if (numDimensions != transformMatrix.GetLength(1)) throw new MatrixMismatchException("numDimensions!=transformMatrix.GetLength(1)");
			if (numDimensions != mean.Length) throw new MatrixMismatchException("numDimensions!=mean.Length");

			double[,] points = new double[numPoints, numDimensions];
			double[] intermed = new double[preTransDimensions];
			for (int pI = 0; pI < numPoints; pI++)
			{

				for (int k = 0; k < preTransDimensions; k++)
					intermed[k] = rand.NextNormal();

				for (int dI = 0; dI < numDimensions; dI++)
				{
					points[pI, dI] = mean[dI];
					for (int k = 0; k < preTransDimensions; k++)
						points[pI, dI] += transformMatrix[k, dI] * intermed[k];
				}
			}
			return points;
		}

		public static void TransformPoints(double[,] points, double[,] transformMatrix)
		{
			int dimensions = transformMatrix.GetLength(0); //TODO more efficient: store transformMatrix in mat[row,col] form.
			if (dimensions != transformMatrix.GetLength(1)) throw new MatrixMismatchException("dimensions!=transformMatrix.GetLength(1)");
			if (dimensions != points.GetLength(1)) throw new MatrixMismatchException("points!=point dimensions");

			int numPoints = points.GetLength(0);
			double[] intermed = new double[dimensions];
			for (int pI = 0; pI < numPoints; pI++)
			{

				for (int k = 0; k < dimensions; k++)
					intermed[k] = points[pI, k];

				for (int dI = 0; dI < dimensions; dI++)
				{
					points[pI, dI] = 0;
					for (int k = 0; k < dimensions; k++)
						points[pI, dI] += transformMatrix[k, dI] * intermed[k];
				}
			}
		}


		public static double[,] RandomTransform(int numDimensions, MersenneTwister rand)
		{
			double[,] points = new double[numDimensions, numDimensions];
			for (int i = 0; i < numDimensions; i++)
			{
				double k = rand.NextNormal();
				for (int j = 0; j < numDimensions; j++)
				{
					points[i, j] = k * Math.Pow(rand.NextNormal(), 1);
				}
			}
			return points;
		}

		public static double[] RandomMean(int numDimensions, MersenneTwister rand, double meandev)
		{
			double[] mean = new double[numDimensions];
			for (int i = 0; i < numDimensions; i++)
			{
				mean[i] = meandev * rand.NextNormal();
			}
			return mean;
		}

		public static double[,] RandomStar(int numPoints, int numDimensions, double[][,] transformMatrices, double[][] means, double stardev, MersenneTwister rand)
		{
			if (means.Length != transformMatrices.Length) throw new MatrixMismatchException("inconsistent number of star tails");
			int starTailCount = transformMatrices.Length;
			if (starTailCount < 1) throw new MatrixMismatchException("Need at least 1 star tails to be useful");

			if(transformMatrices[0].GetLength(0) !=2 ||transformMatrices[0].GetLength(1)!= 2 || means[0].Length != 2) 
				throw new MatrixMismatchException("Star set must have a basic 2-d core");

			const int starDims = 2;

			var stardevs = Enumerable.Range(0, starTailCount).Select(i => RandomMean(starDims, rand, stardev).Zip(means[i], (a, b) => a + b).ToArray()).ToArray();

			double[,] points = new double[numPoints, numDimensions];
			double[] intermed = new double[starDims];
			for (int pI = 0; pI < numPoints; pI++) {
				int starI = rand.Next(starTailCount);

				for (int k = 0; k < starDims; k++)
					intermed[k] = rand.NextNormal();

				for (int dI = 0; dI < starDims; dI++)
				{
					points[pI, dI] = stardevs[starI][dI];
					for (int k = 0; k < starDims; k++)
						points[pI, dI] += transformMatrices[starI][k, dI] * intermed[k];
				}
				for (int dI = starDims; dI < numDimensions; dI++)
					points[pI, dI] = rand.NextNormal();
			}
			return points;
		}

		public static void InitStarSettings(int numStarTails, int numDimensions, double meandev, MersenneTwister rand, out double[][,] transformMatrices, out  double[][] means, out double[,] finalTransform)
		{
			const int starDim=2;
			transformMatrices = Enumerable.Range(0, numStarTails).Select(i => RandomTransform(starDim, rand)).ToArray();
			means = Enumerable.Range(0, numStarTails).Select(i => RandomMean(starDim, rand, meandev)).ToArray();
			finalTransform = RandomTransform(numDimensions, rand);
		}

	}
}

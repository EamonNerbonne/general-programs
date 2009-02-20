using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
	public struct Vector
	{
		public double[] elems;
		public Vector(int N) { elems = new double[N]; }
		public int N { get { return elems.Length; } }

		public static implicit operator Vector(double[] v) { return new Vector { elems = v }; }
		public static implicit operator double[](Vector v) { return v.elems; }

		public static Vector operator *(double a, Vector B) { return B.elems.Scale(a); }
		public static Vector operator *(Vector B, double a) { return B.elems.Scale(a); }
		/// <summary>
		/// Dot Product!
		/// </summary>
		public static double operator &(Vector A, Vector B) { return A.elems.Dot(B); }
		public static Vector operator +(Vector A, Vector B) { return A.elems.Add(B); }
		public static Vector operator -(Vector A, Vector B) { return A.elems.Sub(B); }
		public static Vector operator /(Vector A, double b) { return A.elems.Scale(1.0 / b); }

		public double this[int index] { get { return elems[index]; } set { elems[index] = value; } }
	}

	public static class VectorHelpers
	{
		public static double[] Add(this double[] vecA, double[] vecB) {
			var retval = new double[vecA.Length];
			for (int i = 0; i < vecA.Length; i++) {
				retval[i] = vecA[i] + vecB[i];
			}
			return retval;
		}
		public static double[] Sub(this double[] vecA, double[] vecB) {
			var retval = new double[vecA.Length];
			for (int i = 0; i < vecA.Length; i++) {
				retval[i] = vecA[i] - vecB[i];
			}
			return retval;
		}
		public static void AddTo(this double[] vecA, double[] vecB) {
			for (int i = 0; i < vecA.Length; i++) {
				vecA[i] += vecB[i];
			}
		}

		public static double Dot(this double[] vecA, double[] vecB) {
			var retval = 0.0;
			for (int i = 0; i < vecA.Length; i++) {
				retval += vecA[i] * vecB[i];
			}
			return retval;
		}

		public static double[] Scale(this double[] vecA, double factor) {
			double[] retval = new double[vecA.Length];
			for (int i = 0; i < vecA.Length; i++)
				retval[i] = vecA[i] * factor;
			return retval;
		}

		public static void ScaleTo(this double[] vecA, double factor) {
			for (int i = 0; i < vecA.Length; i++)
				vecA[i] *= factor;
		}

	}
}

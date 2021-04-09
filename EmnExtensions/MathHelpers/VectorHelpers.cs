using System.Diagnostics.CodeAnalysis;

namespace EmnExtensions.MathHelpers
{
    public struct Vector
    {
        public double[] elems;
        public Vector(int N) => elems = new double[N];
        public Vector(double[] v) => elems = v;
        public int N => elems.Length;

        public static implicit operator Vector(double[] v) => new() { elems = v };
        public static implicit operator double[](Vector v) => v.elems;
        public double[] ToArray() => elems;

        public static Vector operator *(double a, Vector B) => B.elems.Scale(a);
        public static Vector operator *(Vector B, double a) => B.elems.Scale(a);

        /// <summary>
        /// Dot Product!
        /// </summary>
        public static double operator &(Vector A, Vector B) => A.elems.Dot(B);

        [SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
        public static Vector operator +(Vector A, Vector B) => A.elems.Add(B);

        [SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract")]
        public static Vector operator -(Vector A, Vector B) => A.elems.Sub(B);

        public static Vector operator /(Vector A, double b) => A.elems.Scale(1.0 / b);

        public double this[int index]
        {
            get => elems[index];
            set => elems[index] = value;
        }
    }

    public static class VectorHelpers
    {
        public static double[] Add(this double[] vecA, double[] vecB)
        {
            var retval = new double[vecA.Length];
            for (var i = 0; i < vecA.Length; i++) {
                retval[i] = vecA[i] + vecB[i];
            }

            return retval;
        }

        public static double[] Sub(this double[] vecA, double[] vecB)
        {
            var retval = new double[vecA.Length];
            for (var i = 0; i < vecA.Length; i++) {
                retval[i] = vecA[i] - vecB[i];
            }

            return retval;
        }

        public static void AddTo(this double[] vecA, double[] vecB)
        {
            for (var i = 0; i < vecA.Length; i++) {
                vecA[i] += vecB[i];
            }
        }

        public static double Dot(this double[] vecA, double[] vecB)
        {
            var retval = 0.0;
            for (var i = 0; i < vecA.Length; i++) {
                retval += vecA[i] * vecB[i];
            }

            return retval;
        }

        public static double[] Scale(this double[] vecA, double factor)
        {
            var retval = new double[vecA.Length];
            for (var i = 0; i < vecA.Length; i++) {
                retval[i] = vecA[i] * factor;
            }

            return retval;
        }

        public static void ScaleTo(this double[] vecA, double factor)
        {
            for (var i = 0; i < vecA.Length; i++) {
                vecA[i] *= factor;
            }
        }
    }
}

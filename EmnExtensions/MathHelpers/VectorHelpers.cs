using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.MathHelpers
{
    public static class VectorHelpers
    {
        public static double[] Add(this double[] vecA, double[] vecB) {
            var retval = new double[vecA.Length];
            for (int i = 0; i < vecA.Length; i++) {
                retval[i] = vecA[i] + vecB[i];
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

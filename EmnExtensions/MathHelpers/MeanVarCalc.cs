using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace EmnExtensions.MathHelpers {
	public struct MeanVarCalc {
		double meanX, sX;
		double weightSum;


		public void Add(double val, double weight = 1.0) {
			if (weight == 0.0) return;//ignore zero-weight stuff...
			double newWeightSum = weightSum + weight;
			double mScale = weight / newWeightSum;
			double sScale = weightSum * weight / newWeightSum;
			weightSum = newWeightSum;
			sX += (val - meanX) * (val - meanX) * sScale;
			meanX += (val - meanX) * mScale;
		}
		public void Add(MeanVarCalc other) {
			double newWeightSum = weightSum + other.weightSum;
			double mScale = other.weightSum / newWeightSum;
			double sScale = weightSum * other.weightSum / newWeightSum;
			weightSum = newWeightSum;
			sX += other.sX + (other.meanX - meanX) * (other.meanX - meanX) * sScale;
			meanX += (other.meanX - meanX) * mScale;
		}

		public double Mean { get { return meanX; } }
		public double Var { get { return sX / weightSum; } }
		public double SampleVar { get { return sX / (weightSum - 1.0); } }
		public double Weight { get { return weightSum; } }
		public MeanVarCalc(double firstVal, double firstWeight = 1.0) {
			weightSum = firstWeight;
			meanX = firstVal;
			sX = 0.0;
		}

		public static MeanVarCalc[] ForValues(double[] val,double weight=1.0) {
			MeanVarCalc[] mvc = new MeanVarCalc[val.Length];
			for (int i = 0; i < val.Length; ++i)
				mvc[i] = new MeanVarCalc(val[i], weight);
			return mvc;
		}

		public static void Add(MeanVarCalc[] stat, double[] vals,double weight=1.0) {
			for (int i = 0; i < vals.Length; ++i)
				stat[i].Add(vals[i], weight);
		}


		public override string ToString() { return Mean.ToString(CultureInfo.InvariantCulture) + " +/- " + Math.Sqrt(SampleVar).ToString(CultureInfo.InvariantCulture); }
	}
}

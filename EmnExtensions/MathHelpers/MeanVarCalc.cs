using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EmnExtensions.MathHelpers
{
	public struct MeanVarDistrib
	{
		readonly double weightSum, sX, meanX;
		MeanVarDistrib(double _weightSum, double _sX, double _meanX) { meanX = _meanX; sX = _sX; weightSum = _weightSum; }

		public static MeanVarDistrib Init(double val, double weight = 1.0) { return new MeanVarDistrib(weight, 0.0, val); }//equivalent to adding to an empty distribution.

		public MeanVarDistrib Add(double val, double weight = 1.0)
		{
			if (weight == 0.0) return this;//ignore zero-weight stuff...
			double newWeightSum = weightSum + weight;
			double mScale = weight / newWeightSum;
			double sScale = weightSum * weight / newWeightSum;
			return new MeanVarDistrib(newWeightSum, sX + (val - meanX) * (val - meanX) * sScale, meanX + (val - meanX) * mScale);
		}

		public MeanVarDistrib Add(MeanVarDistrib other)
		{
			double newWeightSum = weightSum + other.weightSum;
			double mScale = other.weightSum / newWeightSum;
			double sScale = weightSum * other.weightSum / newWeightSum;
			return new MeanVarDistrib(newWeightSum, sX + other.sX + (other.meanX - meanX) * (other.meanX - meanX) * sScale, meanX + (other.meanX - meanX) * mScale);
		}

		public double Mean { get { return meanX; } }
		public double Var { get { return sX / weightSum; } }
		public double StdDev { get { return Math.Sqrt(Var); } }
		public double SampleVar { get { return sX / (weightSum - 1.0); } }
		public double SampleStdDev { get { return Math.Sqrt(SampleVar); } }
		public double Weight { get { return weightSum; } }

		public static MeanVarDistrib Of(IEnumerable<double> vals) { return vals.Aggregate(new MeanVarDistrib(), (mv, v) => mv.Add(v)); }

		public override string ToString() { return Mean.ToString(CultureInfo.InvariantCulture) + " +/- " + Math.Sqrt(SampleVar).ToString(CultureInfo.InvariantCulture); }

	}


	/*	public struct MeanVarCalc
		{
			double meanX, sX;
			double weightSum;

			public void Add(double val, double weight = 1.0)
			{
				if (weight == 0.0) return;//ignore zero-weight stuff...
				double newWeightSum = weightSum + weight;
				double mScale = weight / newWeightSum;
				double sScale = weightSum * weight / newWeightSum;
				weightSum = newWeightSum;
				sX += (val - meanX) * (val - meanX) * sScale;
				meanX += (val - meanX) * mScale;
			}

			public void Add(MeanVarCalc other)
			{
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
			public MeanVarCalc(double firstVal, double firstWeight = 1.0)
			{
				weightSum = firstWeight;
				meanX = firstVal;
				sX = 0.0;
			}

			public static MeanVarCalc[] ForValues(double[] val, double weight = 1.0)
			{
				MeanVarCalc[] mvc = new MeanVarCalc[val.Length];
				for (int i = 0; i < val.Length; ++i)
					mvc[i] = new MeanVarCalc(val[i], weight);
				return mvc;
			}

			public static MeanVarCalc Of(IEnumerable<double> vals)
			{
				MeanVarCalc mvc = new MeanVarCalc();
				foreach (var val in vals)
					mvc.Add(val);
				return mvc;
			}

			public static void Add(MeanVarCalc[] stat, double[] vals, double weight = 1.0)
			{
				for (int i = 0; i < vals.Length; ++i)
					stat[i].Add(vals[i], weight);
			}


			public override string ToString() { return Mean.ToString(CultureInfo.InvariantCulture) + " +/- " + Math.Sqrt(SampleVar).ToString(CultureInfo.InvariantCulture); }
		}*/
}

using System;
using System.Collections.Generic;
//using MoreLinq;
using System.IO;
using System.Linq;
using EmnExtensions;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;

namespace HwrDataModel
{
	public class GaussianEstimate
	{
		const double defaultWeight = 100.0;
		double mean, scaledVar, weightSum;
		public double Mean { get { return mean; } set { if (double.IsNaN(value) || !double.IsNaN(mean)) throw new ApplicationException("mean already set"); else mean = value; } }
		public double ScaledVariance { get { return scaledVar; } set { if (double.IsNaN(value) || !double.IsNaN(scaledVar)) throw new ApplicationException("scaledVar already set"); else scaledVar = value; } }
		public double WeightSum { get { return weightSum; } set { if (double.IsNaN(value) || !double.IsNaN(weightSum)) throw new ApplicationException("weightSum already set"); else weightSum = value; } }

		public double Variance { get { return scaledVar / weightSum; } }
		public double StdDev { get { return Math.Sqrt(Variance); } }

		public GaussianEstimate() : this(double.NaN, double.NaN, double.NaN) { }

		public GaussianEstimate(double mean, double scaledVar, double weightSum) { this.mean = mean; this.scaledVar = scaledVar; this.weightSum = weightSum; }

		public static GaussianEstimate CreateWithScaledVariance(double mean, double scaledVar, double weightSum) { return new GaussianEstimate(mean, scaledVar, weightSum); }
		public static GaussianEstimate CreateWithVariance(double mean, double variance, double weightSum) { return new GaussianEstimate(mean, weightSum * variance, weightSum); }
		public static GaussianEstimate CreateWithVariance(double mean, double variance) { return new GaussianEstimate(mean, variance * defaultWeight, defaultWeight); }
		public static GaussianEstimate operator +(GaussianEstimate a, GaussianEstimate b) { return GaussianEstimate.CreateWithVariance(a.Mean + b.Mean, a.Variance + b.Variance, 0.5 * a.weightSum + 0.5 * b.weightSum); }
	}
}
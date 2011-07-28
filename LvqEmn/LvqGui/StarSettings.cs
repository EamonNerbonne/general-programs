﻿using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public sealed class StarSettings : DatasetCreatorBase<StarSettings> {
#if DEBUG
		public int Dimensions=8;
		public int PointsPerClass=100;
#else
		public int Dimensions = 24;
		public int PointsPerClass = 1000;
#endif

		public int NumberOfClasses = 3;
		public int NumberOfClusters = 4;
		public int ClusterDimensionality = 4;
		public bool RandomlyTransformFirst = true;
		public double ClusterCenterDeviation = 1.5;
		public double IntraClusterClassRelDev = 0.5;
		public double NoiseSigma = 1.0;
		public double GlobalNoiseMaxSigma = 0.0;

		public uint ParamsSeed;

		protected override string RegexText {
			get {
				return @"
				^\s*(.*?--)?
				star-(?<Dimensions>\d+)D
				(?<ExtendDataByCorrelation>x?)
				(?<NormalizeDimensions>n?)-
				(?<NumberOfClasses>\d+)x(?<PointsPerClass>\d+)
				,(?<NumberOfClusters>\d+)
				\((?<ClusterDimensionality>\d+)D(?<RandomlyTransformFirst>r?)\)
				x(?<ClusterCenterDeviation>[^~i]+)[~i](?<IntraClusterClassRelDev>[^\[n]+)(n(?<NoiseSigma>[^\[g]+))?(g(?<GlobalNoiseMaxSigma>[^\[]+))?
				\[(?<ParamsSeed_>[0-9a-fA-F]+),(?<InstanceSeed_>[0-9a-fA-F]+)\]
				\^(?<Folds>\d+)\s*$"
					+ "|" +
					@"^\s*(.*?--)?
				star-(?<Dimensions>\d+)D
				(?<ExtendDataByCorrelation>\*?)
				(?<NormalizeDimensions>n?)-
				(?<NumberOfClasses>\d+)\*(?<PointsPerClass>\d+)
				:(?<NumberOfClusters>\d+)
				\((?<ClusterDimensionality>\d+)D(?<RandomlyTransformFirst>\??)\)
				\*(?<ClusterCenterDeviation>[^~]+)\~(?<IntraClusterClassRelDev>[^\[n]+)(n(?<NoiseSigma>[^\[]+))?
				\[(?<ParamsSeed>\d+):(?<InstanceSeed>\d+)\]
				/(?<Folds>\d+)\s*$";
			}
		}

		protected override string GetShorthand() {
			return "star-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (NormalizeDimensions ? "n" : "") + "-" + NumberOfClasses + "x" + PointsPerClass + ","
				+ NumberOfClusters + "(" + ClusterDimensionality + "D" + (RandomlyTransformFirst ? "r" : "") + ")x" + ClusterCenterDeviation.ToString("r") + "i"
				+ IntraClusterClassRelDev.ToString("r") + (NoiseSigma != 1.0 ? "n" + NoiseSigma.ToString("r") : "")
				+ (GlobalNoiseMaxSigma != 0.0 ? "g" + GlobalNoiseMaxSigma.ToString("r") : "") + "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]^" + Folds;
		}

		public override LvqDatasetCli CreateDataset() {
			return LvqDatasetCli.ConstructStarDataset(Shorthand,
				colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)ParamsSeed)),
				folds: Folds,
				extend: ExtendDataByCorrelation,
				normalizeDims: NormalizeDimensions,
				rngParamsSeed: ParamsSeed,
				rngInstSeed: InstanceSeed,
				dims: Dimensions,
				starDims: ClusterDimensionality,
				numStarTails: NumberOfClusters,
				classCount: NumberOfClasses,
				pointsPerClass: PointsPerClass,
				starMeanSep: ClusterCenterDeviation,
				starClassRelOffset: IntraClusterClassRelDev,
				randomlyTransform: RandomlyTransformFirst,
				noiseSigma: NoiseSigma,
				globalNoiseMaxSigma: GlobalNoiseMaxSigma
			);
		}
	}
}

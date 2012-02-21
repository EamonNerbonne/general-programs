using System.Globalization;
using System.Linq;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public sealed class StarDatasetSettings : DatasetCreatorBase<StarDatasetSettings> {
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
		public double GlobalNoiseMaxSigma;//0.0

		public uint ParamsSeed;

		protected override string RegexText {
			get {
				return @"
				^\s*(.*?--)?
				star-(?<Dimensions>\d+)D
				(?<ExtendDataByCorrelation>x?)
				(?<NormalizeDimensions>(?<NormalizeByScaling>S?)|n)-
				(?<NumberOfClasses>\d+)x(?<PointsPerClass>\d+)
				,(?<NumberOfClusters>\d+)
				\((?<ClusterDimensionality>\d+)D(?<RandomlyTransformFirst>r?)\)
				x(?<ClusterCenterDeviation>[^~i]+)
				[~i](?<IntraClusterClassRelDev>[^\[gn]+)
				(n(?<NoiseSigma>[^\[g]+))?
				(g(?<GlobalNoiseMaxSigma>[^\[]+))?
				(\[(?<ParamsSeed_>[\dA-Fa-f]+)?\,(?<InstanceSeed_>[\dA-Fa-f]+)?\])?
				(\^(?<Folds>\d+))?\s*$"
					;
			}
		}

		protected override string GetShorthand() {
			return "star-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (!NormalizeDimensions ? "" : NormalizeByScaling ? "S" : "n") + "-" + NumberOfClasses + "x" + PointsPerClass + ","
				+ NumberOfClusters + "(" + ClusterDimensionality + "D" + (RandomlyTransformFirst ? "r" : "") + ")x" + ClusterCenterDeviation.ToString("r") + "i"
				+ IntraClusterClassRelDev.ToString("r") + (NoiseSigma != 1.0 ? "n" + NoiseSigma.ToString("r") : "")
				+ (GlobalNoiseMaxSigma != 0.0 ? "g" + GlobalNoiseMaxSigma.ToString("r") : "")
				+ (ParamsSeed == defaults.ParamsSeed && InstanceSeed == defaults.InstanceSeed ? "" : "[" + (ParamsSeed == defaults.ParamsSeed ? "" : ParamsSeed.ToString("x")) + "," + (InstanceSeed == defaults.InstanceSeed ? "" : InstanceSeed.ToString("x")) + "]")
				+ (Folds == defaults.Folds ? "" : "^" + Folds);
		}

		public override LvqDatasetCli CreateDataset() {
			return LvqDatasetCli.ConstructStarDataset(Shorthand,
				colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)ParamsSeed)),
				folds: Folds,
				extend: ExtendDataByCorrelation,
				normalizeDims: NormalizeDimensions,
				normalizeByScaling: NormalizeByScaling,
				rngParamsSeed: ParamsSeed,
				rngInstSeed: InstanceSeed,
				dims: Dimensions,
				starDims: ClusterDimensionality,
				numStarTails: NumberOfClusters,
				classes: Enumerable.Range(0, NumberOfClasses).Select(i => i + 'A' <= 'Z' ? ((char)('A' + i)).ToString(CultureInfo.InvariantCulture) : i.ToString(CultureInfo.InvariantCulture)).ToArray(),
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

using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public class StarSettings : CloneableAs<StarSettings>, IHasShorthand, IDatasetCreator {
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
		public uint InstanceSeed;
		public int Folds = 10;

		public bool ExtendDataByCorrelation;
		public bool NormalizeDimensions;

		static readonly Regex shR =
			new Regex(@"
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
				/(?<Folds>\d+)\s*$"
				,
				RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public string Shorthand {
			get {
				return "star-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (NormalizeDimensions ? "n" : "") + "-" + NumberOfClasses + "x" + PointsPerClass + "," + NumberOfClusters + "(" + ClusterDimensionality + "D" + (RandomlyTransformFirst ? "r" : "") + ")x" + ClusterCenterDeviation.ToString("r") + "i" + IntraClusterClassRelDev.ToString("r")
					+ (NoiseSigma != 1.0 ? "n" + NoiseSigma.ToString("r") : "") + (GlobalNoiseMaxSigma != 0.0 ? "g" + GlobalNoiseMaxSigma.ToString("r") : "") + "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]^" + Folds;
			}
			set {
				var updated = ShorthandHelper.ParseShorthand(this, shR, value);
				if (!updated.Contains("NoiseSigma")) NoiseSigma = 1.0;
				if (!updated.Contains("GlobalNoiseMaxSigma")) GlobalNoiseMaxSigma = 0.0;
			}
		}

		public static StarSettings TryParse(string shorthand) { return ShorthandHelper.TryParseShorthand<StarSettings>(shR, shorthand); }

		public string ShorthandErrors { get { return ShorthandHelper.VerifyShorthand(this, shR); } }
		public LvqDatasetCli CreateDataset() {
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
		public void IncInstanceSeed() { InstanceSeed++; }
	}
}

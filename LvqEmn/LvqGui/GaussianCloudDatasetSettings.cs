using System.Linq;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;
using System.Globalization;

namespace LvqGui {
    public sealed class GaussianCloudDatasetSettings : DatasetCreatorBase<GaussianCloudDatasetSettings> {
        public int NumberOfClasses = 3;
#if DEBUG
        public int Dimensions=8;
        public int PointsPerClass=100;
#else
        public int Dimensions = 24;
        public int PointsPerClass = 1000;
#endif
        public double ClassCenterDeviation = 1.5;
        public uint ParamsSeed;

        protected override string RegexText {
            get {
                return @"^\s*(.*?--)?nrm-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>x?)(?<NormalizeDimensions>(?<NormalizeByScaling>S?)|n)-(?<NumberOfClasses>\d+)x(?<PointsPerClass>\d+)
                    \,(?<ClassCenterDeviation>[^\[]+)(\[(?<ParamsSeed_>[\dA-Fa-f]+)?\,(?<InstanceSeed_>[\dA-Fa-f]+)?\])?(\^(?<Folds>\d+))?\s*$"
                ;
            }
        }

        protected override string GetShorthand() {
            return "nrm-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (!NormalizeDimensions ? "" : NormalizeByScaling ? "S" : "n") + "-" + NumberOfClasses + "x" + PointsPerClass + ","
                + ClassCenterDeviation.ToString("r") + (ParamsSeed == defaults.ParamsSeed && InstanceSeed == defaults.InstanceSeed ? "" :
                "[" + (ParamsSeed == defaults.ParamsSeed ? "" : ParamsSeed.ToString("x")) + "," + (InstanceSeed == defaults.InstanceSeed ? "" : InstanceSeed.ToString("x")) + "]")
                + (Folds == defaults.Folds ? "" : "^" + Folds);
        }


        public override LvqDatasetCli CreateDataset() {
            return LvqDatasetCli.ConstructGaussianClouds(Shorthand,
                                                         folds: Folds,
                                                         extend: ExtendDataByCorrelation,
                                                         normalizeDims: NormalizeDimensions,
                                                         normalizeByScaling: NormalizeByScaling,
                                                         colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)ParamsSeed)),
                                                         rngParamsSeed: ParamsSeed,
                                                         rngInstSeed: InstanceSeed,
                                                         dims: Dimensions,
                                                         classes: Enumerable.Range(0, NumberOfClasses).Select(i=>i+'A'<='Z'? ((char)('A'+i)).ToString(CultureInfo.InvariantCulture):i.ToString(CultureInfo.InvariantCulture) ).ToArray(),
                                                         pointsPerClass: PointsPerClass,
                                                         meansep: ClassCenterDeviation
                );
        }


        public static GaussianCloudDatasetSettings InstableCross() {
            return new GaussianCloudDatasetSettings {
                PointsPerClass = 1000,
                Folds = 10,
                NumberOfClasses = 3,
                Dimensions = 24,
                ClassCenterDeviation = 1.0,
                ParamsSeed = 0x5122ea19,
                InstanceSeed = 0xc62ef64e,
            };
        }
        public static GaussianCloudDatasetSettings PlainCurvedBoundaryExample() {
            return new GaussianCloudDatasetSettings {
                PointsPerClass = 1000,
                Folds = 10,
                NumberOfClasses = 3,
                Dimensions = 12,
                ClassCenterDeviation = 1.8,
                ParamsSeed = 0xdff95b36,
                InstanceSeed = 0x64ea6990,
            };
        }
    }
}

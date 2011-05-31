﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using EmnExtensions.MathHelpers;
using EmnExtensions.Wpf;
using LvqLibCli;

namespace LvqGui {
	public class GaussianCloudSettings : CloneableAs<GaussianCloudSettings>, IHasShorthand {
		public int NumberOfClasses=3;
#if DEBUG
		public int Dimensions=8;
		public int PointsPerClass=100;
#else
		public int Dimensions=24;
		public int PointsPerClass=1000;
#endif
		public double ClassCenterDeviation=1.5;
		public uint ParamsSeed;
		public uint InstanceSeed;
		public int Folds=10;
		public bool ExtendDataByCorrelation;
		public bool NormalizeDimensions;



		static readonly Regex shR =
			new Regex(@"^\s*(.*?--)?nrm-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>x?)(?<NormalizeDimensions>n?)-(?<NumberOfClasses>\d+)x(?<PointsPerClass>\d+)
					\,(?<ClassCenterDeviation>[^\[]+)\[(?<ParamsSeed_>[\dA-Fa-f]+)\,(?<InstanceSeed_>[\dA-Fa-f]+)\]\^(?<Folds>\d+)\s*$"
				+ "|" +
					@"^\s*(.*?--)?nrm-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>\*?)(?<NormalizeDimensions>n?)-(?<NumberOfClasses>\d+)\*(?<PointsPerClass>\d+):(?<ClassCenterDeviation>[^\[]+)\[(?<ParamsSeed>\d+):(?<InstanceSeed>\d+)\]/(?<Folds>\d+)\s*$"
				,
				RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public string Shorthand {
			get {
				return "nrm-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (NormalizeDimensions ? "n" : "") + "-" + NumberOfClasses + "x" + PointsPerClass + "," + ClassCenterDeviation.ToString("r") + "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]^" + Folds;
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors {get { return ShorthandHelper.VerifyShorthand(this, shR); } }


		public LvqDatasetCli CreateDataset() {
			return LvqDatasetCli.ConstructGaussianClouds(Shorthand,
														 folds: Folds,
														 extend: ExtendDataByCorrelation,
														 normalizeDims: ExtendDataByCorrelation,
														 colors: WpfTools.MakeDistributedColors(NumberOfClasses, new MersenneTwister((int)ParamsSeed)),
														 rngParamsSeed: ParamsSeed,
														 rngInstSeed: InstanceSeed,
														 dims: Dimensions,
														 classCount: NumberOfClasses,
														 pointsPerClass: PointsPerClass,
														 meansep: ClassCenterDeviation
				);
		}
		public static GaussianCloudSettings InstableCross() {
			return new GaussianCloudSettings {
				PointsPerClass = 1000,
				Folds = 10,
				NumberOfClasses = 3,
				Dimensions = 24,
				ClassCenterDeviation=1.0,
				ParamsSeed = 0x5122ea19,
				InstanceSeed = 0xc62ef64e,
			};
		}
		public static GaussianCloudSettings PlainCurvedBoundaryExample() {
			return new GaussianCloudSettings {
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
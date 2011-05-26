using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LvqGui.CreatorGui {
	class GaussianCloudSettings : IHasShorthand {
		public int Dimensions;
		public int NumberOfClasses;
		public int PointsPerClass;
		public double ClassCenterDeviation;
		public uint ParamsSeed;
		public uint InstanceSeed;
		public int Folds;
		public bool ExtendDataByCorrelation;
		public bool NormalizeDimensions;
		static readonly Regex shR =
	new Regex(@"^\s*(.*?--)?nrm-(?<Dimensions>\d+)D(?<ExtendDataByCorrelation>x?)(?<NormalizeDimensions>n?)-(?<NumberOfClasses>\d+)x(?<PointsPerClass>\d+)
				\,(?<ClassCenterDeviation>[^\[]+)\[(?<ParamsSeed_>[\dA-Fa-f]+)\,(?<InstanceSeed_>[\dA-Fa-f]+)\]\^(?<Folds>\d+)\s*$",
		RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture|RegexOptions.IgnorePatternWhitespace);


		public string Shorthand {
			get {
				return "nrm-" + Dimensions + "D" + (ExtendDataByCorrelation ? "x" : "") + (NormalizeDimensions ? "n" : "") + "-" + NumberOfClasses + "x" + PointsPerClass + "," + ClassCenterDeviation.ToString("r") + "[" + ParamsSeed.ToString("x") + "," + InstanceSeed.ToString("x") + "]^" + Folds;
			}
			set { ShorthandHelper.ParseShorthand(this, shR, value); }
		}

		public string ShorthandErrors { [MethodImpl(MethodImplOptions.NoInlining)]get { return ShorthandHelper.VerifyShorthand(this, shR); } }


	}
}

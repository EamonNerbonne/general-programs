using System;
using System.Collections.Generic;
using MoreLinq;
using System.IO;
using System.Linq;

namespace HwrDataModel
{
	public class GaussianEstimate
	{
		const double defaultWeight = 100.0;
		double mLength, sLength, weightSum;
		public double Mean { get { return mLength; } }
		public double Variance { get { return sLength / weightSum; } }
		public double StdDev { get { return Math.Sqrt(Variance); } }
		public double WeightSum { get { return weightSum; } }

		public static GaussianEstimate operator +(GaussianEstimate a, GaussianEstimate b) { return GaussianEstimate.Create(a.Mean + b.Mean, a.Variance + b.Variance, 0.5*a.weightSum + 0.5*b.weightSum); }
		public static GaussianEstimate CreateWithDefaultWeight(double len, double var)
		{
			return new GaussianEstimate { mLength = len, sLength = var * defaultWeight, weightSum = defaultWeight, };
		}

		public static GaussianEstimate CreateFromWeightSum(double len, double slen, double wsum)
		{
			return new GaussianEstimate { mLength = len, sLength = slen, weightSum = wsum, };
		}

		public static GaussianEstimate Create(double len, double var, double withSum)
		{
			return new GaussianEstimate { mLength = len, sLength = withSum*var, weightSum = withSum, };
		}

	}

	public class FeatureDistributionEstimate
	{
		public double weightSum;
		public double[] means,variances;
	}

	public class SymbolClass
	{
		public char Letter { get; private set; }//by agreement, char 0 is str-start, char 1 is unknown, char 10 is str-end, and char 32 is space
		public uint Code { get; private set; }
		public GaussianEstimate Length { get; private set; }
		public FeatureDistributionEstimate[] State { get; set; }
		public SymbolClass(char c, GaussianEstimate lengthEstimate) { this.Letter = c; this.Code = uint.MaxValue; this.Length = lengthEstimate; }
		public SymbolClass(char c, uint code, GaussianEstimate lengthEstimate) { this.Letter = c; this.Code = code; this.Length = lengthEstimate; }
		public void SetCode(uint newcode) { if (Code != uint.MaxValue) throw new ApplicationException("code already set"); else Code = newcode; }

		public IEnumerable<SymbolClass> Split(int intoCharPhases)
		{
			if(Code == uint.MaxValue) throw new ApplicationException("Code not set");
			for (int i = 0; i < intoCharPhases; i++)
				yield return new SymbolClass(this.Letter,
					Code * (uint)intoCharPhases + (uint)i,
					GaussianEstimate.Create(Length.Mean / intoCharPhases, Length.Variance / intoCharPhases, Length.WeightSum));
		}

	}


	public static class SymbolClassParser
	{

		/// <summary>
		/// Special chars: 
		/// 0 - start of line
		/// 1 - unknown char
		/// 10 - end of line
		/// 32 - general word spacing (add this to EVERY word once).
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static SymbolClass[] Parse(FileInfo file, int charPhases)
		{
			//TODO: known limitation: culture-sensitive parsing.
			Dictionary<char, SymbolClass> symbolsByChar;
			using (var reader = file.OpenText())
				symbolsByChar = reader.ReadToEnd()
						.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
						.Where(parts => parts.Length == 4)//no empty lines!
						.Select(parts => new SymbolClass(
							(char)int.Parse(parts[0]),
							GaussianEstimate.CreateWithDefaultWeight(double.Parse(parts[1]), double.Parse(parts[2]))
						))
						.ToDictionary(symbolWidth => symbolWidth.Letter);

			symbolsByChar[(char)1] = symbolsByChar.Values.Any()
				? new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithDefaultWeight(symbolsByChar.Values.Select(sw => sw.Length.Mean).Average(), symbolsByChar.Values.Select(sw => sw.Length.Variance).Average() * 9)
					)
				: new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithDefaultWeight(50, 1000)
					);

			var symbolClasses = symbolsByChar.Values.OrderBy(c => c.Letter).ToArray();
			for(int i=0;i<symbolClasses.Length;i++)
				symbolClasses[i].SetCode((uint)i);
			//OK, now we have the symbol classes for one char-phase.

			var phaseSplitSymbols = symbolClasses.SelectMany(symbolClass => symbolClass.Split(charPhases)).ToArray();
			return phaseSplitSymbols;
		}
	}
}

using System;
using System.Collections.Generic;
using MoreLinq;
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

	public class FeatureDistributionEstimate : IXmlSerializable
	{

		public double weightSum;
		public double[] means, scaledVars;

		public System.Xml.Schema.XmlSchema GetSchema() { throw new NotImplementedException(); }

		static double ToDouble(XElement elem) { return double.Parse(elem.Value, CultureInfo.InvariantCulture); }
		static double ToDouble(XAttribute elem) { return double.Parse(elem.Value, CultureInfo.InvariantCulture); }
		public void ReadXml(XmlReader reader) {
			XElement xml = (XElement)XElement.ReadFrom(reader);
			weightSum = ToDouble(xml.Element("weightSum"));
			if (xml.Element("features") != null) {
				means = xml.Element("features").Elements().Attributes("mean").Select(xDouble => ToDouble(xDouble)).ToArray();
				scaledVars = xml.Element("features").Elements().Attributes("scaledVar").Select(xDouble => ToDouble(xDouble)).ToArray();
			} else {
				means = xml.Element("means").Elements("double").Select(xDouble => ToDouble(xDouble)).ToArray();
				scaledVars = xml.Element("scaledVars").Elements("double").Select(xDouble => ToDouble(xDouble)).ToArray();
			}
		}

		public void WriteXml(XmlWriter writer) {
			new XElement("weightSum", weightSum).WriteTo(writer);
			new XElement("features", FeaturesAsXml).WriteTo(writer);
		}

		static string[] featureNames;
		public static string[] FeatureNames {
			get { return featureNames; }
			set {
				featureNames = value.ToArray();
				string lastname = "UNKNOWN";
				int lastI = 0;
				for (int i = 0; i < featureNames.Length; i++) {
					string name;
					if (featureNames[i] != null) {
						name = lastname = featureNames[i];
						lastI = i;
					} else {
						name = lastname;
					}
					if (FeatureNames[i] == null || (i + 1 < featureNames.Length && featureNames[i + 1] == null))
						name += "_" + (i - lastI);

					featureNames[i] = name;
				}

			}
		}
		IEnumerable<XElement> FeaturesAsXml {
			get {
				for (int i = 0; i < means.Length; i++) {
					yield return
						new XElement(featureNames == null ? "unknown" : featureNames[i],
							new XAttribute("mean", means[i].ToString("R", CultureInfo.InvariantCulture)),
							new XAttribute("stddev", Math.Sqrt(scaledVars[i] / weightSum)),
							new XAttribute("scaledVar", scaledVars[i].ToString("R", CultureInfo.InvariantCulture)));
				}
			}
		}
	}

	public class SymbolClass
	{

		char? letter;//by agreement, char 0 is str-start, char 1 is unknown, char 10 is str-end, and char 32 is space
		uint? code;
		public char Letter { get { return letter.Value; } set { if (letter.HasValue && letter.Value != value) throw new ApplicationException("letter already set"); else letter = value; } }
		public string LetterReadable {
			get { return letter.HasValue ? (letter.Value <= ' ' ? ((int)letter.Value).ToString() : "'" + letter.Value.ToString() + "'") : "<null>"; }
			set {
				char newVal = value.StartsWith("'")
					? value[1]
					: (char)int.Parse(value);
				Letter = newVal;
			}
		}

		public uint Code { get { return code.Value; } set { if (code.HasValue) throw new ApplicationException("code already set"); code = value; } }
		public GaussianEstimate Length { get; set; }
		public FeatureDistributionEstimate[][] SubPhase { get; set; }
		public SymbolClass() { }
		public SymbolClass(char c, GaussianEstimate lengthEstimate) { this.Letter = c; this.Length = lengthEstimate; }
		public SymbolClass(char c, uint code, GaussianEstimate lengthEstimate) { this.Letter = c; this.Code = code; this.Length = lengthEstimate; }
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
		public static SymbolClass[] Parse(FileInfo file) {
			//TODO: known limitation: culture-sensitive parsing.
			Dictionary<char, SymbolClass> symbolsByChar;
			using (var reader = file.OpenText())
				symbolsByChar = reader.ReadToEnd()
						.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
						.Select(line => line.Split(',').Select(part => part.Trim()).ToArray())
						.Where(parts => parts.Length == 4)//no empty lines!
						.Select(parts => new SymbolClass(
							(char)int.Parse(parts[0]),
							GaussianEstimate.CreateWithVariance(double.Parse(parts[1]), double.Parse(parts[2]))
						))
						.ToDictionary(symbolWidth => symbolWidth.Letter);

			symbolsByChar[(char)1] = symbolsByChar.Values.Any()
				? new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithVariance(symbolsByChar.Values.Select(sw => sw.Length.Mean).Average(), symbolsByChar.Values.Select(sw => sw.Length.Variance).Average() * 9)
					)
				: new SymbolClass(
					(char)1,
					GaussianEstimate.CreateWithVariance(50, 1000)
					);

			var symbolClasses = symbolsByChar.Values.OrderBy(c => c.Letter).ToArray();
			for (int i = 0; i < symbolClasses.Length; i++)
				symbolClasses[i].Code = (uint)i;
			return symbolClasses;
		}
	}
}

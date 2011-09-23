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

	public class FeatureDistributionEstimate : IXmlSerializable
	{

		public double weightSum;
		public double[] means, scaledVars;

		public System.Xml.Schema.XmlSchema GetSchema() { throw new NotImplementedException(); }

		static double ToDouble(XElement elem) { return double.Parse(elem.Value, CultureInfo.InvariantCulture); }
		static double ToDouble(XAttribute elem) { return double.Parse(elem.Value, CultureInfo.InvariantCulture); }
		public void ReadXml(XmlReader reader)
		{
			XElement xml = (XElement)XElement.ReadFrom(reader);
			weightSum = ToDouble(xml.Element("weightSum"));
			if (xml.Element("features") != null)
			{
				means = xml.Element("features").Elements().Attributes("mean").Select(xDouble => ToDouble(xDouble)).ToArray();
				scaledVars = xml.Element("features").Elements().Attributes("scaledVar").Select(xDouble => ToDouble(xDouble)).ToArray();
			}
			else
			{
				means = xml.Element("means").Elements("double").Select(xDouble => ToDouble(xDouble)).ToArray();
				scaledVars = xml.Element("scaledVars").Elements("double").Select(xDouble => ToDouble(xDouble)).ToArray();
			}
		}

		public void WriteXml(XmlWriter writer)
		{
			new XElement("weightSum", weightSum).WriteTo(writer);
			new XElement("features", FeaturesAsXml).WriteTo(writer);
		}

		static string[] featureNames;
		public static string[] FeatureNames
		{
			get { return featureNames; }
			set
			{
				featureNames = value.ToArray();
				string lastname = "UNKNOWN";
				int lastI = 0;
				for (int i = 0; i < featureNames.Length; i++)
				{
					string name;
					if (featureNames[i] != null)
					{
						name = lastname = featureNames[i];
						lastI = i;
					}
					else
					{
						name = lastname;
					}
					if (FeatureNames[i] == null || (i + 1 < featureNames.Length && featureNames[i + 1] == null))
						name += "_" + (i - lastI);

					featureNames[i] = name;
				}

			}
		}
		IEnumerable<XElement> FeaturesAsXml
		{
			get
			{
				for (int i = 0; i < means.Length; i++)
				{
					yield return
						new XElement(featureNames == null ? "unknown" : featureNames[i],
							new XAttribute("mean", means[i].ToString("R", CultureInfo.InvariantCulture)),
							new XAttribute("stddev", Math.Sqrt(scaledVars[i] / weightSum)),
							new XAttribute("scaledVar", scaledVars[i].ToString("R", CultureInfo.InvariantCulture)));
				}
			}
		}
	}
}
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;

namespace HwrDataModel
{
	public class Word : ShearedBox, IAsXml
	{
		public string text;
		public int no;
		public TrackStatus leftStat, rightStat, topStat, botStat;
		public enum TrackStatus { Uninitialized = 0, Initialized, Calculated, Manual }

		public GaussianEstimate symbolBasedLength;
		public object guiTag;
		// public double cost = double.NaN;
		public Word()
		{
			leftStat = rightStat = topStat = botStat = TrackStatus.Uninitialized;
		}
		public Word(string text, int no, double top, double bottom, double left, double right, double shear)
			: base(top, bottom, left, right, shear)
		{
			this.text = text;
			this.no = no;
			leftStat = rightStat = topStat = botStat = TrackStatus.Initialized;
		}
		public void EstimateLength(Dictionary<char, GaussianEstimate> symbolWidths)
		{
			symbolBasedLength = EstimateWordLength(text, symbolWidths);
		}

		public Word(Word toCopy)
			: this(toCopy.text, toCopy.no, toCopy.top, toCopy.bottom, toCopy.left, toCopy.right, toCopy.shear)
		{
			leftStat = toCopy.leftStat;
			rightStat = toCopy.rightStat;
			topStat = toCopy.topStat;
			botStat = toCopy.botStat;

			symbolBasedLength = toCopy.symbolBasedLength;
		}
		public Word(XElement fromXml)
			: base(fromXml)
		{
			text = (string)fromXml.Attribute("text");
			no = (int)fromXml.Attribute("no");
			leftStat = rightStat = topStat = botStat = TrackStatus.Calculated;//TODO, these should be saved in the XML

		}


		public XNode AsXml()
		{
			return new XElement("Word",
				new XAttribute("no", no),
				base.MakeXAttrs(),
				new XAttribute("text", text)
				);
		}

		static GaussianEstimate EstimateWordLength(string word, Dictionary<char, GaussianEstimate> symbolWidths)
		{
			return word
				.Select(c => symbolWidths.ContainsKey(c) ? symbolWidths[c] : symbolWidths[(char)1])
				.Aggregate((a, b) => a + b);
		}
	}
}

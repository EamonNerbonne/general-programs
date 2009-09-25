using System.Xml.Linq;
using System.Linq;
using MoreLinq;
using System.Collections.Generic;

namespace HwrDataModel
{
	public class HwrTextWord : ShearedBox, IAsXml
	{
		public readonly HwrTextLine line;
		public string text;
		public int no;
		public HwrEndpointStatus leftStat, rightStat, topStat, botStat;

		//public GaussianEstimate symbolBasedLength;
		public object guiTag;
		// public double cost = double.NaN;
		public HwrTextWord(HwrTextLine line)
		{
			this.line = line;
			leftStat = rightStat = topStat = botStat = HwrEndpointStatus.Uninitialized;
		}
		public HwrTextWord(HwrTextLine line, string text, int no, double top, double bottom, double left, double right, double shear)
			: base(top, bottom, left, right, shear)
		{
			this.line = line;
			this.text = text;
			this.no = no;
			leftStat = rightStat = topStat = botStat = HwrEndpointStatus.Uninitialized;
		}
		public HwrTextWord(HwrTextLine line, XElement fromXml,HwrEndpointStatus wordStatus)
			: base(fromXml) {
			this.line = line;
			text = (string)fromXml.Attribute("text");
			no = (int)fromXml.Attribute("no");
			leftStat = rightStat = topStat = botStat = wordStatus;//TODO, these should be saved in the XML
		}

		//includes the endpoint for the preceeding space.
		public IEnumerable<int> ManualEndPoints
		{
			get
			{
				return
				(leftStat == HwrEndpointStatus.Manual ? (int)(left + 0.5) : -1)
					.Concat(Enumerable.Repeat(-1, text.Length - 1))
					.Concat(rightStat == HwrEndpointStatus.Manual ? (int)(right + 0.5) : -1);
			}
		}
		public GaussianEstimate symbolBasedLength { get; private set; }
		public GaussianEstimate EstimateLength(Dictionary<char, GaussianEstimate> symbolWidths)
		{
			return (symbolBasedLength = EstimateWordLength(text, symbolWidths) + symbolWidths[(char)32]);
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

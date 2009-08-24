using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

namespace DataIO
{
	public class TextLine : ShearedBox, IAsXml
	{
		public readonly Word[] words; //after construction, words are fixed - you can only recompute their positions.
		public readonly int no;

		public int bodyTop;//TODO: compute
		public int bodyBot;


		public TextLine() { }
		public TextLine(string text, int no, double top, double bottom, double left, double right, double shear, Dictionary<char, SymbolWidth> symbolWidths)
			: base(top, bottom, left, right, shear) {
			this.no = no;
			this.words = text
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((t, i) => new Word(t, i + 1, top, bottom, 0.0, 0.0, shear))
			.ToArray();
			GuessWordsInString(symbolWidths);
		}


		private void GuessWordsInString( Dictionary<char, SymbolWidth> symbolWidths) {
			foreach (var word in words)
				word.EstimateLength(symbolWidths);

			var lengthEstimates = words.Select(word => word.symbolBasedLength);

			LengthEstimate
				start = symbolWidths[(char)0].estimate,
				end = symbolWidths[(char)10].estimate;

			LengthEstimate totalEstimate = start + lengthEstimates.Aggregate((a, b) => a + b) + end;
			double wordwiseStddevTotal = start.stddev + lengthEstimates.Select(est=>est.stddev).Sum() + end.stddev;

			//ok, so we have a total line length and a per word estimate
			double correctionPerStdDev = (right - left-totalEstimate.len) / wordwiseStddevTotal;
			double position = left + start.len+ start.stddev *correctionPerStdDev;
			foreach(Word word in words) {
				word.left = position;
				position += word.symbolBasedLength.len + word.symbolBasedLength.stddev * correctionPerStdDev;
				word.right = position;
			}
			position += end.len+ end.stddev * correctionPerStdDev;
			Debug.Assert(Math.Abs(position - right) < 1, "math error");
		}

		public TextLine(XElement fromXml)
			: base(fromXml) {
			no = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new Word(xmlWord)).ToArray();
		}

		public XNode AsXml() {
			return new XElement("TextLine",
				new XAttribute("no", no),
				base.MakeXAttrs(),
				words.Select(word => word.AsXml())
					);
		}
	}
}

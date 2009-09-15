using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;
using System.Xml.Linq;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;

namespace HwrDataModel
{
	public class TextLine : ShearedBox, IAsXml
	{
		public readonly Word[] words; //after construction, words are fixed - you can only recompute their positions.
		public readonly int no;

		public int bodyTop, bodyTopAlt;//bodyTop/bodyBot are relative to line top, not to page top.
		public int bodyBot, bodyBotAlt;//bodyTop/bodyBot are relative to line top, not to page top.

		public double OuterExtremeLeft { get { return left + BottomXOffset - 10; } }
		public double OuterExtremeRight { get { return right + 30; } } //hacky

		int[] computedCharEndpoints;
		double computedLikelihood = double.NaN;
		public int[] ComputedCharEndpoints { get { return computedCharEndpoints; } }
		public double ComputedLikelihood { get { return computedLikelihood; } }
		public void SetComputedCharEndpoints(int[] endpoints,  double likelihood, Word.TrackStatus endpointSource)
		{

			//TODO:implement
		}


		public TextLine() { }
		public TextLine(string text, int no, double top, double bottom, double left, double right, double shear, Dictionary<char, GaussianEstimate> symbolWidths)
			: base(top, bottom, left, right, shear) {
			this.no = no;
			this.words = text
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((t, i) => new Word(this,t, i + 1, top, bottom, 0.0, 0.0, shear))
			.ToArray();
			GuessWordsInString(symbolWidths);
		}

		public string FullText { get { return string.Join(" ", words.Select(w => w.text).ToArray()); } }

		public IEnumerable<char> TextWithTerminators {
			get {
				return
					new char[] { (char)0, ' ' }.Concat(FullText).Concat(new char[] { ' ', (char)10 });
			}
		}

		public IEnumerable<int> ManualEndPoints
		{
			get
			{
				return
				(-1) //startsymbol end
					.Concat(words.SelectMany(word => word.ManualEndPoints)) //endpoints of each word and its preceeding space
					.Concat(-1) //endpoint of final space
					.Concat(-1); //endpoint of endsym
			}
		}

		private void GuessWordsInString(Dictionary<char, GaussianEstimate> symbolWidths) {
			foreach (var word in words)
				word.EstimateLength(symbolWidths);

			var lengthEstimates = words.Select(word => word.symbolBasedLength);

			GaussianEstimate
				start = symbolWidths[(char)0],
				end = symbolWidths[(char)10];

			GaussianEstimate totalEstimate = start + lengthEstimates.Aggregate((a, b) => a + b) + end;
			double wordwiseStddevTotal = start.StdDev + lengthEstimates.Select(est => est.StdDev).Sum() + end.StdDev;

			//ok, so we have a total line length and a per word estimate
			double correctionPerStdDev = (right - left - totalEstimate.Mean) / wordwiseStddevTotal;
			double position = left + start.Mean + start.StdDev * correctionPerStdDev;
			foreach (Word word in words) {
				word.left = position;
				position += word.symbolBasedLength.Mean + word.symbolBasedLength.StdDev * correctionPerStdDev;
				word.right = position;
			}
			position += end.Mean + end.StdDev * correctionPerStdDev;
			Debug.Assert(Math.Abs(position - right) < 1, "math error");
		}

		public TextLine(XElement fromXml)
			: base(fromXml) {
			no = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new Word(this, xmlWord)).ToArray();
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

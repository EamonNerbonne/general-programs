using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using MoreLinq;

namespace HwrDataModel
{
	public class HwrTextLine : ShearedBox, IAsXml
	{
		public readonly HwrTextWord[] words; //after construction, words are fixed - you can only recompute their positions.
		public readonly int nr;
		public readonly HwrTextPage Page;

		public int bodyTop, bodyTopAlt;//bodyTop/bodyBot are relative to line top, not to page top.
		public int bodyBot, bodyBotAlt;//bodyTop/bodyBot are relative to line top, not to page top.

		public double OuterExtremeLeft { get { return left + BottomXOffset - 10; } }
		public double OuterExtremeRight { get { return right + 30; } } //hacky

		int[] computedCharEndpoints;
		double computedLikelihood = double.NaN;
		public int[] ComputedCharEndpoints { get { return computedCharEndpoints; } }
		public double ComputedLikelihood { get { return computedLikelihood; } }
		public bool SetComputedCharEndpoints(int[] endpoints, double likelihood, HwrEndpointStatus endpointSource)
		{
			bool possibleError=false;
			//remember: don't overwrite manually set endpoints!!!!

			computedLikelihood = likelihood;
			computedCharEndpoints = endpoints;

			char[] textChars = TextWithTerminators.ToArray();
			if (textChars.Length != endpoints.Length) throw new ArgumentException(string.Format("Passed {0} endspoints for {1} characters ({0}!={1})", endpoints.Length, textChars.Length));

			int currWordI = -1; //start before the first word.

			for (int i = 0; i < textChars.Length; i++)
			{
				if (textChars[i] == ' ')
				{ //found word boundary
					if (currWordI >= 0)
					{ //then the character right before this space was the rightmost character of the previous word.
						if (words[currWordI].rightStat != HwrEndpointStatus.Manual)
						{
							words[currWordI].right = endpoints[i - 1];
							words[currWordI].rightStat = endpointSource;
						}
						else
						{
							if (Math.Abs(words[currWordI].right - endpoints[i - 1]) > 2) {
								Console.Write("Calculated endpoint differs significantly from manual example!\nHwrLine {0}: {1}\nWord {2}: {3}\nPosition is {4} but should be {5}\n", nr, FullText, currWordI, words[currWordI].text, endpoints[i - 1], words[currWordI].right);
								possibleError = true;
							}
						}
					}
					currWordI++;//space means new word
					if (currWordI < words.Length) //then the endpos of the space must be the beginning pos of the new word.
					{
						if (words[currWordI].leftStat != HwrEndpointStatus.Manual)
						{
							words[currWordI].left = endpoints[i];
							words[currWordI].leftStat = endpointSource;
						}
						else
						{
							if (Math.Abs(words[currWordI].left - endpoints[i]) > 2) {
								Console.Write("Calculated startpoint differs significantly from manual example!\nHwrLine {0}: {1}\nWord {2}: {3}\nPosition is {4} but should be {5}\n", nr, FullText, currWordI, words[currWordI].text, endpoints[i], words[currWordI].left);
								possibleError = true;
							}
						}
					}
				}
			}
			if (currWordI != words.Length)
				throw new ApplicationException("programmer error: currWordI(" + currWordI + ") != words.Length (" + words.Length + ")");
			return possibleError;
		}


		public HwrTextLine(HwrTextPage page) { Page = page; }
		public HwrTextLine(HwrTextPage page, string text, int lineNumber, double top, double bottom, double left, double right, double shear)
			: base(top, bottom, left, right, shear)
		{
			Page = page;
			nr = lineNumber;
			words = text
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((t, i) => new HwrTextWord(this, t, i + 1, top, bottom, left, left, shear))
			.ToArray();
		}
		public HwrTextLine(HwrTextPage page, XElement fromXml, HwrEndpointStatus wordSource)
			: base(fromXml)
		{
			Page = page;
			nr = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new HwrTextWord(this, xmlWord, wordSource)).ToArray();
		}


		public string FullText { get { return string.Join(" ", words.Select(w => w.text).ToArray()); } }

		public IEnumerable<char> TextWithTerminators
		{
			get
			{
				return
					new[] { (char)0, ' ' }.Concat(FullText).Concat(new[] { ' ', (char)10 });
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

		public void EstimateWordBoundariesViaSymbolLength(Dictionary<char, GaussianEstimate> symbolWidths)
		{

			GaussianEstimate
				start = symbolWidths[(char)0],
				end = symbolWidths[(char)10];

			var wordEstimates =
				new { HwrTextWord = (HwrTextWord)null, Length = start }
				.Concat(words.Select(word => new { HwrTextWord = word, Length = word.EstimateLength(symbolWidths) }))
				.Concat(new { HwrTextWord = (HwrTextWord)null, Length = end })
				.ToArray();

			int currWordI = 0;
			double edgeLeft = left;

			Action<int, double> distributeWord = (nextWordI, edgeRight) =>
			{
				if (nextWordI != currWordI)
				{
					//spread words [currWordI, i) over [edgeLeft, wordEstimates[i].HwrTextWord.left]
					var relevantEsts = wordEstimates.Skip(currWordI).Take(nextWordI - currWordI);
					GaussianEstimate totalEstimate = relevantEsts.Select(w => w.Length).Aggregate((a, b) => a + b);
					double wordwiseStddevTotal = relevantEsts.Select(w => w.Length).Select(est => est.StdDev).Sum();

					double correctionPerStdDev = (edgeRight - edgeLeft - totalEstimate.Mean) / wordwiseStddevTotal;
					double position = edgeLeft;

					//ok, so we have a total segment length and a per word estimate
					foreach (var wordEst in relevantEsts)
					{
						HwrTextWord word = wordEst.HwrTextWord;
						if (word == null)
						{
							position += wordEst.Length.Mean + wordEst.Length.StdDev * correctionPerStdDev;
						}
						else
						{
							if (word.leftStat > HwrEndpointStatus.Initialized)
							{
								Debug.Assert(Math.Abs(position - word.left) < 1, "math error(left)");
							}
							else
							{
								word.left = position;
								word.leftStat = HwrEndpointStatus.Initialized;
							}
							position += wordEst.Length.Mean + wordEst.Length.StdDev * correctionPerStdDev;
							if (word.rightStat > HwrEndpointStatus.Initialized)
							{
								Debug.Assert(Math.Abs(position - word.right) < 1, "math error(right)");
							}
							else
							{
								word.right = position;
								word.rightStat = HwrEndpointStatus.Initialized;
							}
						}
					}
					Debug.Assert(Math.Abs(position - edgeRight) < 1, "math error(term)");
				}
				currWordI = nextWordI;
				edgeLeft = edgeRight;
			};

			for (int i = 0; i < wordEstimates.Length; i++)
			{
				if (wordEstimates[i].HwrTextWord == null)
					continue;
				if (wordEstimates[i].HwrTextWord.leftStat == HwrEndpointStatus.Calculated || wordEstimates[i].HwrTextWord.leftStat == HwrEndpointStatus.Manual)
				{
					//spread words [currWordI, i) over [edgeLeft, wordEstimates[i].HwrTextWord.left]
					distributeWord(i, wordEstimates[i].HwrTextWord.left);
				}
				if (wordEstimates[i].HwrTextWord.rightStat == HwrEndpointStatus.Calculated || wordEstimates[i].HwrTextWord.rightStat == HwrEndpointStatus.Manual)
				{
					//spread words [currWordI, i+1) over [edgeLeft, wordEstimates[i].HwrTextWord.right]
					distributeWord(i + 1, wordEstimates[i].HwrTextWord.right);
				}
			}
			distributeWord(wordEstimates.Length, right);
		}

		public XNode AsXml()
		{
			return
				new XElement("TextLine",
					new XAttribute("no", nr),
					MakeXAttrs(),
					words.Select(word => word.AsXml())
				);
		}

		public string ProcessorMessage { get; set; }
	}
}

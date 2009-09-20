using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MoreLinq;
using System.Xml.Linq;
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
		public void SetComputedCharEndpoints(int[] endpoints, double likelihood, Word.TrackStatus endpointSource) {
			//remember: don't overwrite manually set endpoints!!!!

			computedLikelihood = likelihood;
			computedCharEndpoints = endpoints;

			char[] textChars = TextWithTerminators.ToArray();
			if (textChars.Length != endpoints.Length) throw new ArgumentException(string.Format("Passed {0} endspoints for {1} characters ({0}!={1})", endpoints.Length, textChars.Length));

			int currWordI = -1; //start before the first word.

			for (int i = 0; i < textChars.Length; i++) {
				if (textChars[i] == ' ') { //found word boundary
					if (currWordI >= 0) { //then the character right before this space was the rightmost character of the previous word.
						if (words[currWordI].rightStat != Word.TrackStatus.Manual) {
							words[currWordI].right = endpoints[i - 1];
							words[currWordI].rightStat = endpointSource;
						} else {
							if (Math.Abs(words[currWordI].right - endpoints[i - 1]) > 1)
								throw new ApplicationException("calculated endpoint differs significantly from manual example!");
						}
					}
					currWordI++;//space means new word
					if (currWordI < words.Length) //then the endpos of the space must be the beginning pos of the new word.
			        {
						if (words[currWordI].leftStat != Word.TrackStatus.Manual) {
							words[currWordI].left = endpoints[i];
							words[currWordI].leftStat = endpointSource;
						} else {
							if (Math.Abs(words[currWordI].left - endpoints[i]) > 1)
								throw new ApplicationException("calculated endpoint differs significantly from manual example!");
						}
					}
				}
			}
			if (currWordI != words.Length)
				throw new ApplicationException("programmer error: currWordI(" + currWordI + ") != words.Length (" + words.Length + ")");
		}


		public TextLine() { }
		public TextLine(string text, int no, double top, double bottom, double left, double right, double shear)
			: base(top, bottom, left, right, shear) {
			this.no = no;
			this.words = text
				.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((t, i) => new Word(this, t, i + 1, top, bottom, left, left, shear))
			.ToArray();
		}

		public string FullText { get { return string.Join(" ", words.Select(w => w.text).ToArray()); } }

		public IEnumerable<char> TextWithTerminators {
			get {
				return
					new char[] { (char)0, ' ' }.Concat(FullText).Concat(new char[] { ' ', (char)10 });
			}
		}

		public IEnumerable<int> ManualEndPoints {
			get {
				return
				(-1) //startsymbol end
					.Concat(words.SelectMany(word => word.ManualEndPoints)) //endpoints of each word and its preceeding space
					.Concat(-1) //endpoint of final space
					.Concat(-1); //endpoint of endsym
			}
		}

		public void EstimateWordBoundariesViaSymbolLength(Dictionary<char, GaussianEstimate> symbolWidths) {

			GaussianEstimate
				start = symbolWidths[(char)0],
				end = symbolWidths[(char)10];

			var wordEstimates =
				new { Word = (Word)null, Length = start }
				.Concat(words.Select(word => new { Word = word, Length = word.EstimateLength(symbolWidths) }))
				.Concat(new { Word = (Word)null, Length = end })
				.ToArray();

			int currWordI = 0;
			double edgeLeft = left;

			Action<int, double> distributeWord = (nextWordI, edgeRight) => {
				if (nextWordI != currWordI) {
					//spread words [currWordI, i) over [edgeLeft, wordEstimates[i].Word.left]
					var relevantEsts = wordEstimates.Skip(currWordI).Take(nextWordI - currWordI);
					GaussianEstimate totalEstimate = relevantEsts.Select(w => w.Length).Aggregate((a, b) => a + b);
					double wordwiseStddevTotal = relevantEsts.Select(w => w.Length).Select(est => est.StdDev).Sum();

					double correctionPerStdDev = (edgeRight - edgeLeft - totalEstimate.Mean) / wordwiseStddevTotal;
					double position = edgeLeft;

					//ok, so we have a total segment length and a per word estimate
					foreach (var wordEst in relevantEsts) {
						Word word = wordEst.Word;
						if (word == null) {
							position += wordEst.Length.Mean + wordEst.Length.StdDev * correctionPerStdDev;
						} else {
							if (word.leftStat > Word.TrackStatus.Initialized) {
								Debug.Assert(Math.Abs(position - word.left) < 1, "math error(left)");
							} else {
								word.left = position;
								word.leftStat = Word.TrackStatus.Initialized;
							}
							position += wordEst.Length.Mean + wordEst.Length.StdDev * correctionPerStdDev;
							if (word.rightStat > Word.TrackStatus.Initialized) {
								Debug.Assert(Math.Abs(position - word.right) < 1, "math error(right)");
							} else {
								word.right = position;
								word.rightStat = Word.TrackStatus.Initialized;
							}
						}
					}
					Debug.Assert(Math.Abs(position - edgeRight) < 1, "math error(term)");
				}
				currWordI = nextWordI;
				edgeLeft = edgeRight;
			};

			for (int i = 0; i < wordEstimates.Length; i++) {
				if (wordEstimates[i].Word == null)
					continue;
				if (wordEstimates[i].Word.leftStat == Word.TrackStatus.Calculated || wordEstimates[i].Word.leftStat == Word.TrackStatus.Manual) {
					//spread words [currWordI, i) over [edgeLeft, wordEstimates[i].Word.left]
					distributeWord(i, wordEstimates[i].Word.left);
				}
				if (wordEstimates[i].Word.rightStat == Word.TrackStatus.Calculated || wordEstimates[i].Word.rightStat == Word.TrackStatus.Manual) {
					//spread words [currWordI, i+1) over [edgeLeft, wordEstimates[i].Word.right]
					distributeWord(i + 1, wordEstimates[i].Word.right);
				}
			}
			distributeWord(wordEstimates.Length, this.right);
		}

		public TextLine(XElement fromXml, Word.TrackStatus wordSource)
			: base(fromXml) {
			no = (int)fromXml.Attribute("no");
			words = fromXml.Elements("Word").Select(xmlWord => new Word(this, xmlWord, wordSource)).ToArray();
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

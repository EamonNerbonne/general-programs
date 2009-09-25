//#define LOG_OVERFLOWS

using System.IO;
using System.Linq;
using MoreLinq;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HwrDataModel
{
	public class HwrTextPage : IAsXml
	{
		public readonly string name;
		public readonly int pageNum;
		public readonly HwrTextLine[] textlines;//textlines can be relayouted post-construction, but the actual line content and number of lines cannot be changed.

		public HwrTextPage(FileInfo file, HwrEndpointStatus wordSource) : this(LoadXDoc(file).Root, wordSource) { }
		private static XDocument LoadXDoc(FileInfo file) {
			using (Stream stream = file.OpenRead())
			using (XmlReader xmlreader = XmlReader.Create(stream))
				return XDocument.Load(xmlreader);
		}
		public HwrTextPage(XElement fromXml, HwrEndpointStatus wordSource) {
			name = (string)fromXml.Attribute("name");
			pageNum = int.Parse(name.Substring(name.Length - 4, 4));
			textlines = fromXml.Elements("HwrTextLine").Select(xmlTextLine => new HwrTextLine(this, xmlTextLine, wordSource)).ToArray();
		}
		public HwrTextPage(string name, int pageNum, Func<HwrTextPage, HwrTextLine[]> textlinesConstructor) { this.name = name; this.pageNum = pageNum; this.textlines = textlinesConstructor(this); }

		public XNode AsXml() {
			return new XElement("Image",
				new XAttribute("name", name),
				textlines.Select(textline => textline.AsXml())
				);
		}

		public struct WordPair
		{
			public HwrTextWord MyWord, Other;
		}

		public IEnumerable<WordPair> MatchWordPairs(HwrTextPage other) {

			if (other == null)
				return Enumerable.Empty<WordPair>();
			return
				from line in textlines
				let otherLines = other.textlines.Where(trainline => trainline.ContainsPoint(line.CenterPoint) && line.ContainsPoint(trainline.CenterPoint)).ToArray()
				where otherLines.Length == 1 //TODO: add error for multiple matching lines.
				let otherLine = otherLines[0]
				let otherWordsByText = otherLine.words.ToLookup(word => word.text)
				let myWordsByText = line.words.ToLookup(word => word.text)
				from otherWords in otherWordsByText
				let myWords = myWordsByText[otherWords.Key]
				where otherWords.Count() == myWords.Count()
				from pair in MoreEnumerable.Zip(otherWords, myWords, (otherW, myW) => new WordPair { Other = otherW, MyWord = myW })
				select pair;
		}

		public HwrTextWord FindWord(Point point) {
			return (
				from line in textlines
				where line.ContainsPoint(point)
				let lineMidpoint = line.bodyBot > 0 ? line.top + 0.5 * (line.bodyBot + line.bodyTop) : 0.5 * (line.top + line.bottom)
				let correctedX = point.X + line.XOffsetForYOffset(line.top - point.Y)
				from word in line.words
				where word.left <= correctedX && correctedX < word.right
				orderby Math.Abs(point.Y - lineMidpoint), Math.Abs(correctedX - 0.5 * (word.left + word.right))
				select word
				).FirstOrDefault();
		}

		public void SetFromManualExample(HwrTextPage handChecked) {
			if (handChecked == null)
				return;

			foreach (HwrTextLine line in textlines) {
				var trainLines = handChecked.textlines.Where(trainline => trainline.ContainsPoint(line.CenterPoint) && line.ContainsPoint(trainline.CenterPoint)).ToArray();
				HwrTextLine trainLine;
				if (trainLines.Length == 0)
					continue;
				else if (trainLines.Length > 1) {
					HashSet<string> myWords = new HashSet<string>(line.words.Select(w => w.text));
					var bestOption = trainLines.OrderByDescending(op => op.words.Where(w => myWords.Contains(w.text)).Count()).First();
					var bestOption2 = trainLines.OrderBy(op => Math.Abs(op.CenterPoint.Y - line.CenterPoint.Y)).First();

					if (bestOption != bestOption2) {
#if LOG_OVERFLOWS
						Console.WriteLine("\nMultiple Options for line @ {1}: {0}", line.FullText, line.CenterPoint);
						foreach (var option in trainLines)
							Console.WriteLine("{2}({1}):  {0}", option.FullText, option.CenterPoint, (bestOption == option ? "=" : "-") + (bestOption2 == option ? "=" : "-"));
						Console.WriteLine();
#endif
						continue;
					} else
						trainLine = bestOption;
				} else
					trainLine = trainLines[0];


				var trainWordsByText = trainLine.words.ToLookup(word => word.text);
				var myWordsByText = line.words.ToLookup(word => word.text);


				var trainPairs =
					from trainWords in trainWordsByText
					join myWords in myWordsByText on trainWords.Key equals myWords.Key
					where trainWords.Count() == myWords.Count()
					from pair in MoreEnumerable.Zip(trainWords, myWords, (trainW, myW) => new { TrainWord = trainW, MyWord = myW })
					select pair;

				foreach (var pair in trainPairs) {
					HwrTextWord myWord = pair.MyWord;
					HwrTextWord trainWord = pair.TrainWord;
					//we have a training example; now to correct for shear...
					double yShift = myWord.top - trainWord.top;
					double xShift = trainWord.XOffsetForYOffset(yShift);
					myWord.left = trainWord.left + xShift;
					myWord.right = trainWord.right + xShift;
					myWord.leftStat = myWord.rightStat = HwrEndpointStatus.Manual;
					if (myWord.left < line.left) {
						if (line.left - myWord.left < 150)
							line.left = myWord.left;
						else {
#if LOG_OVERFLOWS
							Console.WriteLine("Large X-underflow: word at [{0}, {1}) '{2}'\n in line  [{3}, {4}) '{5}', page {6}", myWord.left, myWord.right, myWord.text,
								line.left, line.right, line.FullText, line.Page.pageNum
								);
#endif
						}
					}
					if (myWord.right > line.right) {
						if (myWord.right - line.right < 150)
							line.right = myWord.right;
						else {
#if LOG_OVERFLOWS
							Console.WriteLine("Serious X-overflow: word at [{0}, {1}) '{2}'\n in line  [{3}, {4}) '{5}', page {6}", myWord.left, myWord.right, myWord.text,
									line.left, line.right, line.FullText, line.Page.pageNum
								);
#endif
						}
					}
				}
				//unfortunately some training samples violate my assumption that words are non-overlapping.
				double minX = 0;
				foreach (HwrTextWord word in line.words) {
					if (word.leftStat == HwrEndpointStatus.Manual || word.leftStat == HwrEndpointStatus.Calculated) {
						if (word.left < minX)
							word.leftStat = HwrEndpointStatus.Calculated; //don't regard as absolute truth.
						else
							minX = word.left;
					}

					if (word.rightStat == HwrEndpointStatus.Manual || word.rightStat == HwrEndpointStatus.Calculated) {
						if (word.right < minX)
							word.rightStat = HwrEndpointStatus.Calculated;
						else
							minX = word.right;
					}
				}
				double maxX = double.MaxValue;
				foreach (HwrTextWord word in line.words.Reverse()) {
					if (word.rightStat == HwrEndpointStatus.Manual || word.rightStat == HwrEndpointStatus.Calculated) {
						if (word.right > maxX)
							word.rightStat = HwrEndpointStatus.Calculated;
						else
							maxX = word.right;
					}

					if (word.leftStat == HwrEndpointStatus.Manual || word.leftStat == HwrEndpointStatus.Calculated) {
						if (word.left > maxX)
							word.leftStat = HwrEndpointStatus.Calculated; //don't regard as absolute truth.
						else
							maxX = word.left;
					}
				}
			}
		}

		public void EstimateWordBoundariesViaSymbolLength(Dictionary<char, GaussianEstimate> symbolWidths) {
			foreach (var line in textlines)
				line.EstimateWordBoundariesViaSymbolLength(symbolWidths);
		}
	}
}

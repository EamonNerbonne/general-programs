﻿using System.IO;
using System.Linq;
using MoreLinq;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HwrDataModel
{

	public class WordsImage : IAsXml
	{
		public readonly string name;
		public readonly int pageNum;
		public readonly TextLine[] textlines;//textlines can be relayouted post-construction, but the actual line content and number of lines cannot be changed.

		public WordsImage(FileInfo file) : this(LoadXDoc(file).Root) { }
		private static XDocument LoadXDoc(FileInfo file) {
			using (Stream stream = file.OpenRead())
			using (XmlReader xmlreader = XmlReader.Create(stream))
				return XDocument.Load(xmlreader);
		}
		public WordsImage(XElement fromXml) {
			name = (string)fromXml.Attribute("name");
			pageNum = int.Parse(name.Substring(name.Length - 4, 4));
			textlines = fromXml.Elements("TextLine").Select(xmlTextLine => new TextLine(xmlTextLine)).ToArray();
		}
		public WordsImage(string name, int pageNum, TextLine[] textlines) { this.name = name; this.pageNum = pageNum; this.textlines = textlines; }

		public XNode AsXml() {
			return new XElement("Image",
				new XAttribute("name", name),
				textlines.Select(textline => textline.AsXml())
				);
		}

		public struct WordPair
		{
			public Word MyWord, Other;
		}

		public IEnumerable<WordPair> MatchWordPairs(WordsImage other) {
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
				from pair in otherWords.Zip(myWords, (otherW, myW) => new WordPair { Other = otherW, MyWord = myW })
				select pair;
		}

		public Word FindWord(Point point) {
			return (
				from line in textlines
				where line.ContainsPoint(point)
				let lineMidpoint = line.bodyBot > 0 ? line.top + 0.5 * (line.bodyBot + line.bodyTop) : 0.5 * (line.top + line.bottom)
				orderby Math.Abs(point.Y - lineMidpoint)
				let correctedX = point.X + line.XOffsetForYOffset(line.top - point.Y)
				from word in line.words
				where word.left <= correctedX && correctedX < word.right
				orderby Math.Abs(correctedX - 0.5 * (word.left + word.right))
				select word
				).FirstOrDefault();
		}


		public void SetFromManualExample(WordsImage handChecked) {
			if (handChecked == null)
				return;

			foreach (TextLine line in textlines) {
				var trainLines = handChecked.textlines.Where(trainline => trainline.ContainsPoint(line.CenterPoint) && line.ContainsPoint(trainline.CenterPoint)).ToArray();
				if (trainLines.Length == 0)
					continue;
				else if (trainLines.Length > 1) {
					Console.WriteLine("\nMultiple Options for line @ {1}: {0}", line.FullText, line.CenterPoint);
					foreach (var option in trainLines)
						Console.WriteLine("--({1}):  {0}", option.FullText, option.CenterPoint);
					Console.WriteLine();
					continue;
				}

				var trainLine = trainLines[0];


				var trainWordsByText = trainLine.words.ToLookup(word => word.text);
				var myWordsByText = line.words.ToLookup(word => word.text);


				var trainPairs =
					from trainWords in trainWordsByText
					join myWords in myWordsByText on trainWords.Key equals myWords.Key
					where trainWords.Count() == myWords.Count()
					from pair in trainWords.Zip(myWords, (trainW, myW) => new { TrainWord = trainW, MyWord = myW })
					select pair;

				foreach (var pair in trainPairs) {
					Word myWord = pair.MyWord;
					Word trainWord = pair.TrainWord;
					//we have a training example; now to correct for shear...
					double yShift = myWord.top - trainWord.top;
					double xShift = trainWord.XOffsetForYOffset(yShift);
					myWord.left = trainWord.left + xShift;
					myWord.right = trainWord.right + xShift;
					myWord.leftStat = myWord.rightStat = Word.TrackStatus.Manual;
					if (myWord.left < line.left) {
						Console.WriteLine("X-underflow: word at [{0}, {1}) '{2}'\n in line  [{3}, {4}) '{5}'", myWord.left, myWord.right, myWord.text,
							line.left, line.right, line.FullText
							);
						line.left = myWord.left;
					}
					if (myWord.right > line.right) {
						Console.WriteLine("X-overflow: word at [{0}, {1}) '{2}'\n in line  [{3}, {4}) '{5}'", myWord.left, myWord.right, myWord.text,
							line.left, line.right, line.FullText
							);
						line.right = myWord.right;
					}
				}
				//unfortunately some training samples violate my assumption that words are non-overlapping.
				double minX = 0;
				foreach (Word word in line.words) {
					if (word.leftStat == Word.TrackStatus.Manual || word.leftStat == Word.TrackStatus.Calculated) {
						if (word.left < minX)
							word.leftStat = Word.TrackStatus.Calculated; //don't regard as absolute truth.
						else
							minX = word.left;
					}

					if (word.rightStat == Word.TrackStatus.Manual || word.rightStat == Word.TrackStatus.Calculated) {
						if (word.right < minX)
							word.rightStat = Word.TrackStatus.Calculated;
						else
							minX = word.right;
					}
				}
				double maxX = double.MaxValue;
				foreach (Word word in line.words.Reverse()) {
					if (word.rightStat == Word.TrackStatus.Manual || word.rightStat == Word.TrackStatus.Calculated) {
						if (word.right > maxX)
							word.rightStat = Word.TrackStatus.Calculated;
						else
							maxX = word.right;
					}

					if (word.leftStat == Word.TrackStatus.Manual || word.leftStat == Word.TrackStatus.Calculated) {
						if (word.left > maxX)
							word.leftStat = Word.TrackStatus.Calculated; //don't regard as absolute truth.
						else
							maxX = word.left;
					}


				}
			}
		}

		public void MarkIfManual(WordsImage handChecked) {
			throw new NotImplementedException();
		}
	}
}

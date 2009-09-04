using System.IO;
using System.Linq;
using MoreLinq;
using System.Xml;
using System.Xml.Linq;
using System;
using System.Collections.Generic;

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


		public void SetFromTrainingExample(WordsImage handChecked) {
			if (handChecked == null)
				return;
			var trainLines = handChecked.textlines.ToDictionary(line => line.no);

			foreach (TextLine line in textlines) {
				if (!trainLines.ContainsKey(line.no))
					continue;
				TextLine trainLine = trainLines[line.no];
				if (!line.ContainsPoint(trainLine.CenterPoint)) {
					Console.WriteLine("text/train lines don't match! Page:{0}\n--annot_line={1}\n--train_line={2}", this.pageNum, line.FullText, trainLine.FullText);
					continue;
				}

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
				}
			}
		}
	}
}

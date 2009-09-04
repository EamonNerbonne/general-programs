using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace HwrDataModel
{

	public class WordsImage : IAsXml
	{
		public readonly string name;
		public readonly int pageNum;
		public readonly TextLine[] textlines;//textlines can be relayouted post-construction, but the actual line content and number of lines cannot be changed.
		
		public WordsImage(FileInfo file) : this(LoadXDoc(file).Root) { }
		private static XDocument LoadXDoc(FileInfo file)
		{
			using (Stream stream = file.OpenRead())
			using (XmlReader xmlreader = XmlReader.Create(stream))
				return XDocument.Load(xmlreader);
		}
		public WordsImage(XElement fromXml)
		{
			name = (string)fromXml.Attribute("name");
			pageNum = int.Parse(name.Substring(name.Length - 4, 4));
			textlines = fromXml.Elements("TextLine").Select(xmlTextLine => new TextLine(xmlTextLine)).ToArray();
		}
		public WordsImage(string name, int pageNum, TextLine[] textlines) { this.name = name; this.pageNum = pageNum; this.textlines = textlines; }

		public XNode AsXml()
		{
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
				var trainWords = trainLine.words.ToLookup(word => word.text);
				foreach (Word word in line.words) {
					if (!trainWords.Contains(word.text))
						continue;
					Word trainWord = trainWords[word.text].SingleOrDefault(possibletrainword => word.ContainsPoint(possibletrainword.CenterPoint));
					if (trainWord != null){

						//we have a training example; now to correct for shear...
						double yShift = word.top - trainWord.top;
						double xShift = trainWord.XOffsetForYOffset(yShift);
						word.left = trainWord.left + xShift;
						word.right = trainWord.right + xShift;
						word.leftStat = word.rightStat = Word.TrackStatus.Manual;
					}
				}
			}
		}
	}
}

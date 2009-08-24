using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DataIO
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

	}
}

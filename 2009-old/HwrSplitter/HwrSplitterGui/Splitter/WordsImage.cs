using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DataIO
{

    public class WordsImage : IAsXml
    {
        public string name;
        public int pageNum;
        public TextLine[] textlines;
        public WordsImage() { }

        public WordsImage(XElement fromXml) {
            Init(fromXml);
        }
        public WordsImage(FileInfo file) {
            using (Stream stream = file.OpenRead())
            using (XmlReader xmlreader = XmlReader.Create(stream))
                Init(XDocument.Load(xmlreader).Root);

        }

        private void Init(XElement fromXml) {
            name = (string)fromXml.Attribute("name");
            pageNum = int.Parse(name.Substring(name.Length - 4, 4));
            textlines = fromXml.Elements("TextLine").Select(xmlTextLine => new TextLine(xmlTextLine)).ToArray();

        }
        public XNode AsXml() {
            return new XElement("Image",
                new XAttribute("name", name),
                textlines.Select(textline => textline.AsXml())
                );
        }

    }
}

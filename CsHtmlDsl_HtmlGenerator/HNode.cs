using System.IO;
using System.Xml;

namespace HtmlGenerator
{
	public abstract class HNode
	{
		public static implicit operator HNode(string s) { return new HText(s); }
		public static implicit operator HNode(int i) { return new HText(i.ToString()); }

		public abstract void WriteToString(TextWriter writer, bool indent, int level);

		public abstract void WriteToXml(XmlWriter xw);
	}
}
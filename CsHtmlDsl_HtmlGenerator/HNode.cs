using System.IO;
using System.Xml;

namespace HtmlGenerator
{
	public abstract class HNodeContent
	{
		internal HNodeContent() { }
		public static implicit operator HNodeContent(string s) { return new HText(s); }
		public static implicit operator HNodeContent(int i) { return new HText(i.ToString()); }
	}

	public abstract class HNode : HNodeContent
	{
		public static implicit operator HNode(string s) { return new HText(s); }
		public static implicit operator HNode(int i) { return new HText(i.ToString()); }

		public abstract void WriteToString(TextWriter writer, bool indent, int level);

		public abstract void WriteToXml(XmlWriter xw);
	}
}
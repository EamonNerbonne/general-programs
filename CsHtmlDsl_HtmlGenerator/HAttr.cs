using System.IO;
using System.Web;
using System.Xml;

namespace HtmlGenerator
{
	public sealed class HAttr
	{
		public readonly string Name, Value;
		internal HAttr(string name, string value) { Name = name; Value = value; }

		public void WriteToString(TextWriter writer)
		{
			writer.Write(' ');
			writer.Write(Name);
			writer.Write("=\"");
			HttpUtility.HtmlEncode(Value, writer);
			writer.Write('\"');
		}

		public void WriteToXml(XmlWriter xw)
		{
			xw.WriteAttributeString(Name, Value);
		}
	}
}
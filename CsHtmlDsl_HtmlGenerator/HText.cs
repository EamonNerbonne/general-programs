using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace HtmlGenerator
{
	public class HText : HNode
	{
		public readonly string Content;
		static readonly Regex newline = new Regex(@"^", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
		public HText(string content) { Content = content; }
		public override void WriteToString(TextWriter writer, bool indent, int level)
		{
			var indentedContent = indent && level > 0 ? newline.Replace(Content, new string('\t', level)) : Content;
			HttpUtility.HtmlEncode(indentedContent, writer);
			if (indent)
				writer.Write('\n');
		}

		public override void WriteToXml(XmlWriter xw) { xw.WriteString(Content); }
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace HtmlGenerator
{
	public abstract class HNode
	{
		public static implicit operator HNode(string s) { return new TextNode(s); }
		public static implicit operator HNode(int i) { return new TextNode(i.ToString()); }

		public abstract void WriteToString(TextWriter writer, bool indent, int level);

		public abstract void WriteToXml(XmlWriter xw);
	}
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

	public sealed class HElem : HNode
	{
		public readonly string Name;
		public readonly IReadOnlyList<HAttr> Attributes;
		public readonly IReadOnlyList<HNode> Children;
		static readonly HAttr[] noAttrs = new HAttr[0];
		static readonly HNode[] noNodes = new HNode[0];
		volatile Dictionary<string, string> attributeLookup;
		internal HElem(string name, IReadOnlyList<HAttr> attributes, IReadOnlyList<HNode> children) { Name = name; Attributes = attributes ?? noAttrs; Children = children ?? noNodes; }
		public string GetAttribute(string name)
		{
			var lookup = attributeLookup ?? (attributeLookup = Attributes.ToDictionary(attr => attr.Name, attr => attr.Value, StringComparer.OrdinalIgnoreCase));
			string retval;
			return lookup.TryGetValue(name, out retval) ? retval : null;
		}

		static readonly HashSet<string> voidEls = new HashSet<string>(new[]
			{
				"area", "base", "br", "col", "command", "embed", "hr", "img", "input", "keygen", "link", "meta", "param", "source", "track", "wbr"
			}, StringComparer.OrdinalIgnoreCase);

		public override void WriteToString(TextWriter writer, bool indent, int level)
		{
			DoIndent(writer, indent, level);
			writer.Write('<');
			writer.Write(Name);
			foreach (var attr in Attributes)
				attr.WriteToString(writer);
			if (!Children.Any() && voidEls.Contains(Name))
			{
				writer.Write("/>");
			}
			else
			{
				writer.Write('>');
				if (indent)
					writer.Write('\n');
				foreach (var kid in Children)
					kid.WriteToString(writer, indent, level + 1);
				DoIndent(writer, indent, level);
				writer.Write("</");
				writer.Write(Name);
				writer.Write('>');
			}
			if (indent)
				writer.Write('\n');
		}

		public override void WriteToXml(XmlWriter xw)
		{
			xw.WriteStartElement(Name);
			foreach (var attr in Attributes)
				attr.WriteToXml(xw);
			foreach (var node in Children)
				node.WriteToXml(xw);
			xw.WriteEndElement();
		}

		static void DoIndent(TextWriter writer, bool indent, int level)
		{
			if (indent && level > 0)
			{
				writer.Write(new string('\t', level));
			}
		}
	}
	public class TextNode : HNode
	{
		public readonly string Content;
		static readonly Regex newline = new Regex(@"^", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline);
		public TextNode(string content) { Content = content; }
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
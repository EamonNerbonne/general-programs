using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace HtmlGenerator
{
	public sealed class HElem : HNode
	{
		public readonly string Name;
		public readonly IReadOnlyList<HAttr> Attributes;
		public readonly IReadOnlyList<HNode> Children;


		static readonly HAttr[] noAttrs = new HAttr[0];
		static readonly HNode[] noNodes = new HNode[0];
		volatile Dictionary<string, string> attributeLookup;
		public HElem(string name, IReadOnlyList<HAttr> attributes, IReadOnlyList<HNode> children) { Name = name; Attributes = attributes ?? noAttrs; Children = children ?? noNodes; }
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
}
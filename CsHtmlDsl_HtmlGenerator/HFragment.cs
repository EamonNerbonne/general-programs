using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace HtmlGenerator
{
	public sealed class HFragment : HNodeContent
	{
		public readonly IReadOnlyList<HNode> Nodes;
		public HFragment(IReadOnlyList<HNode> nodes) { Nodes = nodes; }




		//public abstract void WriteToString(TextWriter writer, bool indent, int level);
		public override void WriteToString(TextWriter sw, bool indent, int i)
		{
			foreach (var kid in Nodes)
				kid.WriteToString(sw, indent, i);
		}

		public override void WriteToXml(XmlWriter xw)
		{
			foreach (var kid in Nodes)
				kid.WriteToXml(xw);
		}

		public IEnumerable<XNode> SerializeAsXml()
		{
			var temp = new XElement("temp");
			using (var xw = temp.CreateWriter())
				WriteToXml(xw);
			var retval = temp.Nodes().ToArray();
			temp.RemoveNodes();
			return retval;
		}

		public static BuildHFragment New { get { return new BuildHFragment(); } }
	}
}
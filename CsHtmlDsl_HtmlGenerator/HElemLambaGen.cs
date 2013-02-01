using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlGenerator
{
	struct HElemLambaGen
	{
		internal string Name;
		internal SList<HAttr> Attrs;
		internal SList<HNode> Kids;

		public HElemLambaGen Append(HNode node) { return new HElemLambaGen { Name = Name, Attrs = Attrs, Kids = Kids.Prepend(node) }; }
		public HElemLambaGen Attr(HAttr attr) { return new HElemLambaGen { Name = Name, Attrs = Attrs.Prepend(attr), Kids = Kids }; }
		public static HElemLambaGen Create(string name) { return new HElemLambaGen { Name = name }; }
		public HElem Finish() { return new HElem(Name, Attrs.ToArray(), Kids.ToArray()); }
	}



}

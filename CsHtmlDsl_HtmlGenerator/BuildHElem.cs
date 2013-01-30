using System.Runtime.CompilerServices;

namespace HtmlGenerator
{
	public sealed class BuildHElem<TParent> : ChildFactory<BuildHElem<TParent>> where TParent : INodeBuilder<TParent>
	{
		readonly string name;
		readonly TParent parent;
		readonly SList<HAttr> attrs;
		readonly SList<HNode> children;

		internal BuildHElem(TParent parent, string name, SList<HAttr> attrs, SList<HNode> children) { this.name = name; this.attrs = attrs; this.children = children; this.parent = parent; }


		public TParent End { get { return parent[new HElem(name, attrs.ToArray(), children.ToArray())]; } }

		public BuildHElem<TParent> CustomAttribute(string attr, string value) { return new BuildHElem<TParent>(parent, name, attrs.Prepend(new HAttr(attr, value)), children); }

		BuildHElem<TParent> AttrHelper(string value, [CallerMemberName] string attr = null) { return CustomAttribute(attr.Substring(1), value); }

		public BuildHElem<TParent> _id(string value) { return AttrHelper(value); }
		public BuildHElem<TParent> _class(string value) { return AttrHelper(value); }
		public BuildHElem<TParent> _method(string value) { return AttrHelper(value); }


		public override BuildHElem<TParent> this[HNode node] { get { return new BuildHElem<TParent>(parent, name, attrs, children.Prepend(node)); } }
	}
}
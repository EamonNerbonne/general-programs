using System.Runtime.CompilerServices;

namespace HtmlGenerator
{
	public sealed class BuildHElem<TParent, TContext> : ChildFactory<BuildHElem<TParent, TContext>, TParent, TContext, HElem>
		where TContext : struct, IBuilderContext<HElem, TParent>
	{
		readonly string name;
		readonly SList<HAttr> attrs;
		readonly SList<HNode> children;

		internal BuildHElem(TContext context, string name, SList<HAttr> attrs, SList<HNode> children) : base(context) { this.name = name; this.attrs = attrs; this.children = children; }


		//TParent End { get { return parent[Finish()]; } }

		internal override HElem Finish() { return new HElem(name, attrs.ToArray(), children.ToArray()); }

		public BuildHElem<TParent, TContext> CustomAttribute(string attr, string value) { return new BuildHElem<TParent, TContext>(context, name, attrs.Prepend(new HAttr(attr, value)), children); }

		BuildHElem<TParent, TContext> AttrHelper(string value, [CallerMemberName] string attr = null) { return CustomAttribute(attr.Substring(1), value); }

		public BuildHElem<TParent, TContext> _id(string value) { return AttrHelper(value); }
		public BuildHElem<TParent, TContext> _class(string value) { return AttrHelper(value); }
		public BuildHElem<TParent, TContext> _method(string value) { return AttrHelper(value); }


		public override BuildHElem<TParent, TContext> this[HNode node] { get { return new BuildHElem<TParent, TContext>(context, name, attrs, children.Prepend(node)); } }
	}


}
using System.Diagnostics.Contracts;

namespace HtmlGenerator.Base
{
	public interface IBuilderContext<TNode, TParent>
	{
		[Pure]
		TParent Complete(TNode node);
	}

	public struct HElemBuilderCompleter<TParent> : IBuilderContext<HElem, TParent> where TParent : INodeBuilder<TParent>
	{
		readonly INodeBuilder<TParent> parent;
		public HElemBuilderCompleter(INodeBuilder<TParent> parent) : this() { this.parent = parent; }

		public TParent Complete(HElem node) { return parent[node]; }
	}
	public struct HElemCompleter : IBuilderContext<HElem, HElem>
	{
		public HElem Complete(HElem node) { return node; }
	}
}
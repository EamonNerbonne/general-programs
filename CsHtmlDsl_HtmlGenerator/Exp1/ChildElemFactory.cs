using System.Linq;
using System.Runtime.CompilerServices;
using HtmlGenerator.Base;

namespace HtmlGenerator.Exp1
{
	public abstract class ChildElemFactory<TSelf, TParent, TContext, TNode> : INodeBuilder<TSelf>
		where TSelf : ChildElemFactory<TSelf, TParent, TContext, TNode>
		where TContext : struct, IBuilderContext<TNode, TParent>
	{
		internal readonly TContext context;

		protected ChildElemFactory(TContext context) { this.context = context; }

		internal abstract TNode Finish();

		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> CustomElement(string name) { return new BuildHElem<TSelf, HElemBuilderCompleter<TSelf>>(new HElemBuilderCompleter<TSelf>((TSelf)this), name, null, null); }

		BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> ElemHelper([CallerMemberName] string name = null) { return CustomElement(name); }

		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> div { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> span { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> b { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> i { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> img { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> form { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> a { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> html { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> body { get { return ElemHelper(); } }
		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> p { get { return ElemHelper(); } }

		public TSelf this[params HNodeContent[] nodes] { get { return nodes.SelectMany(node => node is HFragment ? ((HFragment)node).Nodes : new[] { (HNode)node }).Aggregate((TSelf)this, (acc, node) => acc[node]); } }

		public abstract TSelf this[HNode node] { get; }

		public TParent End { get { return context.Complete(Finish()); } }
	}
}
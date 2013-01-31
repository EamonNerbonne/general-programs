using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HtmlGenerator
{
	public interface INodeBuilder<out TSelf> where TSelf : INodeBuilder<TSelf>
	{
		TSelf this[HNode node] { get; }
	}


	public interface IBuilderContext<TNode, TParent> 
	{
		[Pure]
		TParent Complete(TNode node);
	}

	public abstract class ChildFactory<TSelf, TParent, TContext, TNode> : INodeBuilder<TSelf>
		where TSelf : ChildFactory<TSelf, TParent, TContext, TNode>
		where TContext : struct, IBuilderContext<TNode, TParent> 
	{
		internal readonly TContext context;

		protected ChildFactory(TContext context) { this.context = context; }

		internal abstract TNode Finish();

		public BuildHElem<TSelf, HElemBuilderCompleter<TSelf>> CustomElement(string name) { return new BuildHElem<TSelf, HElemBuilderCompleter<TSelf>>(new HElemBuilderCompleter<TSelf>((TSelf) this) , name, null, null); }

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

		public TSelf this[IEnumerable<HNode> nodes] { get { return nodes.Aggregate((TSelf)this, (acc, node) => acc[node]); } }

		public abstract TSelf this[HNode node] { get; }

		public TParent End { get { return context.Complete(Finish()); } }
	}

	public struct HElemBuilderCompleter<TParent> : IBuilderContext<HElem, TParent> where TParent : INodeBuilder<TParent>
	{
		readonly TParent parent;
		public HElemBuilderCompleter(TParent parent) : this() { this.parent = parent; }

		public TParent Complete(HElem node) { return parent[node]; }
	}
	public struct HElemCompleter : IBuilderContext<HElem, HElem>
	{
		public HElem Complete(HElem node) { return node; }
	}
}
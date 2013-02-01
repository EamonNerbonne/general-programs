using System;
using System.Linq.Expressions;
using HtmlGenerator.Core;

namespace HtmlGenerator.Exp3
{
	struct HElemLambaGen
	{
		string Name;
		SList<HAttr> Attrs;
		SList<HNode> Kids;

		public HElemLambaGen Append(HNode node) { return new HElemLambaGen { Name = Name, Attrs = Attrs, Kids = Kids.Prepend(node) }; }
		public HElemLambaGen Attr(HAttr attr) { return new HElemLambaGen { Name = Name, Attrs = Attrs.Prepend(attr), Kids = Kids }; }
		public static HElemLambaGen Create(string name) { return new HElemLambaGen { Name = name }; }

		public HElem Finish() { return new HElem(Name, Attrs.ToArray(), Kids.ToArray()); }
	}

	static class Builder
	{
		static class Cache<TSelf> where TSelf : BaseNodeBuilder<TSelf>
		{
			public static readonly Func<TSelf> Constructor = Expression.Lambda<Func<TSelf>>(Expression.New(typeof(TSelf))).Compile();
		}

		public static TSelf Make<TSelf>(HElemLambaGen state)
			where TSelf : BaseNodeBuilder<TSelf>
		{
			var retval = Cache<TSelf>.Constructor();
			retval.state = state;
			return retval;
		}
	}


	abstract class BaseNodeBuilder<TSelf> : INodeBuilder<TSelf>
		where TSelf : BaseNodeBuilder<TSelf>
	{
		internal BaseNodeBuilder() { }

		internal HElemLambaGen state;

		public TSelf this[HNode node] { get { return Builder.Make<TSelf>(state.Append(node)); } }

	}

	sealed class _I<TParent> where TParent : INodeBuilder<TParent>
	{
		TParent parent;
		HElemLambaGen self;


		public TParent _i { get { return parent[self.Finish()]; } }
	}
}

// ReSharper disable UnusedMember.Global
using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using HtmlGenerator.Core;

namespace HtmlGenerator.Exp3
{

	struct HElemGenContext<TParent>
	{
		public readonly string Name;
		public readonly TParent Parent;
		public HElemGenContext(TParent parent, string name) { Parent = parent; Name = name; }
	}

	struct HElemGenState<TParent>
	{
		public readonly HElemGenContext<TParent> Context;
		public readonly SList<HAttr> Attrs;
		public readonly SList<HNode> Kids;
		public HElemGenState(HElemGenContext<TParent> context, SList<HAttr> attrs, SList<HNode> kids)
		{
			Context = context;
			Attrs = attrs;
			Kids = kids;
		}

		HElemGenState<TParent> NewState(SList<HAttr> attrs, SList<HNode> kids) { return new HElemGenState<TParent>(Context, attrs, kids); }

		public HElemGenState<TParent> Append(HNode node) { return NewState(Attrs, Kids.Prepend(node)); }
		public HElemGenState<TParent> Attr(HAttr attr) { return NewState(Attrs.Prepend(attr), Kids); }
		public static HElemGenState<TParent> Create(TParent parent, string name) { return new HElemGenState<TParent>(new HElemGenContext<TParent>(parent, name), null, null); }

		public HElem FinishElem() { return new HElem(Context.Name, Attrs.ToArray(), Kids.ToArray()); }
	}

	static class Builder
	{
		public static HElemGenState<TParent> CreateContextWithin<TParent>(TParent parent, string name) { return HElemGenState<TParent>.Create(parent, name); }

		static class Cache<TSelf> where TSelf : IBaseNodeBuilder
		{
			public static readonly Func<TSelf> Constructor = Expression.Lambda<Func<TSelf>>(Expression.New(typeof(TSelf))).Compile();
		}

		public static TSelf Make<TSelf, TParent>(HElemGenState<TParent> context)
			where TSelf : BaseNodeBuilder<TSelf, TParent>
			where TParent : INodeBuilder<TParent>
		{
			var retval = Cache<TSelf>.Constructor();
			((IBaseNodeBuilder<TParent>)retval).Init(context);
			return retval;
		}
	}

	interface IBaseNodeBuilder { }

	interface IBaseNodeBuilder<TParent> : IBaseNodeBuilder
	//where TParent : INodeBuilder<TParent>
	{
		void Init(HElemGenState<TParent> initContext);
	}

	public abstract class BaseNodeBuilder<TSelf, TParent> : INodeBuilder<TSelf>, IBaseNodeBuilder<TParent>
		where TSelf : BaseNodeBuilder<TSelf, TParent>
		where TParent : INodeBuilder<TParent>
	{
		internal BaseNodeBuilder() { }
		HElemGenState<TParent> state;

		void IBaseNodeBuilder<TParent>.Init(HElemGenState<TParent> initContext) { state = initContext; }
		protected TParent Finish() { return state.Context.Parent[state.FinishElem()]; }

		protected TKid OpenTag<TKid>(string name) where TKid : BaseNodeBuilder<TKid, TSelf>
		{
			return Builder.Make<TKid, TSelf>(Builder.CreateContextWithin((TSelf)this, name));
		}

		public TSelf this[HNode node] { get { return Builder.Make<TSelf, TParent>(state.Append(node)); } }
	}

	public sealed class _I<TParent> : BaseNodeBuilder<_I<TParent>, TParent>
		where TParent : INodeBuilder<TParent>
	{


		public TParent _i { get { return Finish(); } }
	}
}

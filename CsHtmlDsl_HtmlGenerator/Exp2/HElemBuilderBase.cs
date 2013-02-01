using System.Linq;
using System.Runtime.CompilerServices;
using HtmlGenerator.Core;

namespace HtmlGenerator.Exp2
{
	internal static class BuilderHelper
	{
		internal static TSelf Init<TSelf, TContext>(this TSelf self, TContext context, string name, SList<HAttr> attrs, SList<HNode> children)
			where TSelf : IHElemBuilderInit<TContext>
		{ self.InitImpl(context, name, attrs, children); return self; }

		internal static TSelf New<TSelf, TContext>(TContext context, string name, SList<HAttr> attrs, SList<HNode> children)
			where TSelf : IHElemBuilderInit<TContext>, new()
		{
			var retval = new TSelf();
			retval.InitImpl(context, name, attrs, children);
			return retval;
		}
	}

	internal interface IHElemBuilderInit<TContext>
	{
		void InitImpl(TContext context, string name, SList<HAttr> attrs, SList<HNode> children);
	}

	public abstract class HElemBuilderBase<TSelf, TParent, TContext> : INodeBuilder<TSelf>, IHElemBuilderInit<TContext>
		where TSelf : HElemBuilderBase<TSelf, TParent, TContext>, new()
		where TContext : struct, IBuilderContext<HElem, TParent>
	{
		TContext context;
		string name;
		SList<HAttr> attrs;
		SList<HNode> children;

		public HElemBuilderBase() { }
		void IHElemBuilderInit<TContext>.InitImpl(TContext context, string name, SList<HAttr> attrs, SList<HNode> children) { this.context = context; this.name = name; this.attrs = attrs; this.children = children; }

		public TSelf this[params HNodeContent[] nodes] { get { return (TSelf)nodes.SelectMany(node => node is HFragment ? ((HFragment)node).Nodes : new[] { (HNode)node }).Aggregate((INodeBuilder<TSelf>)this, (acc, node) => acc[node]); } }

		public TParent End { get { return context.Complete(Finish()); } }
		HElem Finish() { return new HElem(name, attrs.ToArray(), children.ToArray()); }


		internal TChild Kid<TChild>([CallerMemberName]string childName = null)
			where TChild : HElemBuilderBase<TChild, TSelf, HElemBuilderCompleter<TSelf>>, new() { return new TChild().Init(new HElemBuilderCompleter<TSelf>(this), childName, null, null); }


		public HElemBuilderGeneral<TSelf, HElemBuilderCompleter<TSelf>> CustomElement(string childName) { return Kid<HElemBuilderGeneral<TSelf, HElemBuilderCompleter<TSelf>>>(childName); }

		public TSelf CustomAttribute(string attr, string value) { return new TSelf().Init(context, name, attrs.Prepend(new HAttr(attr, value)), children); }

		internal TSelf AttrHelper(string value, [CallerMemberName] string attr = null) { return CustomAttribute(attr.Substring(1), value); }

		public TSelf _id(string val) { return AttrHelper(val); }
		public TSelf _class(string val) { return AttrHelper(val); }
		public TSelf _title(string val) { return AttrHelper(val); }
		public TSelf _tabindex(string val) { return AttrHelper(val); }
		public TSelf _accesskey(string val) { return AttrHelper(val); }
		public TSelf _data(string datakey, string val) { return CustomAttribute("data-" + datakey, val); }

		public UncommonGlobalAttributes _RareAttr { get { return new UncommonGlobalAttributes(this); } }

		public struct UncommonGlobalAttributes
		{
			readonly HElemBuilderBase<TSelf, TParent, TContext> wrapper;
			public UncommonGlobalAttributes(HElemBuilderBase<TSelf, TParent, TContext> wrapper) : this() { this.wrapper = wrapper; }
			internal TSelf AttrHelper(string value, [CallerMemberName] string attr = null) { return wrapper.CustomAttribute(attr.Substring(1), value); }

			public TSelf _accesskey(string val) { return AttrHelper(val); }
			public TSelf _class(string val) { return AttrHelper(val); }
			public TSelf _contenteditable(string val) { return AttrHelper(val); }
			public TSelf _contextmenu(string val) { return AttrHelper(val); }
			public TSelf _dir(string val) { return AttrHelper(val); }
			public TSelf _draggable(string val) { return AttrHelper(val); }
			public TSelf _dropzone(string val) { return AttrHelper(val); }
			public TSelf _hidden(string val) { return AttrHelper(val); }
			public TSelf _id(string val) { return AttrHelper(val); }
			public TSelf _lang(string val) { return AttrHelper(val); }
			public TSelf _spellcheck(string val) { return AttrHelper(val); }
			public TSelf _style(string val) { return AttrHelper(val); }
			public TSelf _tabindex(string val) { return AttrHelper(val); }
			public TSelf _title(string val) { return AttrHelper(val); }
			public TSelf _translate(string val) { return AttrHelper(val); }
		}

		public TSelf this[HNode node]
		{
			get
			{
				var retval = new TSelf();
				retval.Init(context, name, attrs, children.Prepend(node));
				return retval;
			}
		}
	}

	public sealed class HElemBuilderGeneral<TParent, TContext> : HElemBuilderBase<HElemBuilderGeneral<TParent, TContext>, TParent, TContext>
		where TContext : struct, IBuilderContext<HElem, TParent>
	{

	}

	public sealed class HElemBuilder_text<TParent, TContext> : HElemBuilderBase<HElemBuilder_text<TParent, TContext>, TParent, TContext>
		where TContext : struct, IBuilderContext<HElem, TParent>
	{

	}

	public sealed class HElemBuilder_html<TParent, TContext> : HElemBuilderBase<HElemBuilder_html<TParent, TContext>, TParent, TContext>
		where TContext : struct, IBuilderContext<HElem, TParent>
	{
		public HElemBuilder_metadata<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>> head { get { return Kid<HElemBuilder_metadata<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>> body { get { return Kid<HElemBuilder_flow<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>>>(); } }
	}

	//public sealed class HElemBuilder_html2<TParent, TContext> : HElemBuilderBase<HElemBuilder_html2<TParent, TContext>, TParent, TContext>
	//where TContext : struct, IBuilderContext<HElem, TParent>
	//{
	//	public HElemBuilder_metadata<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>> head { get { return Kid<HElemBuilder_metadata<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>>>(); } }
	//	public HElemBuilder_flow<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>> body { get { return Kid<HElemBuilder_flow<HElemBuilder_html<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_html<TParent, TContext>>>>(); } }
	//}


	public sealed class HElemBuilder_metadata<TParent, TContext> : HElemBuilderBase<HElemBuilder_metadata<TParent, TContext>, TParent, TContext>
	where TContext : struct, IBuilderContext<HElem, TParent>
	{
				public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> Base { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> command { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> link { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> meta { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> noscript { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> script { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> style { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }
		public HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>> title { get { return Kid<HElemBuilder_text<HElemBuilder_metadata<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_metadata<TParent, TContext>>>>(); } }

	}


	public sealed class HElemBuilder_body<TParent, TContext> : HElemBuilderBase<HElemBuilder_body<TParent, TContext>, TParent, TContext>
where TContext : struct, IBuilderContext<HElem, TParent>
	{

	}

	public sealed class HElemBuilder_phrasing<TParent, TContext> : HElemBuilderBase<HElemBuilder_phrasing<TParent, TContext>, TParent, TContext>
where TContext : struct, IBuilderContext<HElem, TParent>
	{

	}

	public sealed class HElemBuilder_flow<TParent, TContext> : HElemBuilderBase<HElemBuilder_flow<TParent, TContext>, TParent, TContext>
where TContext : struct, IBuilderContext<HElem, TParent>
	{
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> a { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> abbr { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> address { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> article { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> aside { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> audio { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> b { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> bdo { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> blockquote { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> br { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> button { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> canvas { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> cite { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> code { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> command { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> datalist { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> del { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> details { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> dfn { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> div { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> dl { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> em { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> embed { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> fieldset { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> figure { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> footer { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> form { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h1 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h2 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h3 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h4 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h5 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> h6 { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> header { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> hgroup { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> hr { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> i { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> iframe { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> img { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> input { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> ins { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> kbd { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> keygen { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> label { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> map { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> mark { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> math { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> menu { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> meter { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> nav { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> noscript { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> Object { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> ol { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> output { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> p { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> pre { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> progress { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> q { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> ruby { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> samp { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> script { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> section { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> select { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> small { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> span { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> strong { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> sub { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> sup { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> svg { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> table { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> textarea { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> time { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> ul { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> var { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> video { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> wbr { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> area { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> link { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> meta { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
		public HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>> style { get { return Kid<HElemBuilder_flow<HElemBuilder_flow<TParent, TContext>, HElemBuilderCompleter<HElemBuilder_flow<TParent, TContext>>>>(); } }
	}

	//global attrs : 
#if false
    accesskey
    class
    contenteditable
    contextmenu
    dir
    draggable
    dropzone
    hidden
    id
    lang
    spellcheck
    style
    tabindex
    title
    translate

	events:
	------------------------------------------
	    onabort
    onblur*
    oncancel
    oncanplay
    oncanplaythrough
    onchange
    onclick
    onclose
    oncontextmenu
    oncuechange
    ondblclick
    ondrag
    ondragend
    ondragenter
    ondragleave
    ondragover
    ondragstart
    ondrop
    ondurationchange
    onemptied
    onended
    onerror*
    onfocus*
    oninput
    oninvalid
    onkeydown
    onkeypress
    onkeyup
    onload*
    onloadeddata
    onloadedmetadata
    onloadstart
    onmousedown
    onmousemove
    onmouseout
    onmouseover
    onmouseup
    onmousewheel
    onpause
    onplay
    onplaying
    onprogress
    onratechange
    onreset
    onscroll*
    onseeked
    onseeking
    onselect
    onshow
    onstalled
    onsubmit
    onsuspend
    ontimeupdate
    onvolumechange
    onwaiting


#endif
}

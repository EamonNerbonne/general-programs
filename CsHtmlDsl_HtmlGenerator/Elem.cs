using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HtmlGenerator
{
	public interface INodeContainer { }
	public interface INodeContent { }


	public class TextNode : INodeContent
	{
		public readonly string Content;

		public TextNode(string content) { Content = content; }
		public static implicit operator TextNode(string s) { return new TextNode(s); }
		public static implicit operator TextNode(int i) { return new TextNode(i.ToString()); }
	}

	public class XYZ : NullReferenceException
	{
	}

	public class Wrapper<TSelf> where TSelf : Wrapper<TSelf>, INodeContainer
	{
		public Elem<TSelf> Elem(string name) { return new Elem<TSelf>((TSelf)this); }

		Elem<TSelf> ElemHelper([CallerMemberName]string name = null) { return Elem(name); }

		public Elem<TSelf> div { get { return ElemHelper(); } }
		public Elem<TSelf> span { get { return ElemHelper(); } }
		public Elem<TSelf> b { get { return ElemHelper(); } }
		public Elem<TSelf> i { get { return ElemHelper(); } }
		public Elem<TSelf> form { get { return ElemHelper(); } }
		public Elem<TSelf> a { get { return ElemHelper(); } }
		public Elem<TSelf> html { get { return ElemHelper(); } }
		public Elem<TSelf> body { get { return ElemHelper(); } }
		public Elem<TSelf> p { get { return ElemHelper(); } }

		public TSelf this[INodeContent content] { get { return (TSelf)this; } }
		public TSelf this[TextNode content] { get { return (TSelf)this; } }
	}


	public class Fragment : Wrapper<Fragment>, INodeContainer, INodeContent
	{
		public static Fragment New { get { return new Fragment(); } }
	}

	public class Elem<TParent> : Wrapper<Elem<TParent>>, INodeContainer, INodeContent where TParent : INodeContainer
	{
		readonly TParent parent;

		public Elem(TParent parent) { this.parent = parent; }

		public Elem<TParent> _id(string id) { return this; }
		public Elem<TParent> _class(string classname) { return this; }

		public TParent End { get { return parent; } }

		public Elem<TParent> Attr(string asdf, string s)
		{
			return this;
		}
	}

	public abstract class Helper
	{
		public static string MyElem = "";
	}

	public sealed class Experiment : Helper
	{
		public static void No1()
		{
			var fragment = Fragment.New.b["is bold"].End;

			Fragment doc = Fragment.New
				.html
					.body
						.div["asda asdf"]
							.span._id("someid")._class("asdasd")[
								"asd"
								].End
							.b.i["asdfasdf"].End.End
							[fragment]
						.End
					.End
				.End;

			var doc1 = Fragment.New
				.p._class("par")["text"]
				.span["meer text"]
				["text"][doc];

			var e = MyElem;

			var bla = Fragment.New.div._id("asdf").b["test"].End
				;



			/*
			 * html [ 
			 *		body [ 
			 *			div.id("myid") [
			 *				"asda asdf" + span["asd"]
			 *			]
			 *		]
			 *	]
			 *	- Compile time checking
			 *	- intellisense (but no list of all options)
			 *	- Requires base class for elem names to be in scope
			*/
			/*
			 * Fragment.html()
			 *		.body()
			 *			.div.id("myid").class("asd")()
			 *					[
			 *						"asda asdf"
			 *					].span()["asd"].End
			 *			.End
			 *		.End
			 *	.End
			 * OR
			 * Fragment.html
			 *		.body
			 *			.div._id("myid")
			 *					["asda asdf"]
			 *					.span("asd")
			 *			.End
			 *		.End
			 *	.End
			 * OR
			 * Fragment.html
			 *		.body
			 *			.div._id["myid"]
			 *					["asda asdf"]
			 *					.span["asd"]()
			 *			()
			 *		()
			 *	()
			 *	- Compile time checking (unclosed fragments are syntactically valid, just can't be converted to string/tree)
			  * -  intellisense (with list of all options)
			*/

		}
	}

}

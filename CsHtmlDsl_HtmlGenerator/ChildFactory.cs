using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace HtmlGenerator
{
	public interface INodeBuilder<TSelf> where TSelf : INodeBuilder<TSelf>
	{
		TSelf this[HNode node] { get; }
	}


	public abstract class ChildFactory<TSelf> : INodeBuilder<TSelf>
		where TSelf : ChildFactory<TSelf>, INodeBuilder<TSelf>
	{
		public ChildBuilder<TSelf> CustomElement(string name) { return new ChildBuilder<TSelf>((TSelf)this, name, null, null); }

		ChildBuilder<TSelf> ElemHelper([CallerMemberName] string name = null) { return CustomElement(name); }

		public ChildBuilder<TSelf> div { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> span { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> b { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> i { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> form { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> a { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> html { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> body { get { return ElemHelper(); } }
		public ChildBuilder<TSelf> p { get { return ElemHelper(); } }

		public abstract TSelf this[HNode node] { get; }
	}

	public class Fragment : ChildFactory<Fragment>
	{
		readonly SList<HNode> children;
		Fragment(SList<HNode> children) { this.children = children; }

		public static Fragment New { get { return new Fragment(null); } }

		public override Fragment this[HNode node] { get { return new Fragment(children.Prepend(node)); } }

		public string End_AsString(bool indent)
		{
			using (var sw = new StringWriter())
			{
				foreach (var kid in children.ToArray())
					kid.WriteToString(sw, indent, 0);
				sw.Flush();
				return sw.ToString();
			}
		}

		public void End_WriteToXml(XmlWriter xw)
		{
			foreach (var kid in children.ToArray())
				kid.WriteToXml(xw);
		}

		public IEnumerable<XNode> End_AsXml()
		{
			var temp = new XElement("temp");
			using (var xw = temp.CreateWriter())
				End_WriteToXml(xw);
			var retval = temp.Nodes().ToArray();
			temp.RemoveNodes();
			return retval;
		}

		public override string ToString()
		{
			return children.ToArray().Select(node => node.ToString()).ToString();
		}
	}

	public sealed class ChildBuilder<TParent> : ChildFactory<ChildBuilder<TParent>> where TParent : INodeBuilder<TParent>
	{
		readonly string name;
		readonly TParent parent;
		readonly SList<HAttr> attrs;
		readonly SList<HNode> children;

		internal ChildBuilder(TParent parent, string name, SList<HAttr> attrs, SList<HNode> children) { this.name = name; this.attrs = attrs; this.children = children; this.parent = parent; }


		public TParent End { get { return parent[new HElem(name, attrs.ToArray(), children.ToArray())]; } }

		public ChildBuilder<TParent> CustomAttribute(string attr, string value) { return new ChildBuilder<TParent>(parent, name, attrs.Prepend(new HAttr(attr, value)), children); }

		public override ChildBuilder<TParent> this[HNode node] { get { return new ChildBuilder<TParent>(parent, name, attrs, children.Prepend(node)); } }
	}


	public sealed class Experiment
	{
		public static void No1()
		{
			var fragment = Fragment.New.b["is bold"].End;

			//Fragment doc = Fragment.New
			//	.html
			//	.body
			//	.div["asda asdf"]
			//	.span._id("someid")._class("asdasd")[
			//		"asd"
			//	].End
			//	.b.i["asdfasdf"].End.End
			//	[fragment]
			//	.End
			//	.End
			//	.End;

			//var doc1 = Fragment.New
			//	.p._class("par")["text"]
			//	.span["meer text"]
			//	["text"][doc];


			//var bla = Fragment.New.div._id("asdf").b["test"].End
			//	;


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
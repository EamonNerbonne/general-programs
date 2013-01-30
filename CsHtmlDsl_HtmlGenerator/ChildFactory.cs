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
		public BuildHElem<TSelf> CustomElement(string name) { return new BuildHElem<TSelf>((TSelf)this, name, null, null); }

		BuildHElem<TSelf> ElemHelper([CallerMemberName] string name = null) { return CustomElement(name); }

		public BuildHElem<TSelf> div { get { return ElemHelper(); } }
		public BuildHElem<TSelf> span { get { return ElemHelper(); } }
		public BuildHElem<TSelf> b { get { return ElemHelper(); } }
		public BuildHElem<TSelf> i { get { return ElemHelper(); } }
		public BuildHElem<TSelf> img { get { return ElemHelper(); } }
		public BuildHElem<TSelf> form { get { return ElemHelper(); } }
		public BuildHElem<TSelf> a { get { return ElemHelper(); } }
		public BuildHElem<TSelf> html { get { return ElemHelper(); } }
		public BuildHElem<TSelf> body { get { return ElemHelper(); } }
		public BuildHElem<TSelf> p { get { return ElemHelper(); } }

		public TSelf this[IEnumerable<HNode> nodes] { get { return nodes.Aggregate((TSelf)this, (acc, node) => acc[node]); } }
		public abstract TSelf this[HNode node] { get; }
	}


	public sealed class Experiment
	{
		public static void No1()
		{

			HFragment fragment = HFragment.New.
				div._class("test \"it'")
				["test >"]
				.b["bolded"]
				.i["italic & bold\nand a multiline string"]
				.End
				.End
				.End
				["plain text"]
				.form._method("get")["nothing to see!"].End
				.img.End
				.p.End
				.div.End
				.End;

			HFragment doc1 = HFragment.New
				.p._class("par")["text"]
				.span["meer text"]
				["text"][fragment]
				.End.End
				.End;


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
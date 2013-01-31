namespace HtmlGenerator
{
	public sealed class Experiment
	{
		public static void No1()
		{

			HFragment fragment = HFragment.New.
				div._class("test \"it'")
				["test >"]
				.b["bolded"]
				.i["italic & bold\nand a multiline string"].End
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

			HElem div = HElem.New("div")["asdasd"].b["test"].End.End;

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
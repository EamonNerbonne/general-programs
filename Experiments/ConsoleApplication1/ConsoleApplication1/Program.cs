using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
namespace ConsoleApplication1
{
	class Program
	{
		const string xml = 
			@"
<components>
   <component name='a'/>
   <component name='b'/>
   <component name='c'/>
 </components>
";
		static void Main(string[] args)
		{
			const string xml =@"
<components>
   <component name='a'/>
   <component name='b'/>
   <component name='c'/>
 </components>
";
			foreach (XElement component in XElement.Parse(xml).Elements() )
				component.Save(component.Attribute("name").Value + ".xml");


		}
	}
}

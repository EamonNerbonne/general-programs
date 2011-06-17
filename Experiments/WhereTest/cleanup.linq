<Query Kind="Statements">
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference Relative="..\..\EmnExtensions\bin\Release\EmnExtensions.dll">D:\BigDocs\VCS\emn\programs\EmnExtensions\bin\Release\EmnExtensions.dll</Reference>
  <Reference Relative="..\..\..\..\ExpressionToCode\ExpressionToCodeLib\bin\Release\ExpressionToCodeLib.dll">D:\BigDocs\VCS\ExpressionToCode\ExpressionToCodeLib\bin\Release\ExpressionToCodeLib.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>EmnExtensions</Namespace>
  <Namespace>EmnExtensions.Algorithms</Namespace>
  <Namespace>EmnExtensions.DebugTools</Namespace>
  <Namespace>System.Windows.Media</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>EmnExtensions.MathHelpers</Namespace>
  <Namespace>ExpressionToCodeLib</Namespace>
</Query>

var doc = XDocument.Load(@"D:\BigDocs\VCS\emn\programs\WhereTest\WhereTest\bin\Release\dumpfile.xml");
XNamespace etw = @"http://schemas.microsoft.com/win/2004/08/events/event";
var trash = doc.Descendants(etw+"Event").Where(el=>el.Elements(etw+"RenderingInfo").Elements(etw+"Message").Any(el2=>el2.Value.Contains("ReSharper"))).ToArray();
foreach(var el in trash)
	el.Remove();
	
doc.Save(@"D:\BigDocs\VCS\emn\programs\WhereTest\WhereTest\bin\Release\dumpfile.xml");
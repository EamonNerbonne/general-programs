<Query Kind="Statements">
  <Reference Relative="..\..\EmnExtensions\bin\Release\EmnExtensions.dll">E:\emn\programs\EmnExtensions\bin\Release\EmnExtensions.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>EmnExtensions</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

var text =

Regex.Replace(
Regex.Replace(File.ReadAllText(Path.GetDirectoryName( Util.CurrentQueryPath) + @"\Program.cs"),
	@"(//[^\n]*(?=\n))|(\/\*.*?\*/)","",RegexOptions.Singleline)
 , @"\s+"," ");
var bytes = Encoding.UTF8.GetBytes(text);
var ms = new MemoryStream();
var gzS = new GZipStream(ms,CompressionLevel.Optimal);
gzS.Write(bytes,0,bytes.Length);
gzS.Close();
ms.ToArray().Length.Dump();

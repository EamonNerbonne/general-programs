<Query Kind="Statements">
  <Reference>C:\VCS\emn\programs\EmnExtensions\bin\Release\EmnExtensions.dll</Reference>
  <NuGetReference>AvsAn</NuGetReference>
  <NuGetReference>ExpressionToCodeLib</NuGetReference>
  <NuGetReference>FSPowerPack.Core.Community</NuGetReference>
  <NuGetReference>morelinq</NuGetReference>
  <Namespace>AvsAnLib</Namespace>
  <Namespace>EmnExtensions</Namespace>
  <Namespace>EmnExtensions.Algorithms</Namespace>
  <Namespace>EmnExtensions.MathHelpers</Namespace>
  <Namespace>ExpressionToCodeLib</Namespace>
  <Namespace>Microsoft.FSharp.Collections</Namespace>
  <Namespace>Microsoft.FSharp.Core</Namespace>
  <Namespace>MoreLinq</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
</Query>

var dir = @"E:\video_test\";

Parallel.ForEach(Directory.GetFiles(dir,"*.wav"),new ParallelOptions{ MaxDegreeOfParallelism = 2}, f=> {
	var name = Path.GetFileNameWithoutExtension(f);
	//var oggfile = dir+name+".ogg";
	var opusfile = dir+name+".opus";
	//WinProcessUtil.ExecuteProcessSynchronously(@"C:\Utils\oggenc\oggenc2.exe", "--quiet -q 0 \""+f+"\"","");
	WinProcessUtil.ExecuteProcessSynchronously(@"C:\Utils\opustools\opusenc.exe", "--quiet --framesize 60 --bitrate 64 \""+f+"\" \""+opusfile+"\"","");
	Console.WriteLine("Finished "+name);
});
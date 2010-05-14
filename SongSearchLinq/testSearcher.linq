<Query Kind="Statements">
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\SongData\bin\Debug\taglib-sharp.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\SongData\bin\Debug\SongData.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\SongData\bin\Debug\EmnExtensions.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\SuffixTreeLib\bin\Debug\SuffixTreeLib.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>SongDataLib</Namespace>
  <Namespace>SuffixTreeLib</Namespace>
</Query>

var paths = Directory.GetFiles(@"E:\Music\","*myl*mp3",SearchOption.AllDirectories);

var mylopath=@"E:\Music\2004-08-27\Clubbers Guide to 2004 (disc 2)-10-Mylo-Paris 400.mp3";
var songdata = SongDataFactory.ConstructFromFile(new FileInfo(mylopath));

var smallDb= new SongDB(paths.Select(p=>SongDataFactory.ConstructFromFile(new FileInfo(p))));
var searchE=new SuffixTreeSongSearcher();
searchE.Init(smallDb);
var ss=SongUtil.CanonicalizedSearchStr("myles");
var myloS =SongUtil.CanonicalizedSearchStr(songdata.FullInfo);
//ss.Dump();
//myloS.Dump();
myloS.Contains(ss).Dump();
searchE.Query(ss).songIndexes.Select(i=>smallDb.songs[i].FullInfo) .Dump();


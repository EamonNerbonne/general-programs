<Query Kind="Statements">
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Release\EmnExtensions.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Release\taglib-sharp.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Release\SongData.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\Sqlite.netBin\x64\System.Data.SQLite.DLL</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Release\LastFMspider.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>LastFMspider</Namespace>
</Query>

//System.Data.SQLite.SQLiteFactory.Instance.Dump();
var tools = new LastFmTools();
var simLookup = tools.SimilarSongs;

simLookup.Lookup(SongRef.Create("the chemical brothers","galvanize")).Dump();
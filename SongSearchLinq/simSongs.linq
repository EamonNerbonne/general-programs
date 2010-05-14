<Query Kind="Statements">
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Debug\EmnExtensions.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Debug\taglib-sharp.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Debug\SongData.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Debug\System.Data.SQLite.DLL</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSpider\LastFMspider\LastFMspider\bin\Debug\LastFMspider.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Xml.Linq</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Dynamic</Namespace>
  <Namespace>LastFMspider</Namespace>
  <Namespace>LastFMspider.LastFMSQLiteBackend</Namespace>
</Query>

//System.Data.SQLite.SQLiteFactory.Instance.Dump();
var tools = new LastFmTools();
var simLookup = tools.SimilarSongs;
var db = simLookup.backingDB;
db.LookupTrack.Execute(new TrackId(1149200)).Dump();
//simLookup.Lookup(SongRef.Create("Stereo Total","Ringo, I Love you")).Dump();
//TrackId: 1149163
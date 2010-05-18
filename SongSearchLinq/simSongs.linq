<Query Kind="Statements">
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\LastFMspider\bin\Debug\EmnExtensions.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\LastFMspider\bin\Debug\taglib-sharp.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\LastFMspider\bin\Debug\SongData.dll</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\LastFMspider\bin\Debug\System.Data.SQLite.DLL</Reference>
  <Reference>D:\EamonLargeDocs\docs-trunk\programs\SongSearchLinq\LastFMspider\bin\Debug\LastFMspider.dll</Reference>
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

var songs = tools.DB.Songs
.Where(song=>song.artist =="The Beatles" && !song.SongUri.LocalPath.EndsWith(".mpc")).Dump();
//.Where(song=>song.popularity.TitlePopularity<0).Take(1000).Dump(); 
//.Where(song=>song.popularity.ArtistPopularity>350000).Dump(); 
//.Select(song=>song.popularity.TitlePopularity).Max().Dump();
//.Count(pop=>pop>200000).Dump();
//db.LookupTrack.Execute(new TrackId(1149200)).Dump();
//simLookup.LookupTopTracks("Lady Gaga").Dump();
//simLookup.Lookup(SongRef.Create("Stereo Total","Ringo, I Love you")).Dump();
//TrackId: 1149163
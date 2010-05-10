using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using SongDataLib;

namespace LastFMspider
{
	public static class LastFmDbBuilder
	{
		//we set legacy format to false for better storage efficiency
		//we set datetime format to ticks for better efficiency (internally just stored as Int64)
		//rating is stored as a REAL which is a float in C#.

		const string DataProvider = "System.Data.SQLite";
		const string DataConnectionString = "page size=4096;cache size=100000;datetimeformat=Ticks;Legacy Format=False;Synchronous=Normal;Default Timeout=300;data source=\"{0}\"";
		const string DatabaseDef = @"
PRAGMA journal_mode = PERSIST;




CREATE TABLE IF NOT EXISTS [Artist] (
  [ArtistID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [FullArtist] TEXT  NOT NULL,
  [LowercaseArtist] TEXT  NOT NULL,
  [IsAlternateOf] INTEGER NULL,
  [CurrentSimilarArtistList] INTEGER NULL,
  [CurrentTopTracksList] INTEGER NULL,
  CONSTRAINT fk_is_alt_artist FOREIGN KEY(IsAlternateOf) REFERENCES Artist(ArtistID),
  CONSTRAINT fk_cur_sal_simart FOREIGN KEY(CurrentSimilarArtistList) REFERENCES SimilarArtistList(ListID),
  CONSTRAINT fk_cur_ttl_toptracks FOREIGN KEY(CurrentTopTracksList) REFERENCES TopTracksList(ListID)
);
CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Artist_LowercaseArtist] ON [Artist]([LowercaseArtist]  ASC);





CREATE TABLE IF NOT EXISTS [SimilarArtistList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
CONSTRAINT fk_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID)
);
CREATE INDEX IF NOT EXISTS [IDX_SimilarArtistList_ArtistID_LookupTimestamp] ON [SimilarArtistList]([ArtistID]  ASC, [LookupTimestamp]  ASC);



CREATE TABLE IF NOT EXISTS [SimilarArtist] (
[SimilarArtistID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
[ListID] INTEGER  NOT NULL,
[ArtistB] INTEGER  NOT NULL,
[Rating] REAL NOT NULL,
CONSTRAINT fk_other_artist FOREIGN KEY(ArtistB) REFERENCES Artist(ArtistID),
  CONSTRAINT fk_sal_owner FOREIGN KEY(ListID) REFERENCES SimilarArtistList(ListID)
);
DROP INDEX IF EXISTS [Unique_SimilarArtist_ArtistA_ArtistB];
DROP INDEX IF EXISTS [IDX_SimilarArtist_Rating];
CREATE INDEX  IF NOT EXISTS [IDX_SimilarArtist_ListID] ON [SimilarArtist]([ListID]  ASC);






CREATE TABLE IF NOT EXISTS  [Track] (
  [TrackID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
  [ArtistID] INTEGER  NOT NULL,
  [FullTitle] TEXT  NOT NULL,
  [LowercaseTitle] TEXT  NOT NULL,
  [CurrentSimilarTrackList] INTEGER NULL,
	CONSTRAINT fk_of_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID),
	CONSTRAINT fk_cur_stl FOREIGN KEY(CurrentSimilarTrackList) REFERENCES SimilarTrackList(ListID)
);
CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Track_ArtistID_LowercaseTitle] ON [Track]([ArtistID]  ASC,  [LowercaseTitle]  ASC);
--CREATE TRIGGER IF NOT EXISTS Track_Ignore_Duplicates BEFORE INSERT ON Track
--FOR EACH ROW BEGIN 
   --INSERT OR IGNORE 
      --INTO Track (TrackID, ArtistID, FullTitle, LowercaseTitle, CurrentSimilarTrackList) 
      --VALUES (NEW.TrackID, NEW.ArtistID, NEW.FullTitle, NEW.LowercaseTitle, NEW.CurrentSimilarTrackList);
   --select RAISE(IGNORE);
--END;




CREATE TABLE IF NOT EXISTS [SimilarTrackList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[TrackID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
	CONSTRAINT fk_of_track FOREIGN KEY(TrackID) REFERENCES Track(TrackID)
);
DROP INDEX IF EXISTS [IDX_SimilarTrackList_LookupTimestamp]; 
DROP INDEX IF EXISTS [IDX_SimilarTrackList_TrackID];
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrackList_TrackID_LookupTimestamp] ON [SimilarTrackList](
  [TrackID]  ASC, 
  [LookupTimestamp]  ASC
);



CREATE TABLE IF NOT EXISTS [SimilarTrack] (
  [SimilarTrackID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
  [ListID] INTEGER  NOT NULL,
  [TrackB] INTEGER  NOT NULL,
  [Rating] REAL NOT NULL,
CONSTRAINT fk_owning_stl FOREIGN KEY(ListID) REFERENCES SimilarTrackList(ListID),
CONSTRAINT fk_other_track FOREIGN KEY(TrackB) REFERENCES Track(TrackID)
);
DROP INDEX IF EXISTS [Unique_SimilarTrack_TrackA_TrackB];
DROP INDEX IF EXISTS [IDX_SimilarTrack_TrackB];
DROP INDEX IF EXISTS [IDX_SimilarTrack_Rating];
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_ListID] ON [SimilarTrack]([ListID]  ASC);



CREATE TABLE IF NOT EXISTS [TopTracksList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
CONSTRAINT fk_owner_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID)
);
DROP INDEX IF EXISTS [IDX_TopTracksList_LookupTimestamp];
DROP INDEX IF EXISTS [IDX_TopTracksList_ArtistID];
CREATE INDEX IF NOT EXISTS [IDX_TopTracksList_ArtistID_LookupTimestamp] ON [TopTracksList](  [ArtistID]  ASC,  [LookupTimestamp]  ASC);



CREATE TABLE IF NOT EXISTS [TopTracks] (
[TopTrackID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[TrackID] INTEGER  NOT NULL,
[ListID] INTEGER  NOT NULL,
[Reach] INTEGER NOT NULL,
CONSTRAINT fk_of_ttl FOREIGN KEY(ListID) REFERENCES TopTracksList(ListID),
CONSTRAINT fk_has_track FOREIGN KEY(TrackID) REFERENCES Track(TrackID)
);
DROP INDEX IF EXISTS [Unique_TopTracks_ListID_TrackID];
DROP INDEX IF EXISTS [IDX_TopTracks_TrackID];
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_ListID] ON [TopTracks](  [ListID]  ASC);
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_Reach] ON [TopTracks](  [Reach]  DESC);

";
//CREATE TRIGGER IF NOT EXISTS Artist_Ignore_Duplicates BEFORE INSERT ON Artist
//FOR EACH ROW BEGIN 
//   INSERT OR IGNORE 
//      INTO Artist (ArtistID, FullArtist, LowercaseArtist, IsAlternateOf, CurrentSimilarArtistList, CurrentTopTracksList) 
//      VALUES (NEW.ArtistId, NEW.FullArtist, NEW.LowercaseArtist, NEW.IsAlternateOf, NEW.CurrentSimilarArtistList, NEW.CurrentTopTracksList);
//   select RAISE(IGNORE);
//END;
//CREATE TRIGGER IF NOT EXISTS Track_Ignore_Duplicates BEFORE INSERT ON Track
//FOR EACH ROW BEGIN 
//   INSERT OR IGNORE 
//      INTO Track (TrackID, ArtistID, FullTitle, LowercaseTitle, CurrentSimilarTrackList) 
//      VALUES (NEW.TrackID, NEW.ArtistID, NEW.FullTitle, NEW.LowercaseTitle, NEW.CurrentSimilarTrackList);
//   select RAISE(IGNORE);
//END;

		
		/*
CREATE TABLE IF NOT EXISTS  [Tag] (
  [TagID] INTEGER NOT NULL PRIMARY KEY,
  [LowercaseTag] TEXT  NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_Tag_LowercaseTag] ON [Tag](
  [LowercaseTag]  ASC
);

CREATE TABLE IF NOT EXISTS  [TrackTag] (
  [TrackTagID] INTEGER NOT NULL PRIMARY KEY,
  [TagID] INTEGER NOT NULL,
  [TrackID] INTEGER NOT NULL,
  [TagCount] INTEGER NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_TrackTag_TrackID_TagID] ON [TrackTag](
  [TrackID]  ASC,
  [TagID] ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_TrackTag_TagID] ON [TrackTag](
  [TagID] ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_TrackTag_TagCount] ON [TrackTag](
  [TagCount] DESC
);



CREATE TABLE IF NOT EXISTS  [Mbid] (
  [MbidID] INTEGER NOT NULL PRIMARY KEY,
  [LowercaseMbid] TEXT NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_Mbid_LowercaseMbid] ON [Mbid](
  [LowercaseMbid]  ASC
);



CREATE TABLE IF NOT EXISTS  [TrackInfo] (
  [TrackID] INTEGER NOT NULL PRIMARY KEY,
  [InfoTimestamp] INTEGER NOT NULL,
  [Listeners] INTEGER NULL,
  [Playcount] INTEGER NULL,
  [Duration] INTEGER NULL,
  [ArtistMbidID] INTEGER NULL,
  [TrackMbidID] INTEGER NULL,
  [LastFmId] INTEGER NULL
);
CREATE INDEX  IF NOT EXISTS [IDX_TrackInfo_InfoTimestamp] ON [TrackInfo](
  [InfoTimestamp] ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_TrackInfo_Listeners] ON [TrackInfo](
  [Listeners] DESC
);
CREATE INDEX  IF NOT EXISTS [IDX_TrackInfo_Playcount] ON [TrackInfo](
  [Playcount] DESC
);
";*/
		public static string ConnectionString(FileInfo dbFile) { return String.Format(DataConnectionString, dbFile.FullName); } 


		/// <summary>
		/// DbConnection is IDisposable, and the caller is responsible for disposing the connection.
		/// </summary>
		public static DbConnection ConstructConnection(FileInfo dbFile) {
			DbConnection conn = null;
			try {

				conn = System.Data.SQLite.SQLiteFactory.Instance.CreateConnection();
					//DbProviderFactories.GetFactory(DataProvider).CreateConnection();
				conn.ConnectionString = ConnectionString(dbFile);
				return conn;
			} catch { if (conn != null) conn.Dispose(); throw; }
		}
		public static DbConnection ConstructConnection(SongDatabaseConfigFile configFile) {
			return ConstructConnection(DbFile(configFile));
		}

		public static void CreateTables(DbConnection lfmDbConnection) {
			using (DbTransaction trans = lfmDbConnection.BeginTransaction())
			using (DbCommand createComm = lfmDbConnection.CreateCommand()) {
				createComm.CommandText = DatabaseDef;
				createComm.CommandTimeout = 0;
				//createComm.Prepare()
				
				createComm.ExecuteNonQuery();
				trans.Commit();
			}
		}
		const string filename = "lastFMcache.s3db";
		public static FileInfo DbFile(SongDatabaseConfigFile config) { return new FileInfo(Path.Combine(config.DataDirectory.CreateSubdirectory("cache").FullName, filename)); }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using SongDataLib;

namespace LastFMspider {
	public static class LastFmDbBuilder {
		//we set legacy format to false for better storage efficiency
		//we set datetime format to ticks for better efficiency (internally just stored as Int64)
		//rating is stored as a REAL which is a float in C#.

		const string DataProvider = "System.Data.SQLite";
		const string DataConnectionString = "page size=4096;cache size=100000;datetimeformat=Ticks;Legacy Format=False;Synchronous=Normal;Journal Mode=Persist;Default Timeout=30;data source=\"{0}\"";
		const string DatabaseDef = @"
PRAGMA journal_mode = PERSIST;


CREATE TABLE IF NOT EXISTS [Artist] (
  [ArtistID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [FullArtist] TEXT  NOT NULL,
  [LowercaseArtist] TEXT  NOT NULL,
  [IsAlternateOf] INTEGER NULL,
  [CurrentSimilarArtistList] INTEGER NULL,
  [CurrentTopTracksList] INTEGER NULL,
  [CurrentSimilarArtistListTimestamp] INTEGER NULL,
  [CurrentTopTracksListTimestamp] INTEGER NULL,

CONSTRAINT fk_is_alt_artist FOREIGN KEY(IsAlternateOf) REFERENCES Artist(ArtistID),
  CONSTRAINT fk_cur_sal_simart FOREIGN KEY(CurrentSimilarArtistList) REFERENCES SimilarArtistList(ListID),
  CONSTRAINT fk_cur_ttl_toptracks FOREIGN KEY(CurrentTopTracksList) REFERENCES TopTracksList(ListID)
);
CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Artist_LowercaseArtist] ON [Artist]([LowercaseArtist]  ASC);
CREATE INDEX IF NOT EXISTS [IDX_Artist_CurrentSimilarArtistListTimestamp] ON [Artist]([IsAlternateOf] ASC, [CurrentSimilarArtistListTimestamp]  ASC);
CREATE INDEX IF NOT EXISTS [IDX_Artist_CurrentTopTracksListTimestamp] ON [Artist]([IsAlternateOf] ASC, [CurrentTopTracksListTimestamp]  ASC);


CREATE TABLE IF NOT EXISTS [SimilarArtistList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
[SimilarArtists] BLOB,
CONSTRAINT fk_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID)
);
CREATE INDEX IF NOT EXISTS [IDX_SimilarArtistList_ArtistID_LookupTimestamp] ON [SimilarArtistList]([ArtistID]  ASC, [LookupTimestamp]  ASC);



CREATE TABLE IF NOT EXISTS  [Track] (
  [TrackID] INTEGER  NOT NULL PRIMARY KEY AUTOINCREMENT,
  [ArtistID] INTEGER  NOT NULL,
  [FullTitle] TEXT  NOT NULL,
  [LowercaseTitle] TEXT  NOT NULL,
  [CurrentSimilarTrackList] INTEGER NULL,
  [CurrentSimilarTrackListTimestamp] INTEGER NULL,
	CONSTRAINT fk_of_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID),
	CONSTRAINT fk_cur_stl FOREIGN KEY(CurrentSimilarTrackList) REFERENCES SimilarTrackList(ListID)
);
CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Track_ArtistID_LowercaseTitle] ON [Track]([ArtistID]  ASC,  [LowercaseTitle]  ASC);
CREATE INDEX IF NOT EXISTS [IDX_Track_CurrentSimilarTrackListTimestamp] ON [Track]([CurrentSimilarTrackListTimestamp]  ASC);



CREATE TABLE IF NOT EXISTS [SimilarTrackList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[TrackID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
[SimilarTracks] BLOB,
	CONSTRAINT fk_of_track FOREIGN KEY(TrackID) REFERENCES Track(TrackID)
);
DROP INDEX IF EXISTS [IDX_SimilarTrackList_LookupTimestamp]; 
DROP INDEX IF EXISTS [IDX_SimilarTrackList_TrackID];
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrackList_TrackID_LookupTimestamp] ON [SimilarTrackList](
  [TrackID]  ASC, 
  [LookupTimestamp]  ASC
);



CREATE TABLE IF NOT EXISTS [TopTracksList] (
[ListID] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
[StatusCode] INTEGER,
[TopTracks] BLOB,
CONSTRAINT fk_owner_artist FOREIGN KEY(ArtistID) REFERENCES Artist(ArtistID)
);
DROP INDEX IF EXISTS [IDX_TopTracksList_LookupTimestamp];
DROP INDEX IF EXISTS [IDX_TopTracksList_ArtistID];
CREATE INDEX IF NOT EXISTS [IDX_TopTracksList_ArtistID_LookupTimestamp] ON [TopTracksList](  [ArtistID]  ASC,  [LookupTimestamp]  ASC);
";

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
				createComm.CommandTimeout = 5;
				//createComm.Prepare()

				createComm.ExecuteNonQuery();
				trans.Commit();
			}
		}
		const string filename = "lastFMcache.s3db";
		public static FileInfo DbFile(SongDatabaseConfigFile config) { return new FileInfo(Path.Combine(config.DataDirectory.CreateSubdirectory("cache").FullName, filename)); }
	}
}

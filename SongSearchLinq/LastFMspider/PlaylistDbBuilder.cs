using System;
using System.Data.Common;
using System.IO;
using System.Runtime.InteropServices;
using SongDataLib;

namespace LastFMspider {
	public static class PlaylistDbBuilder {

		const string DataConnectionString = "page size=4096;cache size=2000;datetimeformat=Ticks;Legacy Format=False;Synchronous=Normal;Journal Mode=WAL;Default Timeout=30;data source=\"{0}\"";
		const string DatabaseDef = @"
pragma journal_mode=wal;

CREATE TABLE IF NOT EXISTS [Playlist] (
  [PlaylistID] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
  [LastVersionID] INTEGER NULL,
  [Username] TEXT NOT NULL,
  [PlaylistTitle] TEXT NOT NULL,
  [StoredTimestamp] INTEGER NOT NULL,
  [LastPlayedTimestamp] INTEGER NOT NULL,
  [IsCurrent] INTEGER NOT NULL,
  [PlayCount] INTEGER NOT NULL,
  [CumulativePlayCount] INTEGER NOT NULL,
  [PlaylistContents] TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS [IDX_Playlist_IsCurrent_User_Title] ON [Playlist] ([IsCurrent] ASC, [Username] ASC, [PlaylistTitle] ASC);
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

		public static DbConnection ConstructConnection(SongDataConfigFile configFile) {
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
		const string filename = "playlists.s3db";
		public static FileInfo DbFile(SongDataConfigFile config) { return new FileInfo(Path.Combine(config.DataDirectory.CreateSubdirectory("playlistDb").FullName, filename)); }
	}
}

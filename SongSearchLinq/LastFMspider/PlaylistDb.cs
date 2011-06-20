using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using LastFMspider.LastFMSQLiteBackend;
using SongDataLib;

namespace LastFMspider {
	public sealed class PlaylistDb : IDisposable {
		#region DbHelpers
		public readonly object SyncRoot = new object();

		DbConnection Connection { get; set; }

		public void DoInTransaction(Action action) { DoInTransaction(rollback => action()); }
		public void DoInTransaction(Action<Action> action) {
			DbTransaction tran = null;
			bool error = false;
			try {
				lock (SyncRoot)
					tran = Connection.BeginTransaction();
				action(() => { error = true; });
				if (!error)
					lock (SyncRoot)
						tran.Commit();
			} finally {
				if (tran != null)
					lock (SyncRoot)
						tran.Dispose();
			}
		}
		public T DoInTransaction<T>(Func<T> func) { return DoInTransaction(rollback => func()); }
		public T DoInTransaction<T>(Func<Action, T> func) {
			DbTransaction tran = null;
			bool error = false;
			try {
				lock (SyncRoot)
					tran = Connection.BeginTransaction();
				T retval = func(() => { error = true; });
				if (!error)
					lock (SyncRoot)
						tran.Commit();
				return retval;
			} finally {
				if (tran != null)
					lock (SyncRoot)
						tran.Dispose();
			}
		}

		public TOut DoInLockedTransaction<TOut>(Func<TOut> func) {
			lock (SyncRoot)
				using (var trans = Connection.BeginTransaction()) {
					TOut retval = func();
					trans.Commit();
					return retval;
				}
		}
		public void DoInLockedTransaction(Action action) {
			lock (SyncRoot)
				using (var trans = Connection.BeginTransaction()) {
					action();
					trans.Commit();
				}
		}

		internal DbCommand CreateCommand() { return Connection.CreateCommand(); }//must be locked already!
		#endregion

		class ParDef {
			public string name;
			public DbType? type;
		}

		DbCommand CreateCommand(string commandText, params string[] commandParams) {
			return CreateCommand(commandText, commandParams.Select(p => new ParDef { name = p }).ToArray());
		}

		DbCommand CreateCommand(string commandText, ParDef[] commandParams) {
			var command = CreateCommand();
			var dict = new Dictionary<string, DbParameter>();
			command.CommandText = commandText;
			foreach (var par in commandParams) {
				var dbpar = command.CreateParameter();
				dbpar.ParameterName = par.name;
				dbpar.DbType = par.type ?? DbType.Object;
				command.Parameters.Add(dbpar);
				dict.Add(par.name, dbpar);
			}
			return command;
		}

		DbCommand listAllPlaylists, storeNewPlaylist, updatePlaylistContents, loadPlaylist;
		DbCommand renamePlaylist;
		private DbCommand updatePlaycount;

		public PlaylistDb(SongDataConfigFile config, bool suppressCreation = false) {
			Connection = PlaylistDbBuilder.ConstructConnection(config);
			Connection.Open();
			PlaylistDbBuilder.CreateTables(Connection); 
			//if (!suppressCreation) try { PlaylistDbBuilder.CreateTables(Connection); } catch (SQLiteException) { }//if we can't create, we just hope the tables are already OK.

			InitCommands();
		}

		void InitCommands() {
			listAllPlaylists = CreateCommand(@"
				SELECT PlaylistID, LastVersionID, Username, PlaylistTitle,StoredTimestamp,LastPlayedTimestamp,PlayCount,CumulativePlayCount
                  FROM Playlist
                  WHERE IsCurrent = 1
                ");
			storeNewPlaylist = CreateCommand(@"
				INSERT INTO Playlist (Username,PlaylistTitle,StoredTimestamp, IsCurrent, PlayCount, CumulativePlayCount, PlaylistContents)
				  VALUES (@pUsername, @pPlaylistTitle, @pStoredTimestamp, 1, 0, 0, @pPlaylistContents);

				SELECT max(PlaylistID)
				FROM Playlist
				WHERE Username = @pUsername AND PlaylistTitle = @pPlaylistTitle AND StoredTimestamp = @pStoredTimestamp
			", "@pUsername", "@pPlaylistTitle", "@pStoredTimestamp", "@pPlaylistContents");

			updatePlaylistContents = CreateCommand(@"
				INSERT INTO Playlist (LastVersionID, Username, PlaylistTitle, StoredTimestamp, IsCurrent, PlayCount, CumulativePlayCount, PlaylistContents)
				SELECT						@pLastVersionID, @pUsername, @pPlaylistTitle, @pStoredTimestamp, 1,   0,             PlayCount,            @pPlaylistContents
				FROM Playlist WHERE PlaylistID = @pLastVersionID;
                
				UPDATE Playlist SET IsCurrent = 0 WHERE PlaylistID = @pLastVersionID;

				SELECT max(PlaylistID)
				FROM Playlist
				WHERE LastVersionID = @pLastVersionID AND StoredTimestamp = @pStoredTimestamp
			", "@pUsername", "@pPlaylistTitle", "@pStoredTimestamp", "@pPlaylistContents", "@pLastVersionID");

			loadPlaylist = CreateCommand(@"
				SELECT LastVersionID, Username, PlaylistTitle,StoredTimestamp,LastPlayedTimestamp,PlayCount,CumulativePlayCount, PlaylistContents
                FROM Playlist
                WHERE PlaylistID = @pPlaylistID
                ", "@pPlaylistID");

			renamePlaylist = CreateCommand(@"
				UPDATE Playlist SET PlaylistTitle = @pPlaylistTitle, Username = @pUsername WHERE PlaylistID = @pPlaylistID
                ", "@pPlaylistID","@pUsername", "@pPlaylistTitle");

			updatePlaycount = CreateCommand(@"
				UPDATE Playlist SET PlayCount = PlayCount + 1, CumulativePlayCount = CumulativePlayCount + 1, LastPlayedTimestamp = @pLastPlayedTimestamp
				WHERE PlaylistID = @pPlaylistID
                ", "@pPlaylistID", "@pLastPlayedTimestamp");
		}



		public long UpdatePlaylistContents(string username, string playlistTitle, DateTime storedTimestamp, string playlistContents, long lastVersionId) {
			return DoInLockedTransaction(() => {
				updatePlaylistContents.Parameters["@pUsername"].Value = username;
				updatePlaylistContents.Parameters["@pPlaylistTitle"].Value = playlistTitle;
				updatePlaylistContents.Parameters["@pStoredTimestamp"].Value = storedTimestamp.ToUniversalTime().Ticks;
				updatePlaylistContents.Parameters["@pPlaylistContents"].Value = playlistContents;
				updatePlaylistContents.Parameters["@pLastVersionID"].Value = lastVersionId;

				return updatePlaylistContents.ExecuteScalar().CastDbObjectAs<long>();
			});
		}


		public long StoreNewPlaylist(string username, string playlistTitle, DateTime storedTimestamp, string playlistContents) {
			return DoInLockedTransaction(() => {

				storeNewPlaylist.Parameters["@pUsername"].Value = username;
				storeNewPlaylist.Parameters["@pPlaylistTitle"].Value = playlistTitle;
				storeNewPlaylist.Parameters["@pStoredTimestamp"].Value = storedTimestamp.ToUniversalTime().Ticks;
				storeNewPlaylist.Parameters["@pPlaylistContents"].Value = playlistContents;

				return storeNewPlaylist.ExecuteScalar().CastDbObjectAs<long>();
			});
		}

		public class ListAllPlaylistResults {
			public long PlaylistID;
			public long? LastVersionID;
			public string Username, PlaylistTitle;
			public DateTime StoredTimestamp;
			public DateTime? LastPlayedTimestamp;
			public long PlayCount, CumulativePlayCount;
		}

		public ListAllPlaylistResults[] ListAllPlaylists() {
			return DoInLockedTransaction(() => {
				var retval = new List<ListAllPlaylistResults>();
				using (var reader = listAllPlaylists.ExecuteReader())
					while (reader.Read())
						retval.Add(new ListAllPlaylistResults {
							PlaylistID = reader[0].CastDbObjectAs<long>(),
							LastVersionID = reader[1].CastDbObjectAs<long?>(),
							Username = reader[2].CastDbObjectAs<string>(),
							PlaylistTitle = reader[3].CastDbObjectAs<string>(),
							StoredTimestamp = reader[4].CastDbObjectAsDateTime().Value,
							LastPlayedTimestamp = reader[5].CastDbObjectAsDateTime(),
							PlayCount = reader[6].CastDbObjectAs<long>(),
							CumulativePlayCount = reader[7].CastDbObjectAs<long>(),
						});

				return retval.ToArray();
			});
		}

		public class LoadPlaylistResult {
			public long? LastVersionID;
			public string Username, PlaylistTitle;
			public DateTime StoredTimestamp;
			public DateTime? LastPlayedTimestamp;
			public long PlayCount, CumulativePlayCount;
			public string PlaylistContents;
		}

		public LoadPlaylistResult LoadPlaylist(long playlistID) {
			return DoInLockedTransaction(() => {
				loadPlaylist.Parameters["@pPlaylistID"].Value = playlistID;
				var dbRow = loadPlaylist.ExecuteGetTopRow();
				return new LoadPlaylistResult {
					LastVersionID = dbRow[0].CastDbObjectAs<long?>(),
					Username = dbRow[1].CastDbObjectAs<string>(),
					PlaylistTitle = dbRow[2].CastDbObjectAs<string>(),
					StoredTimestamp = dbRow[3].CastDbObjectAsDateTime().Value,
					LastPlayedTimestamp = dbRow[4].CastDbObjectAsDateTime(),
					PlayCount = dbRow[5].CastDbObjectAs<long>(),
					CumulativePlayCount = dbRow[6].CastDbObjectAs<long>(),
					PlaylistContents = dbRow[7].CastDbObjectAs<string>(),
				};
			});
		}

		public void RenamePlaylist(long playlistID, string newUser, string newName) {
			DoInLockedTransaction(() => {
				renamePlaylist.Parameters["@pPlaylistID"].Value = playlistID;
				renamePlaylist.Parameters["@pUsername"].Value = newUser;
				renamePlaylist.Parameters["@pPlaylistTitle"].Value = newName;

				renamePlaylist.ExecuteNonQuery();
			});
		}

		public void UpdatePlaycount(long playlistID, DateTime lastplayed) {
			DoInLockedTransaction(() => {
				updatePlaycount.Parameters["@pPlaylistID"].Value = playlistID;
				updatePlaycount.Parameters["@pLastPlayedTimestamp"].Value = lastplayed.ToUniversalTime().Ticks;
				updatePlaycount.ExecuteNonQuery();
			});
		}

		public void Dispose() {
			lock (SyncRoot) {
				if (Connection != null)
					Connection.Dispose();
			}
		}
	}

}

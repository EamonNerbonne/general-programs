using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using System.Data.SQLite;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
    public class LastFMSQLiteCache {
        //we set legacy format to false for better storage efficiency
        //we set datetime format to ticks for better efficiency (interally just stored as integer)
        //rating is stored as a REAL which is a float in C#.

        const string DataProvider = "System.Data.SQLite";
        const string DataConnectionString = "page size=4096;datetimeformat=Ticks;Legacy Format=False;data source=\"{0}\"";
        const string DatabaseDef = @"
CREATE TABLE IF NOT EXISTS [Artist] (
[ArtistID] INTEGER  PRIMARY KEY NOT NULL,
[FullArtist] TEXT  NOT NULL,
[LowercaseArtist] TEXT  NOT NULL
);

CREATE TABLE IF NOT EXISTS [SimilarTrack] (
[SimilarTrackID] INTEGER  NOT NULL PRIMARY KEY,
[TrackA] INTEGER  NOT NULL,
[TrackB] INTEGER  NOT NULL,
[Rating] REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS  [Track] (
[TrackID] INTEGER  NOT NULL PRIMARY KEY,
[ArtistID] INTEGER  NOT NULL,
[FullTitle] TEXT  NOT NULL,
[LowercaseTitle] TEXT  NOT NULL,
[LookupTimestamp] INTEGER  NULL
);

CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_Artist_LowercaseArtist] ON [Artist](
[LowercaseArtist]  ASC
);

CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_TrackB] ON [SimilarTrack](
[TrackB]  ASC
);

CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarTrack_TrackA_TrackB] ON [SimilarTrack](
[TrackA]  ASC,
[TrackB]  ASC
);

CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Track_ArtistID_LowercaseTitle] ON [Track](
[ArtistID]  ASC,
[LowercaseTitle]  ASC
);
";


        FileInfo dbFile;
        internal DbConnection Connection { get; private set; }
        public LookupSimilarityList LookupSimilarityList { get; private set; }
        public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
        public LastFMSQLiteCache(FileInfo sqliteFile) { dbFile = sqliteFile; Init(); }


        /// <summary>
        /// Opens the DB connection.  Should be called by all constructors or whenever Db is reopened.
        /// </summary>
        private void Init() {
            //dbFile.Delete();//TODO:comment soon!
            string instanceConnStr = String.Format(DataConnectionString, dbFile.FullName);
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(DataProvider);
            Connection = dbProviderFactory.CreateConnection();
            //Connection could be disposed eventually - but when? We're not troubled with failing to release a bit of memory for a while.
            Connection.ConnectionString = instanceConnStr;
            Connection.Open();
            Create();
            PrepareSql();
        }


        private void PrepareSql() {
            PrepareInsertArtist();
            PrepareInsertTrack();
            PrepareInsertSimilarity();
            PrepareUpdateTimestamp();
            LookupSimilarityList = new LookupSimilarityList(this);
            LookupSimilarityListAge = new LookupSimilarityListAge(this);
            
            PrepareDeleteSimilaritiesOf();
        }


        private void Create() {
            using (DbTransaction trans = Connection.BeginTransaction()) {
                using (DbCommand createComm = Connection.CreateCommand()) {
                    createComm.CommandText = DatabaseDef;
                    createComm.ExecuteNonQuery();
                }
                trans.Commit();
            }
        }




        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        public void InsertSimilarityList(SongSimilarityList list, DateTime? lookupTimestamp) {
            if (list == null) return;
            var tracks = DeNull(list.similartracks).Select(sim => sim.similarsong).ToList();
            tracks.Add(list.songref);

            DateTime timestamp = lookupTimestamp ?? DateTime.UtcNow;

            using (DbTransaction trans = Connection.BeginTransaction()) {
                DateTime? oldTime = LookupSimilarityListAge.Execute(list.songref);
                if (oldTime != null) {
                    if ((DateTime)oldTime >= timestamp)
                        return;
                    else
                        DeleteSimilaritiesOf(list.songref);
                }


                foreach (var songref in tracks) InsertTrack(songref);
                foreach (var similartrack in DeNull(list.similartracks)) InsertSimilarity(list.songref, similartrack.similarsong, similartrack.similarity);
                UpdateTrackTimestamp(list.songref, timestamp);
                trans.Commit();
            }
        }

        //note that SQLite Administrator doesn't support numbers outside of int32!  It will display these timestamps as 0 even though they're actually some 64-bit number.
        const string updateTrackTimestampSql = @"
UPDATE [Track] SET LookupTimestamp = @ticks
WHERE LowercaseTitle = @lowerTitle AND ArtistID = (SELECT ArtistID FROM [Artist] WHERE LowercaseArtist = @lowerArtist)
";
        DbCommand updateTrackTimestampCommand;
        DbParameter updateTrackTimestampParamLowerTitle, updateTrackTimestampParamLowerArtist, updateTrackTimestampParamTicks;
        private void PrepareUpdateTimestamp() {
            updateTrackTimestampCommand = Connection.CreateCommand();
            updateTrackTimestampCommand.CommandText = updateTrackTimestampSql;

            updateTrackTimestampParamLowerTitle = new SQLiteParameter("@lowerTitle");
            updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamLowerTitle);

            updateTrackTimestampParamLowerArtist = new SQLiteParameter("@lowerArtist");
            updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamLowerArtist);

            updateTrackTimestampParamTicks = new SQLiteParameter("@ticks");
            updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamTicks);
        }

        private void UpdateTrackTimestamp(SongRef songRef, DateTime dateTime) {
            updateTrackTimestampParamTicks.Value = dateTime.Ticks;
            updateTrackTimestampParamLowerArtist.Value = songRef.Artist.ToLowerInvariant();
            updateTrackTimestampParamLowerTitle.Value = songRef.Title.ToLowerInvariant();
            updateTrackTimestampCommand.ExecuteNonQuery();
        }

        const string insertSimilaritySql = @"
INSERT OR REPLACE INTO [SimilarTrack] (TrackA, TrackB, Rating) 
SELECT A.TrackID, B.TrackID, (@rating) AS Rating
FROM Track A, Track B, Artist AsArtist, Artist BsArtist WHERE A.ArtistID = AsArtist.ArtistID AND B.ArtistID == BsArtist.ArtistID
  AND AsArtist.LowercaseArtist = @lowerArtistA AND A.LowercaseTitle == @lowerTitleA 
  AND BsArtist.LowercaseArtist = @lowerArtistB AND B.LowercaseTitle == @lowerTitleB
";
        DbCommand insertSimilarityCommand;
        DbParameter insertSimilarityParamLowerTitleA, insertSimilarityParamLowerTitleB, insertSimilarityParamLowerArtistA, insertSimilarityParamLowerArtistB, insertSimilarityParamRating;
        private void PrepareInsertSimilarity() {
            insertSimilarityCommand = Connection.CreateCommand();
            insertSimilarityCommand.CommandText = insertSimilaritySql;

            insertSimilarityParamRating = new SQLiteParameter("@rating");
            insertSimilarityCommand.Parameters.Add(insertSimilarityParamRating);

            insertSimilarityParamLowerArtistA = new SQLiteParameter("@lowerArtistA");
            insertSimilarityCommand.Parameters.Add(insertSimilarityParamLowerArtistA);

            insertSimilarityParamLowerTitleA = new SQLiteParameter("@lowerTitleA");
            insertSimilarityCommand.Parameters.Add(insertSimilarityParamLowerTitleA);

            insertSimilarityParamLowerArtistB = new SQLiteParameter("@lowerArtistB");
            insertSimilarityCommand.Parameters.Add(insertSimilarityParamLowerArtistB);

            insertSimilarityParamLowerTitleB = new SQLiteParameter("@lowerTitleB");
            insertSimilarityCommand.Parameters.Add(insertSimilarityParamLowerTitleB);
        }


        private void InsertSimilarity(SongRef songRefA, SongRef songRefB, double rating) {
            insertSimilarityParamRating.Value = rating;
            insertSimilarityParamLowerArtistA.Value = songRefA.Artist.ToLowerInvariant();
            insertSimilarityParamLowerTitleA.Value = songRefA.Title.ToLowerInvariant();
            insertSimilarityParamLowerArtistB.Value = songRefB.Artist.ToLowerInvariant();
            insertSimilarityParamLowerTitleB.Value = songRefB.Title.ToLowerInvariant();
            insertSimilarityCommand.ExecuteNonQuery();
        }

        const string deleteSimilaritiesOfSql = @"
DELETE FROM SimilarTrack
WHERE TrackA = (
  SELECT T.TrackID
  FROM Artist A,Track T
  WHERE A.LowercaseArtist= @lowerArtist
  AND A.ArtistID = T.ArtistID
  AND T.LowercaseTitle == @lowerTitle
);
UPDATE OR IGNORE Track
SET LookupTimestamp=NULL
WHERE LowercaseTitle = @lowerTitle
AND ArtistID=
  (SELECT A.ArtistID
   FROM Artist A
   WHERE A.LowercaseArtist = @lowerArtist)
";
        DbCommand deleteSimilaritiesOfCommand;
        DbParameter deleteSimilaritiesOfParamLowerArtist, deleteSimilaritiesOfParamLowerTitle;

        private void PrepareDeleteSimilaritiesOf() {
            deleteSimilaritiesOfCommand = Connection.CreateCommand();
            deleteSimilaritiesOfCommand.CommandText = deleteSimilaritiesOfSql;

            deleteSimilaritiesOfParamLowerArtist = new SQLiteParameter("@lowerArtist");
            deleteSimilaritiesOfCommand.Parameters.Add(deleteSimilaritiesOfParamLowerArtist);

            deleteSimilaritiesOfParamLowerTitle = new SQLiteParameter("@lowerTitle");
            deleteSimilaritiesOfCommand.Parameters.Add(deleteSimilaritiesOfParamLowerTitle);
        }

        private void DeleteSimilaritiesOf(SongRef songref) {
            deleteSimilaritiesOfParamLowerArtist.Value = songref.Artist.ToLowerInvariant();
            deleteSimilaritiesOfParamLowerTitle.Value = songref.Title.ToLowerInvariant();
            deleteSimilaritiesOfCommand.ExecuteNonQuery();

        }


        const string insertTrackSql = @"
INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
SELECT ArtistID, @fullTitle, @lowerTitle FROM [Artist]
WHERE LowercaseArtist = @lowerArtist
";
        DbCommand insertTrackCommand;
        DbParameter insertTrackParamFullTitle, insertTrackParamLowerTitle, insertTrackParamLowerArtist;
        private void PrepareInsertTrack() {
            insertTrackCommand = Connection.CreateCommand();
            insertTrackCommand.CommandText = insertTrackSql;

            insertTrackParamFullTitle = new SQLiteParameter("@fullTitle");
            insertTrackCommand.Parameters.Add(insertTrackParamFullTitle);

            insertTrackParamLowerTitle = new SQLiteParameter("@lowerTitle");
            insertTrackCommand.Parameters.Add(insertTrackParamLowerTitle);

            insertTrackParamLowerArtist = new SQLiteParameter("@lowerArtist");
            insertTrackCommand.Parameters.Add(insertTrackParamLowerArtist);
        }

        private void InsertTrack(SongRef songref) {
            InsertArtist(songref.Artist);
            insertTrackParamFullTitle.Value = songref.Title;
            insertTrackParamLowerTitle.Value = songref.Title.ToLowerInvariant();
            insertTrackParamLowerArtist.Value = songref.Artist.ToLowerInvariant();
            insertTrackCommand.ExecuteNonQuery();
        }


        const string insertArtistSql = @"INSERT OR IGNORE INTO [Artist](FullArtist, LowercaseArtist) VALUES (?,?)";
        DbCommand insertArtistCommand;
        DbParameter insertArtistParamFull, insertArtistParamLower;
        private void PrepareInsertArtist() {
            insertArtistCommand = Connection.CreateCommand();
            insertArtistCommand.CommandText = insertArtistSql;
            insertArtistParamFull = insertArtistCommand.CreateParameter();
            insertArtistParamLower = insertArtistCommand.CreateParameter();
            insertArtistCommand.Parameters.Add(insertArtistParamFull);
            insertArtistCommand.Parameters.Add(insertArtistParamLower);
        }
        private void InsertArtist(string artist) {
            insertArtistParamFull.Value = artist;
            insertArtistParamLower.Value = artist.ToLowerInvariant();
            insertArtistCommand.ExecuteNonQuery();
        }





    }

}

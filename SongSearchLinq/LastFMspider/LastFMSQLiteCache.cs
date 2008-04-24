﻿using System;
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
        public DeleteSimilaritiesOf DeleteSimilaritiesOf { get; private set; }
        public InsertArtist InsertArtist { get; private set; }
        public InsertTrack InsertTrack { get; private set; }
        public InsertSimilarity InsertSimilarity { get; private set; }
        public InsertSimilarityList InsertSimilarityList { get; private set; }
        public UpdateTrackTimestamp UpdateTrackTimestamp { get; private set; }
        //public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
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
            InsertTrack = new InsertTrack(this);
            InsertSimilarityList = new InsertSimilarityList(this);
            InsertSimilarity = new InsertSimilarity(this);
            InsertArtist = new InsertArtist(this);
            DeleteSimilaritiesOf = new DeleteSimilaritiesOf(this);
            UpdateTrackTimestamp = new UpdateTrackTimestamp(this);
            LookupSimilarityList = new LookupSimilarityList(this);
            LookupSimilarityListAge = new LookupSimilarityListAge(this);
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












    }

}

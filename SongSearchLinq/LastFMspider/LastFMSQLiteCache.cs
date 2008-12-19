using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.Common;
using System.Data.SQLite;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml.Linq;

namespace LastFMspider {
    public class LastFMSQLiteCache:IDisposable {
        public readonly object SyncRoot = new object();
        //we set legacy format to false for better storage efficiency
        //we set datetime format to ticks for better efficiency (interally just stored as integer)
        //rating is stored as a REAL which is a float in C#.

        const string DataProvider = "System.Data.SQLite";
        const string DataConnectionString = "page size=4096;cache size=1000000;datetimeformat=Ticks;Legacy Format=False;Synchronous=OFF;data source=\"{0}\"";
        const string EdmConnectionString = "metadata=res://*/EDM.LfmSqliteEdm.csdl|res://*/EDM.LfmSqliteEdm.ssdl|res://*/EDM.LfmSqliteEdm.msl;provider=System.Data.SQLite;provider connection string='{0}'";
        const string DatabaseDef = @"
CREATE TABLE IF NOT EXISTS [Artist] (
  [ArtistID] INTEGER  PRIMARY KEY NOT NULL,
  [FullArtist] TEXT  NOT NULL,
  [LowercaseArtist] TEXT  NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_Artist_LowercaseArtist] ON [Artist](
  [LowercaseArtist]  ASC
);



CREATE TABLE IF NOT EXISTS [SimilarArtistList] (
[ListID] INTEGER NOT NULL PRIMARY KEY,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarArtistList_ArtistID_LookupTimestamp] ON [SimilarArtistList](
  [ArtistID]  ASC,
  [LookupTimestamp]  DESC
);
CREATE INDEX IF NOT EXISTS [IDX_SimilarArtistList_LookupTimestamp] ON [SimilarArtistList](
  [LookupTimestamp]  ASC
);



CREATE TABLE IF NOT EXISTS [SimilarArtist] (
[SimilarArtistID] INTEGER  NOT NULL PRIMARY KEY,
[ListID] INTEGER  NOT NULL,
[ArtistB] INTEGER  NOT NULL,
[Rating] REAL NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarArtist_ArtistA_ArtistB] ON [SimilarArtist](
  [ListID]  ASC,
  [ArtistB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarArtist_ArtistB] ON [SimilarArtist](
  [ArtistB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarArtist_Rating] ON [SimilarArtist](
  [Rating]  DESC
);




CREATE TABLE IF NOT EXISTS  [Track] (
  [TrackID] INTEGER  NOT NULL PRIMARY KEY,
  [ArtistID] INTEGER  NOT NULL,
  [FullTitle] TEXT  NOT NULL,
  [LowercaseTitle] TEXT  NOT NULL,
  [LookupTimestamp] INTEGER  NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS [Unique_Track_ArtistID_LowercaseTitle] ON [Track](
  [ArtistID]  ASC,
  [LowercaseTitle]  ASC
);
CREATE INDEX IF NOT EXISTS [IDX_Track_LookupTimestamp] ON [Track](
  [LookupTimestamp]  ASC
);

CREATE TABLE IF NOT EXISTS [SimilarTrackList] (
[ListID] INTEGER NOT NULL PRIMARY KEY,
[TrackID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarArtistList_ArtistID_LookupTimestamp] ON [SimilarArtistList](
  [TrackID]  ASC,
  [LookupTimestamp]  DESC
);
CREATE INDEX IF NOT EXISTS [IDX_SimilarArtistList_LookupTimestamp] ON [SimilarArtistList](
  [LookupTimestamp]  ASC
);

CREATE TABLE IF NOT EXISTS [SimilarTrack] (
  [SimilarTrackID] INTEGER  NOT NULL PRIMARY KEY,
  [ListID] INTEGER  NOT NULL,
  [TrackB] INTEGER  NOT NULL,
  [Rating] REAL NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarTrack_TrackA_TrackB] ON [SimilarTrack](
  [ListID]  ASC,
  [TrackB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_TrackB] ON [SimilarTrack](
  [TrackB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_Rating] ON [SimilarTrack](
  [Rating]  DESC
);



CREATE TABLE IF NOT EXISTS [TopTracksList] (
[ListID] INTEGER NOT NULL PRIMARY KEY,
[ArtistID] INTEGER  NOT NULL,
[LookupTimestamp] INTEGER NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_TopTracksList_ArtistID_LookupTimestamp] ON [TopTracksList](
  [ArtistID]  ASC,
  [LookupTimestamp]  DESC
);
CREATE INDEX IF NOT EXISTS [IDX_TopTracksList_LookupTimestamp] ON [TopTracksList](
  [LookupTimestamp]  ASC
);


CREATE TABLE IF NOT EXISTS [TopTracks] (
[TopTrackID] INTEGER NOT NULL PRIMARY KEY,
[TrackID] INTEGER  NOT NULL,
[ListID] INTEGER  NOT NULL,
[Reach] INTEGER NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_TopTracks_ListID_TrackID] ON [TopTracks](
  [ListID]  ASC,
  [TrackID]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_TrackID] ON [TopTracks](
  [TrackID]  DESC
);
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_Reach] ON [TopTracks](
  [Reach]  DESC
);


";/*
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


        FileInfo dbFile;
        public DbConnection Connection { get; private set; }
        public LookupSimilarityList LookupSimilarityList { get; private set; }
        public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
        public InsertArtist InsertArtist { get; private set; }
        public InsertTrack InsertTrack { get; private set; }
        public LookupTrack LookupTrack { get; private set; }
        public LookupTrackID LookupTrackID { get; private set; }
        public InsertSimilarity InsertSimilarity { get; private set; }
        public InsertSimilarityList InsertSimilarityList { get; private set; }
        public CountSimilarities CountSimilarities { get; private set; }
        public CountRoughSimilarities CountRoughSimilarities { get; private set; }
        public AllTracks AllTracks { get; private set; }
        public TracksWithoutSimilarityList TracksWithoutSimilarityList { get; private set; }
        public RawArtists RawArtists { get; private set; }
        public RawTracks RawTracks { get; private set; }
        public UpdateTrackCasing UpdateTrackCasing { get; private set; }
        public RawSimilarTracks RawSimilarTracks { get; private set; }
        public LookupArtistSimilarityList LookupArtistSimilarityList { get; private set; }
        public LookupArtistSimilarityListAge LookupArtistSimilarityListAge { get; private set; }
        public InsertArtistSimilarity InsertArtistSimilarity { get; private set; }
        public InsertArtistSimilarityList InsertArtistSimilarityList { get; private set; }
        public ArtistsWithoutSimilarityList ArtistsWithoutSimilarityList { get; private set; }
        public ArtistsWithoutTopTracksList ArtistsWithoutTopTracksList { get; private set; }
        public LookupArtistTopTracksListAge LookupArtistTopTracksListAge { get; private set; }
        public LookupArtistTopTracksList LookupArtistTopTracksList { get; private set; }
        public InsertArtistTopTrack InsertArtistTopTrack { get; private set; }
        public InsertArtistTopTracksList InsertArtistTopTracksList { get; private set; }

        public LastFMSQLiteCache(FileInfo sqliteFile) { dbFile = sqliteFile; Init(); }


        /// <summary>
        /// Opens the DB connection.  Should be called by all constructors or whenever Db is reopened.
        /// </summary>
        private void Init() {
            string instanceConnStr = String.Format(DataConnectionString, dbFile.FullName);
            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(DataProvider);
            Connection = dbProviderFactory.CreateConnection();
            //Connection could be disposed eventually - but when? We're not troubled with failing to release a bit of memory for a while.
            Connection.ConnectionString = instanceConnStr;
            Connection.Open();
            Create();
            PrepareSql();

            string edmConnStr = String.Format(EdmConnectionString, instanceConnStr);
            //EDMCont = new LastFMspider.EDM.LfmSqliteEdmContainer(edmConnStr);
        }
        //public EDM.LfmSqliteEdmContainer EDMCont { get; private set; }


        private void PrepareSql() {
            InsertTrack = new InsertTrack(this);
            LookupTrack = new LookupTrack(this);
            LookupTrackID = new LookupTrackID(this);
            InsertSimilarityList = new InsertSimilarityList(this);
            InsertSimilarity = new InsertSimilarity(this);
            InsertArtist = new InsertArtist(this);
            LookupSimilarityList = new LookupSimilarityList(this);
            LookupSimilarityListAge = new LookupSimilarityListAge(this);
            CountSimilarities = new CountSimilarities(this);
            CountRoughSimilarities = new CountRoughSimilarities(this);
            AllTracks = new AllTracks(this);
            RawTracks = new RawTracks(this);
            RawArtists = new RawArtists(this);
            UpdateTrackCasing = new UpdateTrackCasing(this);
            RawSimilarTracks = new RawSimilarTracks(this);
            TracksWithoutSimilarityList = new TracksWithoutSimilarityList(this);
            LookupArtistSimilarityListAge = new LookupArtistSimilarityListAge(this);
            LookupArtistSimilarityList = new LookupArtistSimilarityList(this);
            InsertArtistSimilarityList = new InsertArtistSimilarityList(this);
            InsertArtistSimilarity = new InsertArtistSimilarity(this);
            ArtistsWithoutSimilarityList = new ArtistsWithoutSimilarityList(this);
            ArtistsWithoutTopTracksList = new ArtistsWithoutTopTracksList(this);
            LookupArtistTopTracksListAge = new LookupArtistTopTracksListAge(this);
            LookupArtistTopTracksList = new LookupArtistTopTracksList(this);
            InsertArtistTopTrack = new InsertArtistTopTrack(this);
            InsertArtistTopTracksList = new InsertArtistTopTracksList(this);
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


        #region IDisposable Members

        public void Dispose() {
            lock (SyncRoot) {
                Connection.Dispose();
            }
        }

        #endregion
    }

}

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
        //we set legacy format to false for better storage efficiency
        //we set datetime format to ticks for better efficiency (interally just stored as integer)
        //rating is stored as a REAL which is a float in C#.

        const string DataProvider = "System.Data.SQLite";
        const string DataConnectionString = "page size=4096;cache size=65536;datetimeformat=Ticks;Legacy Format=False;Synchronous=N;data source=\"{0}\"";
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

CREATE TABLE IF NOT EXISTS [SimilarTrack] (
  [SimilarTrackID] INTEGER  NOT NULL PRIMARY KEY,
  [TrackA] INTEGER  NOT NULL,
  [TrackB] INTEGER  NOT NULL,
  [Rating] REAL NOT NULL
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarTrack_TrackA_TrackB] ON [SimilarTrack](
  [TrackA]  ASC,
  [TrackB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_TrackB] ON [SimilarTrack](
  [TrackB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarTrack_Rating] ON [SimilarTrack](
  [Rating]  DESC
);


CREATE TABLE [SimilarArtist] (
[SimilarArtistID] INTEGER  NOT NULL PRIMARY KEY,
[ArtistA] INTEGER  NOT NULL,
[ArtistB] INTEGER  NOT NULL,
[Rating] REAL NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_SimilarArtist_ArtistA_ArtistB] ON [SimilarArtist](
  [ArtistA]  ASC,
  [ArtistB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarArtist_ArtistB] ON [SimilarArtist](
  [ArtistB]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_SimilarArtist_Rating] ON [SimilarArtist](
  [Rating]  DESC
);
CREATE INDEX IF NOT EXISTS [IDX_SimilarArtist_LookupTimestamp] ON [SimilarArtist](
  [LookupTimestamp]  DESC
);


CREATE TABLE [TopTracks] (
[TrackID] INTEGER  NOT NULL PRIMARY KEY,
[ArtistID] INTEGER  NOT NULL,
[Reach] REAL NOT NULL,
[LookupTimestamp] INTEGER NOT NULL,
);
CREATE UNIQUE INDEX  IF NOT EXISTS [Unique_TopTracks_ArtistID_TrackID] ON [TopTracks](
  [ArtistID]  ASC,
  [TrackID]  ASC
);
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_LookupTimestamp] ON [TopTracks](
  [LookupTimestamp]  DESC
);
CREATE INDEX  IF NOT EXISTS [IDX_TopTracks_Reach] ON [TopTracks](
  [Reach]  DESC
);





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
";


        FileInfo dbFile;
        public DbConnection Connection { get; private set; }
        public LookupSimilarityList LookupSimilarityList { get; private set; }
        public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
        public DeleteSimilaritiesOf DeleteSimilaritiesOf { get; private set; }
        public InsertArtist InsertArtist { get; private set; }
        public InsertTrack InsertTrack { get; private set; }
        public LookupTrack LookupTrack { get; private set; }
        public LookupTrackID LookupTrackID { get; private set; }
        public InsertTag InsertTag { get; private set; }
        public InsertTagging InsertTagging { get; private set; }
        public InsertSimilarity InsertSimilarity { get; private set; }
        public InsertSimilarityList InsertSimilarityList { get; private set; }
        public UpdateTrackTimestamp UpdateTrackTimestamp { get; private set; }
        public LookupReverseSimilarityList LookupReverseSimilarityList { get; private set; }
        public LookupSimilarityStats LookupSimilarityStats { get; private set; }
        public CountSimilarities CountSimilarities { get; private set; }
        public CountRoughSimilarities CountRoughSimilarities { get; private set; }
        public AllTracks AllTracks { get; private set; }
        public RawArtists RawArtists { get; private set; }
        public RawTracks RawTracks { get; private set; }
        public RawTags RawTags { get; private set; }
        public UpdateArtist UpdateArtist { get; private set; }
        public UpdateTrack UpdateTrack { get; private set; }
        public DeleteArtist DeleteArtist { get; private set; }
        public DeleteTrack DeleteTrack { get; private set; }
        public DeleteTag DeleteTag { get; private set; }
        public DeleteTagging DeleteTagging { get; private set; }
        public UpdateTrackCasing UpdateTrackCasing { get; private set; }
        public RawSimilarTracks RawSimilarTracks { get; private set; }
        //public LookupSimilarityListAge LookupSimilarityListAge { get; private set; }
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
            InsertTag = new InsertTag(this);
            InsertTagging = new InsertTagging(this);
            DeleteSimilaritiesOf = new DeleteSimilaritiesOf(this);
            UpdateTrackTimestamp = new UpdateTrackTimestamp(this);
            LookupSimilarityList = new LookupSimilarityList(this);
            LookupSimilarityListAge = new LookupSimilarityListAge(this);
            LookupSimilarityStats = new LookupSimilarityStats(this);
            LookupReverseSimilarityList = new LookupReverseSimilarityList(this);
            CountSimilarities = new CountSimilarities(this);
            CountRoughSimilarities = new CountRoughSimilarities(this);
            AllTracks = new AllTracks(this);
            RawTracks = new RawTracks(this);
            RawArtists = new RawArtists(this);
            RawTags = new RawTags(this);
            UpdateTrack = new UpdateTrack(this);
            UpdateArtist = new UpdateArtist(this);
            DeleteTrack = new DeleteTrack(this);
            DeleteArtist = new DeleteArtist(this);
            DeleteTagging = new DeleteTagging(this);
            DeleteTag = new DeleteTag(this);
            UpdateTrackCasing = new UpdateTrackCasing(this);
            RawSimilarTracks = new RawSimilarTracks(this);
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
            Connection.Dispose();
        }

        #endregion
    }

}

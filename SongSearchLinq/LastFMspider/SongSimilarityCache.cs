using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.PersistantCache;
using System.IO;
using System.Xml.Linq;
using System.Data.Common;
using System.Transactions;
using System.Data.SQLite;

namespace LastFMspider
{
	public class SongSimilarityCache
	{
        class InternalDB
        {
            const string DataProvider = "System.Data.SQLite";
            const string DataConnectionString = "page size=4096;datetimeformat=Ticks;legacy format=False;data source=\"{0}\"";
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
            DbConnection conn;

            public InternalDB(FileInfo sqliteFile) { dbFile = sqliteFile; Init(); }

            
            /// <summary>
            /// Opens the DB connection.  Should be called by all constructors or whenever Db is reopened.
            /// </summary>
            private void Init()
            {
                //dbFile.Delete();//TODO:comment soon!
                string instanceConnStr = String.Format(DataConnectionString, dbFile.FullName);
                DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(DataProvider);
                conn = dbProviderFactory.CreateConnection();
                //Connection could be disposed eventually - but when? We're not troubled with failing to release a bit of memory for a while.
                conn.ConnectionString = instanceConnStr;
                conn.Open();
                Create();
                PrepareSql();
            }


            private void PrepareSql()
            {
                PrepareInsertArtist();
                PrepareInsertTrack();
                PrepareInsertSimilarity();
                PrepareUpdateTimestamp();
            }


            private void Create()
            {
                using (DbTransaction trans = conn.BeginTransaction())
                {
                    using (DbCommand createComm = conn.CreateCommand())
                    {
                        createComm.CommandText = DatabaseDef;
                        createComm.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }

            /*public SongSimilarityList Lookup(SongRef songref)
            {

            }*/

            private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }


            const string lookupSimilarityListSql = @"
SELECT S.Rating, A.FullArtist, T.FullTitle FROM
  SimilarTrack S, Artist A, Track T, Track Torig, Artist Aorig
  WHERE
    Aorig.LowercaseArtist=@lowerArtist AND
    Aorig.ArtistID = Torig.ArtistID AND
    Torig.LowercaseTitle = @lowerTitle AND
    S.TrackA = Torig.TrackID AND
    T.TrackID = S.TrackB AND
    A.ArtistID = T.ArtistID
  ORDER BY S.Rating DESC
";
            DbCommand lookupSimilarityListCommand;
            DbParameter lookupSimilarityListParamLowerTitle, lookupSimilarityListParamLowerArtist;
            //private void PrepareUpdateTimestamp


            public SongSimilarityList LookupSimilarityList(SongRef songref)
            {

                throw new NotImplementedException("TODO");
            }

            public void InsertSimilarityList(SongSimilarityList list)
            {
                HashSet<SongRef> tracks = new HashSet<SongRef>(  DeNull(list.similartracks).Select(sim=>sim.similarsong) );
                tracks.Add(list.songref);


                using (DbTransaction trans = conn.BeginTransaction())
                {
                    foreach (var songref in tracks) InsertTrack(trans,songref);
                    foreach (var similartrack in DeNull(list.similartracks)) InsertSimilarity(list.songref, similartrack.similarsong, similartrack.similarity);
                    UpdateTrackTimestamp(list.songref, DateTime.Now.ToUniversalTime());
                    trans.Commit();
                }
            }

            const string updateTrackTimestampSql = @"
UPDATE [Track] SET LookupTimestamp = @ticks
WHERE LowercaseTitle = @lowerTitle AND ArtistID = (SELECT ArtistID FROM [Artist] WHERE LowercaseArtist = @lowerArtist)
";
            DbCommand updateTrackTimestampCommand;
            DbParameter updateTrackTimestampParamLowerTitle, updateTrackTimestampParamLowerArtist, updateTrackTimestampParamTicks;
            private void PrepareUpdateTimestamp()
            {
                updateTrackTimestampCommand = conn.CreateCommand();
                updateTrackTimestampCommand.CommandText = updateTrackTimestampSql;

                updateTrackTimestampParamLowerTitle = new SQLiteParameter("@lowerTitle");
                updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamLowerTitle);

                updateTrackTimestampParamLowerArtist = new SQLiteParameter("@lowerArtist");
                updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamLowerArtist);

                updateTrackTimestampParamTicks = new SQLiteParameter("@ticks");
                updateTrackTimestampCommand.Parameters.Add(updateTrackTimestampParamTicks);
            }

            private void UpdateTrackTimestamp(SongRef songRef, DateTime dateTime)
            {
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
            private void PrepareInsertSimilarity()
            {
                insertSimilarityCommand = conn.CreateCommand();
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


            private void InsertSimilarity(SongRef songRefA, SongRef songRefB, double rating)
            {
                insertSimilarityParamRating.Value = rating;
                insertSimilarityParamLowerArtistA.Value = songRefA.Artist.ToLowerInvariant();
                insertSimilarityParamLowerTitleA.Value = songRefA.Title.ToLowerInvariant();
                insertSimilarityParamLowerArtistB.Value = songRefB.Artist.ToLowerInvariant();
                insertSimilarityParamLowerTitleB.Value = songRefB.Title.ToLowerInvariant();
                insertSimilarityCommand.ExecuteNonQuery();
            }




            const string insertTrackSql = @"
INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
SELECT ArtistID, @fullTitle, @lowerTitle FROM [Artist]
WHERE LowercaseArtist = @lowerArtist
";
            DbCommand insertTrackCommand;
            DbParameter insertTrackParamFullTitle, insertTrackParamLowerTitle, insertTrackParamLowerArtist;
            private void PrepareInsertTrack()
            {
                insertTrackCommand = conn.CreateCommand();
                insertTrackCommand.CommandText = insertTrackSql;
                
                insertTrackParamFullTitle = new SQLiteParameter("@fullTitle");
                insertTrackCommand.Parameters.Add(insertTrackParamFullTitle);

                insertTrackParamLowerTitle = new SQLiteParameter("@lowerTitle");
                insertTrackCommand.Parameters.Add(insertTrackParamLowerTitle);

                insertTrackParamLowerArtist = new SQLiteParameter("@lowerArtist");
                insertTrackCommand.Parameters.Add(insertTrackParamLowerArtist);
            }

            private void InsertTrack(DbTransaction dbTrans, SongRef songref)
            {
                InsertArtist(dbTrans, songref.Artist);
                insertTrackParamFullTitle.Value = songref.Title;
                insertTrackParamLowerTitle.Value = songref.Title.ToLowerInvariant();
                insertTrackParamLowerArtist.Value = songref.Artist.ToLowerInvariant();
                insertTrackCommand.ExecuteNonQuery();
            }


            const string insertArtistSql = @"INSERT OR IGNORE INTO [Artist](FullArtist, LowercaseArtist) VALUES (?,?)";
            DbCommand insertArtistCommand;
            DbParameter insertArtistParamFull, insertArtistParamLower;
            private void PrepareInsertArtist()
            {
                insertArtistCommand = conn.CreateCommand();
                insertArtistCommand.CommandText = insertArtistSql;
                insertArtistParamFull = insertArtistCommand.CreateParameter();
                insertArtistParamLower = insertArtistCommand.CreateParameter();
                insertArtistCommand.Parameters.Add(insertArtistParamFull);
                insertArtistCommand.Parameters.Add(insertArtistParamLower);
            }
            private void InsertArtist(DbTransaction dbTrans,string artist)
            {
                    insertArtistParamFull.Value = artist;
                    insertArtistParamLower.Value = artist.ToLowerInvariant();
                    insertArtistCommand.ExecuteNonQuery();
            }





        }
		PersistantCache<SongRef, SongSimilarityList> backingCache;
        InternalDB backingDB;
		public SongSimilarityCache(DirectoryInfo cacheDir) {
            Console.WriteLine("Initializing file db");
            backingCache = new PersistantCache<SongRef, SongSimilarityList>(cacheDir, ".xml", new Mapper());
            // Console.WriteLine("Initializing sqlite db");
           // backingDB = new InternalDB(new FileInfo(Path.Combine(cacheDir.FullName,"lastFMcache.s3db")));//TODO decide what kind of DB we really want...
            //Console.WriteLine("Porting file -> sqlite ...");
            //Port();
		}

        public IEnumerable<SongRef> DiskCacheContents()
        {
            foreach (string songrefStr in backingCache.GetDiskCacheContents())
                yield return SongRef.CreateFromCacheName(songrefStr);
        }

        public void Port()
        {
            List<SongSimilarityList> songSims = new List<SongSimilarityList>();
            Dictionary<SongRef, SongRef> findCapitalization = new Dictionary<SongRef, SongRef>();
            HashSet<SongRef> noSims = new HashSet<SongRef>();
            int progress=0;
            foreach (string keystring in backingCache.GetDiskCacheContents())
            {
                SongRef songref = SongRef.CreateFromCacheName(keystring);
                SongSimilarityList list = backingCache.Lookup(songref);
                findCapitalization[songref] = songref;//add all listed songrefs
                if (list != null)
                    songSims.Add(list);
                else
                    noSims.Add(songref);

                if (++progress % 100 == 0)
                    Console.WriteLine("Loaded {0}.", progress);
            }
            //we have all songsimilarities loaded!
            foreach (SongSimilarityList list in songSims)
                foreach (SimilarTrack similar in list.similartracks)
                    if(findCapitalization.ContainsKey(similar.similarsong))
                        findCapitalization[similar.similarsong] = similar.similarsong;//add useful capitalization
            foreach (SongSimilarityList list in songSims)
                list.songref = findCapitalization[list.songref]; //fix capitalization.
            noSims = new HashSet<SongRef>(noSims.Select(sr => findCapitalization[sr]));
            //OK we have correctly capitalized muck, hopefully.

            progress = 0;
            foreach (var list in songSims.Concat(noSims.Select(sr => new SongSimilarityList() { songref = sr, similartracks = null })))
            {
                backingDB.InsertSimilarityList(list);
                
                if (++progress % 100 == 0)
                    Console.WriteLine("Stored {0}.", progress);

            }
        }



		public Dictionary<SongRef,SongSimilarityList> Cache { get { return backingCache.MemoryCache; } }

		public SongSimilarityList Lookup(SongRef songref) { try { return backingCache.Lookup(songref); } catch(PersistantCacheException) { return null; } }

		private class Mapper : IPersistantCacheMapper<SongRef, SongSimilarityList>
		{
            static TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
            DateTime nextRequestWhen = DateTime.Now;
			string xmlrep;
			SongSimilarityList lastlist;
			public string KeyToString(SongRef key) {				return key.CacheName();			}

			public SongSimilarityList Evaluate(SongRef songref) {
                try
                {
                    var now = DateTime.Now;
                    if (nextRequestWhen > now)
                    {
                        Console.Write("<");
                        System.Threading.Thread.Sleep(nextRequestWhen - now);
                        Console.Write(">");
                    }
                    nextRequestWhen = now + minReqDelta;
                    xmlrep = new System.Net.WebClient().DownloadString(songref.AudioscrobblerSimilarUrl());
                    return lastlist = SongSimilarityList.CreateFromAudioscrobblerXml(songref, XDocument.Parse(xmlrep));
                }
                catch { return null; }//don't bother trying if anything happens
			}

			public void StoreItem(SongSimilarityList item, Stream to) {
				try {
					if(lastlist != item) {
						throw new NotImplementedException("Can only store similarity lists just retrieved from lastfm.");
					}
					using(var w = new StreamWriter(to))						w.Write(xmlrep ?? "");
				} finally {
					xmlrep = null;
					lastlist = null;
				}
			}

			public SongSimilarityList LoadItem(SongRef songref,Stream from) {
				string loadxmlrep=new StreamReader(from).ReadToEnd();
				XDocument doc;
				try {
					doc = XDocument.Parse(loadxmlrep);
				} catch {
					return null;
				}
				return SongSimilarityList.CreateFromAudioscrobblerXml(songref, doc);
			}

		}

	}
}

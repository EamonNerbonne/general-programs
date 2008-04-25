using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.PersistantCache;
using System.IO;
using System.Xml.Linq;
using LastFMspider.LastFMSQLiteBackend;

namespace LastFMspider {
    public class SongSimilarityCache {
        PersistantCache<SongRef, SongSimilarityList> backingCache; //TODO split backingCache and backingDB into separate SimilarityCaches
        LastFMSQLiteCache backingDB;
        public SongSimilarityCache(DirectoryInfo cacheDir) {
            Console.WriteLine("Initializing sqlite db");
            backingDB = new LastFMSQLiteCache(new FileInfo(Path.Combine(cacheDir.FullName, "lastFMcache.s3db")));//TODO decide what kind of DB we really want...
            Console.WriteLine("Initializing file db");
            backingCache = new PersistantCache<SongRef, SongSimilarityList>(cacheDir, ".xml", new Mapper(this));
        }

        public SimilarityStat[] LookupDbStats() {
            Console.WriteLine("Getting DB similarity stats (this may take a while)");
            DateTime start = DateTime.UtcNow;
            var retval = backingDB.LookupSimilarityStats.Execute();
            DateTime end = DateTime.UtcNow;
            Console.WriteLine("========Took {0} seconds!", (end - start).TotalSeconds);
            return retval;
        }

        public IEnumerable<SongRef> DiskCacheContents() {
            foreach (string songrefStr in backingCache.GetDiskCacheContents())
                yield return SongRef.CreateFromCacheName(songrefStr);
        }

        public void Port() {
            Console.WriteLine("Porting file -> sqlite ...");
            var songSims = new List<Timestamped<SongSimilarityList>>();
            Dictionary<SongRef, SongRef> findCapitalization = new Dictionary<SongRef, SongRef>();
            int progress = 0;
            string[] keys = backingCache.GetDiskCacheContents().ToArray();
            int numKeys = keys.Length;
            using (var trans = backingDB.Connection.BeginTransaction()) {
                try {
                    foreach (string keystring in keys) {
                        try {
                            progress++;
                            SongRef songref = SongRef.CreateFromCacheName(keystring);
                            var list = backingCache.Lookup(songref);
                            
                            if (list.Item != null) {
                                songSims.Add(list);
                                if (list.Item.similartracks != null) {
                                    foreach (var simtrack in list.Item.similartracks) //note this is part of one big transaction!
                                        backingDB.InsertTrack.Execute(simtrack.similarsong); //by pre-inserting, capitalization from last.fm takes priority over our own.
                                }
                            }
                            else //occurs only on error, but we should cache those too.
                                songSims.Add(
                                    new Timestamped<SongSimilarityList> {
                                        Item = new SongSimilarityList {
                                            songref = songref,
                                            similartracks = null
                                        },
                                        Timestamp = list.Timestamp
                                    }
                                    );

                            if (progress % 100 == 0)
                                Console.WriteLine("Loaded {0}: {1}/{2}.", 100.0 * progress / numKeys, progress, numKeys);
                        }
                        catch (Exception e) {
                            Console.WriteLine("Failed on {0}", keystring);
                            Console.WriteLine("Exception {0}, {1}", e.Message, e.StackTrace);
                        }
                    }
                }
                finally {
                    trans.Commit(); //safe to commit even with exception since separate components are also transactions.
                }
            }
            keys = null;
            progress = 0;
            using (var trans = backingDB.Connection.BeginTransaction()) {
                try {

                    foreach (var list in songSims) {
                        try {
                            progress++;
                            backingDB.InsertSimilarityList.Execute(list.Item, list.Timestamp);
                            backingCache.DeleteItem(list.Item.songref);
                            if (progress % 100 == 0)
                                Console.WriteLine("Loaded {0}: {1}/{2}.", 100.0 * progress / numKeys, progress, numKeys);
                        }
                        catch (Exception e) {
                            Console.WriteLine("Failed on {0}th, ", progress, list.Item == null ? "<null-list>" : list.Item.songref == null ? "<null-songref>" : list.Item.songref.ToString());
                            Console.WriteLine("Exception {0}, {1}", e.Message, e.StackTrace);
                        }

                    }
                }
                finally {
                    trans.Commit(); //safe to commit even with exception since separate components are also transactions.
                }
            }

        }

        public Dictionary<SongRef, Timestamped<SongSimilarityList>> MemoryCache { get { return backingCache.MemoryCache; } }

        public SongSimilarityList Lookup(SongRef songref) {
            try {
                return LookupViaSQLite(songref);
             //   return backingCache.Lookup(songref);
            }
            catch (PersistantCacheException) { return null; }
        }

        private SongSimilarityList LookupViaSQLite(SongRef songref) {
            DateTime? cachedVersionAge = backingDB.LookupSimilarityListAge.Execute(songref);
            if (cachedVersionAge == null) { //get online version
                var retval = DirectWebRequest(songref);
                backingDB.InsertSimilarityList.Execute(retval, DateTime.UtcNow);
                return retval;
            }
            else {
                return  backingDB.LookupSimilarityList.Execute(songref);
            }
        }

        TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
        DateTime nextRequestWhen = DateTime.Now;
        string lastlistxmlrep;
        SongSimilarityList lastlist;

        private SongSimilarityList DirectWebRequest(SongRef songref) {
            try {
                var now = DateTime.Now;
                if (nextRequestWhen > now) {
                    Console.Write("<");
                    System.Threading.Thread.Sleep(nextRequestWhen - now);
                    Console.Write(">");
                }
                nextRequestWhen = now + minReqDelta;
                lastlistxmlrep = new System.Net.WebClient().DownloadString(songref.AudioscrobblerSimilarUrl());
                return lastlist = SongSimilarityList.CreateFromAudioscrobblerXml(songref, XDocument.Parse(lastlistxmlrep));
            }
            catch { return new SongSimilarityList { songref = songref, similartracks = null }; }//Also cache negatively; assume 404s and other errors remain.
        }

        private class Mapper : IPersistantCacheMapper<SongRef, SongSimilarityList> {
            SongSimilarityCache owner;
            public Mapper(SongSimilarityCache owner) {
                this.owner = owner;
            }
            public string KeyToString(SongRef key) { return key.CacheName(); }

            public SongSimilarityList Evaluate(SongRef songref) {
                return owner.DirectWebRequest(songref);
            }

            public void StoreItem(SongSimilarityList item, Stream to) {
                try {
                    if (owner.lastlist != item) {
                        throw new NotImplementedException("Can only store similarity lists just retrieved from lastfm.");
                    }
                    using (var w = new StreamWriter(to)) w.Write(owner.lastlistxmlrep ?? "");
                }
                finally {
                    owner.lastlistxmlrep = null;
                    owner.lastlist = null;
                }
            }

            public SongSimilarityList LoadItem(SongRef songref, Stream from) {
                string loadxmlrep = new StreamReader(from).ReadToEnd();
                XDocument doc;
                try {
                    doc = XDocument.Parse(loadxmlrep);
                    return SongSimilarityList.CreateFromAudioscrobblerXml(songref, doc);
                }
                catch {
                    return null;
                }
                
            }

        }

    }
}

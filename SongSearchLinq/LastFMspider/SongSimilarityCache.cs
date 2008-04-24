using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.PersistantCache;
using System.IO;
using System.Xml.Linq;

namespace LastFMspider {
    public class SongSimilarityCache {
        PersistantCache<SongRef, SongSimilarityList> backingCache;
        LastFMSQLiteCache backingDB;
        public SongSimilarityCache(DirectoryInfo cacheDir) {
            Console.WriteLine("Initializing sqlite db");
            backingDB = new LastFMSQLiteCache(new FileInfo(Path.Combine(cacheDir.FullName, "lastFMcache.s3db")));//TODO decide what kind of DB we really want...
            Console.WriteLine("Initializing file db");
            backingCache = new PersistantCache<SongRef, SongSimilarityList>(cacheDir, ".xml", new Mapper(this));
            Console.WriteLine("Porting file -> sqlite ...");
            //Port();
        }

        public IEnumerable<SongRef> DiskCacheContents() {
            foreach (string songrefStr in backingCache.GetDiskCacheContents())
                yield return SongRef.CreateFromCacheName(songrefStr);
        }

        public void Port() {
            var songSims = new List<Timestamped<SongSimilarityList>>();
            Dictionary<SongRef, SongRef> findCapitalization = new Dictionary<SongRef, SongRef>();
            int progress = 0;
            foreach (string keystring in backingCache.GetDiskCacheContents()) {
                SongRef songref = SongRef.CreateFromCacheName(keystring);
                var list = backingCache.Lookup(songref);
                findCapitalization[songref] = songref;//add all listed songrefs
                if (list.Item != null)
                    songSims.Add(list);
                else
                    songSims.Add(
                        new Timestamped<SongSimilarityList> {
                            Item = new SongSimilarityList {
                                songref = songref,
                                similartracks = null
                            },
                            Timestamp = list.Timestamp
                        }
                        );

                if (++progress % 100 == 0)
                    Console.WriteLine("Loaded {0}.", progress);
            }
            //we have all songsimilarities loaded!
            var capitizableSongs = from timestampedList in songSims
                                   where timestampedList.Item.similartracks != null
                                   from similartrack in timestampedList.Item.similartracks
                                   where findCapitalization.ContainsKey(similartrack.similarsong)
                                   select similartrack.similarsong;
            foreach (var capSong in capitizableSongs)
                        findCapitalization[capSong] = capSong;//add useful capitalization
            foreach (var tlist in songSims)
                tlist.Item.songref = findCapitalization[tlist.Item.songref]; //fix capitalization in similarity lists
            //OK we have correctly capitalized muck, hopefully.

            progress = 0;
            foreach (var list in songSims) {
                backingDB.InsertSimilarityList.Execute(list.Item, list.Timestamp);//TODO, deal with dates
                backingCache.DeleteItem(list.Item.songref);
                if (++progress % 100 == 0)
                    Console.WriteLine("Stored {0}.", progress);

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
                }
                catch {
                    return null;
                }
                return SongSimilarityList.CreateFromAudioscrobblerXml(songref, doc);
            }

        }

    }
}

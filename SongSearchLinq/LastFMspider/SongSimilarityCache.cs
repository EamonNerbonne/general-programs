using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.PersistantCache;
using System.IO;
using System.Xml.Linq;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml;

using EmnExtensions.Text;
using EmnExtensions.Web;
using EmnExtensions.Collections;
using EmnExtensions;
using System.Diagnostics;
using SongDataLib;

namespace LastFMspider {
    public class SongSimilarityCache {
        public LastFMSQLiteCache backingDB { get; private set; }
        public SongSimilarityCache(DirectoryInfo cacheDir) {
            Init(cacheDir);
        }

        public SongSimilarityCache(SongDatabaseConfigFile configFile) {
            Init(configFile);
        }


        private void Init() {
            Console.WriteLine("Loading config...");
            var configFile = new SongDatabaseConfigFile(false);
            Init(configFile);
        }

        private void Init(SongDatabaseConfigFile configFile) {
            Init(configFile.DataDirectory.CreateSubdirectory("cache"));
        }

        private void Init(DirectoryInfo cacheDir) {
            Console.WriteLine("Initializing sqlite db");
            backingDB = new LastFMSQLiteCache(new FileInfo(Path.Combine(cacheDir.FullName, "lastFMcache.s3db")));//TODO decide what kind of DB we really want...
        }


        public SongSimilarityList Lookup(SongRef songref) {
            bool ignore;
            return Lookup(songref, out ignore);
        }
        public SongSimilarityList Lookup(SongRef songref, out bool isNewlyDownloaded) {
            return LookupViaSQLite(songref, out isNewlyDownloaded);
        }

        private SongSimilarityList LookupViaSQLite(SongRef songref, out bool isNewlyDownloaded) {
            DateTime? cachedVersionAge=null;
            using (var trans = backingDB.Connection.BeginTransaction()) {
                cachedVersionAge = backingDB.LookupSimilarityListAge.Execute(songref);
            }
            if (cachedVersionAge == null) { //get online version
                Console.Write("?");
                var retval = DirectWebRequest(songref);
                isNewlyDownloaded = true;
                try {
                    backingDB.InsertSimilarityList.Execute(retval);
                } catch {//retry; might be a locking issue.  only retry once.
                    System.Threading.Thread.Sleep(100);
                    backingDB.InsertSimilarityList.Execute(retval);
                }
                return retval;
            } else {
                isNewlyDownloaded = false;
                return backingDB.LookupSimilarityList.Execute(songref);
            }
        }

        TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
        DateTime nextRequestWhen = DateTime.Now;
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
                //WebClient.Download???? has encoding issues, hence the use of UriRequest.
                var requestedData= UriRequest.Execute(new Uri(songref.AudioscrobblerSimilarUrl()));
               
                var xdoc = XDocument.Parse(requestedData.ContentAsString);
                lastlist = SongSimilarityList.CreateFromAudioscrobblerXml(songref,xdoc,DateTime.UtcNow );
                return lastlist;
            } catch { return new SongSimilarityList { songref = songref, similartracks = null }; }//Also cache negatively; assume 404s and other errors remain.
        }
    }
}

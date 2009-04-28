using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using LastFMspider.OldApi;

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
            ListStatus? cachedVersion = backingDB.LookupSimilarityListAge.Execute(songref);
            if (cachedVersion == null) { //get online version
                Console.Write("?");
                var retval = DirectWebRequest(songref);
                isNewlyDownloaded = true;
                if (retval == null) 
                    return retval;
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


        private static IEnumerable<T> DeNull<T>(IEnumerable<T> iter) { return iter == null ? Enumerable.Empty<T>() : iter; }
        private SongSimilarityList DirectWebRequest(SongRef songref) {
            try {
                ApiTrackSimilarTracks simTracks = OldApiClient.Track.GetSimilarTracks(songref);
                var newEntry = simTracks == null
                    ? new SongSimilarityList {//represents 404 Not Found
                        LookupTimestamp = DateTime.UtcNow,
                        songref = songref,
                        similartracks = new SimilarTrack[0],
                    }
                    : new SongSimilarityList {
                        LookupTimestamp = DateTime.UtcNow,
                        songref = songref,
                        similartracks = DeNull(simTracks.track).Select(simTrack => new SimilarTrack {
                            similarity = simTrack.match,
                            similarsong = SongRef.Create(simTrack.artist.name, simTrack.name),
                        }).ToArray(),
                    };
                return newEntry;
            } catch (Exception) {
                return null;
            }
        }
    }
}

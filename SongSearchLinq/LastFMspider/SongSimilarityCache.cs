using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.PersistantCache;
using System.IO;
using System.Xml.Linq;
using LastFMspider.LastFMSQLiteBackend;
using System.Xml;

using EamonExtensionsLinq.Text;
using EamonExtensionsLinq.Web;

namespace LastFMspider {
    public class SongSimilarityCache {
        LastFMSQLiteCache backingDB;
        public SongSimilarityCache(DirectoryInfo cacheDir) {
            Console.WriteLine("Initializing sqlite db");
            backingDB = new LastFMSQLiteCache(new FileInfo(Path.Combine(cacheDir.FullName, "lastFMcache.s3db")));//TODO decide what kind of DB we really want...
        }

        public SimilarityStat[] LookupDbStats() {
            Console.WriteLine("Getting DB similarity stats (this may take a while)");
            DateTime start = DateTime.UtcNow;
            var retval = backingDB.LookupSimilarityStats.Execute();
            DateTime end = DateTime.UtcNow;
            Console.WriteLine("========Took {0} seconds!", (end - start).TotalSeconds);
            return retval;
        }
        public void ConvertEncoding() {
            using (var trans = backingDB.Connection.BeginTransaction()) {
                var artists = backingDB.RawArtists.Execute();
                foreach (var artist in artists) {
                    var webbytes = Encoding.Default.GetBytes(artist.FullArtist);
                    var goodstr = Encoding.UTF8.GetString(webbytes);
                    if (goodstr != artist.FullArtist) {
                        Console.Write("A: {0} isn't {1}: ", artist.FullArtist, goodstr);
                        artist.FullArtist = goodstr;
                        artist.LowercaseArtist = goodstr.ToLowerInvariant();
                        if (goodstr.ToCharArray().All(c => Canonicalize.IsReasonableChar(c) && c != (char)65533)) {
                            try {
                                backingDB.UpdateArtist.Execute(artist);
                                Console.WriteLine("Updated.");
                            } catch {
                                backingDB.DeleteArtist.Execute(artist.ArtistID);
                                Console.WriteLine("Deleted dup.");
                            }
                        } else {
                            backingDB.DeleteArtist.Execute(artist.ArtistID);
                            Console.WriteLine("Deleted nogood.");
                        }
                    }
                }
                trans.Commit();
            }
                using (var trans = backingDB.Connection.BeginTransaction()) {
                var tracks = backingDB.RawTracks.Execute();
                foreach (var track in tracks) {
                    var webbytes = Encoding.Default.GetBytes(track.FullTitle);
                    var goodstr = Encoding.UTF8.GetString(webbytes);
                    if (goodstr != track.FullTitle) {
                        Console.Write("T: {0} isn't {1}: ", track.FullTitle, goodstr);
                        track.FullTitle = goodstr;
                        track.LowercaseTitle = goodstr.ToLowerInvariant();
                        if (goodstr.ToCharArray().All(c => Canonicalize.IsReasonableChar(c) && c != (char)65533)) {
                            try {
                                backingDB.UpdateTrack.Execute(track);
                                Console.WriteLine("Updated.");

                            } catch {
                                backingDB.DeleteTrack.Execute(track.TrackID);
                                Console.WriteLine("Deleted dup.");
                            }
                        } else {
                            backingDB.DeleteTrack.Execute(track.TrackID);
                            Console.WriteLine("Deleted nogood.");
                        }
                    }
                }
                trans.Commit();
            }
        }

        public SongSimilarityList Lookup(SongRef songref) {
            try {
                return LookupViaSQLite(songref);
                //   return backingCache.Lookup(songref);
            } catch (PersistantCacheException) { return null; }
        }

        private SongSimilarityList LookupViaSQLite(SongRef songref) {
            DateTime? cachedVersionAge = backingDB.LookupSimilarityListAge.Execute(songref);
            if (cachedVersionAge == null) { //get online version
                var retval = DirectWebRequest(songref);
                backingDB.InsertSimilarityList.Execute(retval, DateTime.UtcNow);
                return retval;
            } else {
                return backingDB.LookupSimilarityList.Execute(songref);
            }
        }

        TimeSpan minReqDelta = new TimeSpan(0, 0, 0, 1);//no more than one request per second.
        DateTime nextRequestWhen = DateTime.Now;
        byte[] lastlistxmlrep;
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
                //lastlistxmlrep = new System.Net.WebClient().DownloadData(songref.AudioscrobblerSimilarUrl());//important:DownloadString destroys data due to encoding!
                //var xdoc=XDocument.Load( XmlReader.Create(new MemoryStream(lastlistxmlrep)));
                var requestedData= UriRequest.Execute(new Uri(songref.AudioscrobblerSimilarUrl()));//TODO: check that this actually works...
                lastlistxmlrep = requestedData.Content;
               
                var xdoc = XDocument.Parse(requestedData.ContentAsString);
                lastlist = SongSimilarityList.CreateFromAudioscrobblerXml(songref,xdoc );
                return lastlist;
            } catch { return new SongSimilarityList { songref = songref, similartracks = null }; }//Also cache negatively; assume 404s and other errors remain.
        }
    }
}

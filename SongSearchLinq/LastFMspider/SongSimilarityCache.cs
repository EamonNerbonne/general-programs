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
using EamonExtensionsLinq.Collections;
using EamonExtensionsLinq;
using System.Diagnostics;

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

        public struct SimTrans {
            public float rateAB,rateBC,rateAC;
        }
        private struct SimilarTo { public int track; public float rating; }

        private TOut[][] ReMap<TIn, TOut>(int idCount, TIn[] inSet, Func<TIn,int> getId,Func<TIn,TOut> getVal ) {
            int[] valCount = new int[idCount];
            foreach (var item in inSet)
                valCount[getId(item)]++;

            TOut[][] map = new TOut[idCount][];
            for (int itemId = 0; itemId < idCount; itemId++)
                map[itemId] = new TOut[valCount[itemId]];


            foreach (var item in inSet) {
                int id = getId(item);
                map[id][--valCount[id]] = getVal(item);
            }
            return map;
        }

        static float CentralMoment(IEnumerable<float> list, float average, int moment) {
            return (float)list.Average(x => Math.Pow(x-average,moment));
        }
        static float Covariance(Statistics A, Statistics B) {
            return (float)(A.Seq.Cast<double>().ZipWith(B.Seq.Cast<double>(), (a, b) => (a - A.Mean) * (b - B.Mean)).Average() /Math.Sqrt(A.Var)/Math.Sqrt(B.Var) );
        }


        class Statistics {
            public float Mean, Var, Skew, Kurtosis;
            public int Count;
            public IEnumerable<float> Seq;
            public Statistics(IEnumerable<SimTrans> seq, Func<float, float, float> comb) :this(seq.Select(r=>comb(r.rateAB,r.rateBC)) ) { }


            public Statistics(IEnumerable<float> seq) {
                Mean = seq.Average();
                Count = seq.Count();
                Var = CentralMoment(seq, Mean, 2);
                Skew = CentralMoment(seq, Mean, 3);
                Kurtosis = CentralMoment(seq, Mean, 4);
                Seq = seq;
            }

            public override string ToString() {
                return string.Format("Mean = {0}, Var = {1}, Skew = {2}, Kurtosis = {3}, Count = {4}", Mean, Var, Skew, Kurtosis, Count);
            }
        }

        public void SimilarityPatterns() {
            var timer = new NiceTimer("Getting all similarities");
            Console.Write("Processing: [");
            var sims = backingDB.RawSimilarTracks.Execute(true);
            Console.WriteLine("]");
            Console.WriteLine("Got {0} similarities", sims.Length);
            
            timer.TimeMark("Counting...");
            
            var tracks = (from sim in sims
                          from track in new[] { sim.TrackA, sim.TrackB }
                          select track).Distinct().ToArray();
            var trackCount = tracks.Length;
            Console.WriteLine("We have {0} unique tracks, maximum id being {1}", trackCount, tracks.Max());
            
            timer.TimeMark("renumbering");
            
            Array.Sort(tracks);
            Dictionary<int, int> renum = new Dictionary<int,int>();
            int i=0;
            foreach(var tracknum in tracks) 
                renum[tracknum] = i++;
            foreach (var sim in sims) {
                sim.TrackA = renum[sim.TrackA];
                sim.TrackB = renum[sim.TrackB];
            }
            tracks = null;//no longer relevant
            renum = null;//TODO: maybe keep this alive since it might allow reverse mapping?
            timer.TimeMark("Mapping...");


            SimilarTo[][] map = ReMap(trackCount, sims, s => s.TrackA, s => new SimilarTo { track = s.TrackB, rating = s.Rating });

            timer.TimeMark("Finding overlapping reachabilities");

            List<SimTrans> retval = new List<SimTrans>();
            float[] refBuf = new float[trackCount];
            Random rnd = new Random((int)(DateTime.UtcNow.Ticks/128));//just dividing since the windows times isn't that accurate anyhow.
            foreach (int trackId in Enumerable.Range(0, trackCount)) {
                foreach (SimilarTo reachable in map[trackId]) refBuf[reachable.track] = reachable.rating;

                foreach (SimilarTo reachable in map[trackId])
                    foreach (SimilarTo transReachable in map[reachable.track])
                        if (refBuf[transReachable.track] > 0 && rnd.Next()%10==0) //hit!
                            retval.Add(new SimTrans { rateAB = reachable.rating, rateBC = transReachable.rating, rateAC = refBuf[transReachable.track] });

                foreach (SimilarTo reachable in map[trackId]) refBuf[reachable.track] = -1;
            }
            Console.WriteLine("Found {0} A->B->C whilst A->C",retval.Count);

            timer.TimeMark("Calculating statistics");

            var ratingsWhole=sims.Select(str=>str.Rating);
             var   ratingsAB=retval.Select(r=>r.rateAB);
             var   ratingsBC = retval.Select(r => r.rateBC);
             var   ratingsAC=retval.Select(r=>r.rateAC);

            Statistics whole = new Statistics( ratingsWhole),
                rateAB=new Statistics( ratingsAB),
                rateBC=new Statistics( ratingsBC),
                rateAC=new Statistics( ratingsAC);



            Console.WriteLine("Ratings: {0}", whole.ToString());
            Console.WriteLine("Rate A->B: {0}", rateAB.ToString());
            Console.WriteLine("Rate B->C: {0}", rateBC.ToString());
            Console.WriteLine("Rate A->C: {0}", rateAC.ToString());

            Console.WriteLine("Cov(AB,BC): {0}",Covariance(rateAB,rateBC));
            Console.WriteLine("Cov(AB,AC): {0}",Covariance(rateAB,rateAC));
            Console.WriteLine("Cov(BC,AC): {0}",Covariance(rateBC,rateAC));

            Func<float,float,float>
              sum= (a,b)=>a+b,
              mul= (a,b)=>a*b,
              max= (a,b)=>Math.Max(a,b),
              sqrsum= (a,b)=>a*a+b*b,
              sqrtsum= (a,b)=>(float)(Math.Sqrt(a)+Math.Sqrt(b)),
              euclidian = (a, b) => (float)Math.Sqrt(a * a + b * b);
            

            //OK mistaken assumption here.  I'm doing statistics and looking at covariance over whole sets of songs, when it should be only ratings of a particular song.
            //That's why Cov(AB,AC) is so high - generally one artist has a bunch of similarities in a similar range.
            //So, as a hack, I could normalize mean and variance per artists... but... that's just weird, and wouldn't be right... so what to do?

            Statistics
                statSum = new Statistics(retval, sum),
                statMul = new Statistics(retval, mul),
                statMax = new Statistics(retval, max),
                statSqrSum = new Statistics(retval, sqrsum),
                statSqrtSum = new Statistics(retval, sqrtsum),
                statEuclidian = new Statistics(retval, euclidian);

            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "sum", Covariance(statSum, rateAC), statSum);
            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "mul", Covariance(statMul, rateAC), statMul);
            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "max", Covariance(statMax, rateAC), statMax);
            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "sqrsum", Covariance(statSqrSum, rateAC), statSqrSum);
            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "sqrtsum", Covariance(statSqrtSum, rateAC), statSqrtSum);
            Console.WriteLine("f={0}, Cov={1}, Stats={2}", "euclidian", Covariance(statEuclidian, rateAC), statEuclidian); 

            timer.TimeMark(null);
        }

        public void ConvertEncoding() { //TODO this should really be part of more advanced logic in EamonExtensionsLinq to guess and fix encodings.
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
                Console.Write("?");
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

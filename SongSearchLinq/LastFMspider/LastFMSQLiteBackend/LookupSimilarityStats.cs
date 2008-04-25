using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
    public class SimilarityStat {
        public SongRef SongRef;
        public DateTime? LookupTimestamp;
        public int TimesReferenced;
        public int TrackID;
    }

    public class LookupSimilarityStats : AbstractLfmCacheQuery {
        public LookupSimilarityStats(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
        protected override string CommandText {
            get {
                return @"SELECT TrackB, count(*) FROM SimilarTrack Group by TrackB";
            }
        }

        struct simstat {
            public int ID;
            public int Count;
        }

        public SimilarityStat[] Execute() {
            List<simstat> rates = new List<simstat>();
            using (var reader = CommandObj.ExecuteReader()) { //no transaction needed for a single select!
                while (reader.Read()) {
                    var id = (int)(long)reader[0];
                    var count = (int)(long)reader[1];
                    rates.Add(new simstat{ID=id,Count=count});
                }
            }
            Console.Write("[...]");
            var trackLookup = lfmCache.AllTracks.Execute().ToDictionary(ct=>ct.ID) ;
            return (from stat in rates
                    let cachedTrack = trackLookup[stat.ID]

                    select new SimilarityStat {
                        TrackID = stat.ID,
                        SongRef = cachedTrack.SongRef,
                        LookupTimestamp = cachedTrack.LookupTimestamp,
                        TimesReferenced = stat.Count
                    }).ToArray();
        }
    }
}


/*Performance discussion:

    We're essentially just grouping SimilarTrack by TrackB and then displaying some aggregate statistics.
 * 
 * There are two crucial speed bog downs, however.  Firstly, using subqueries is really important for group by.  
 * Something like the following just won't work fast enough:

SELECT S.TrackB, count(*)
FROM SimilarTrack S,Track T
WHERE T.TrackID=S.TrackB
GROUP BY S.TrackB
order by count(*) desc

 * This is because SQLite decides to join first and then group, which means it doesn't use the indexes efficiently. We need to remove the join on Track
 * So the following is "fast" being on the order of minutes on ten million similar tracks:
 * 

SELECT S.TrackB, count(*)
FROM SimilarTrack S
GROUP BY S.TrackB
order by count(*) desc
 * 
 * This should be used as a sub-clause in the select to speed things up; so we then have:


SELECT A.FullArtist, T.FullTitle, sub.refcount
FROM (
   SELECT TrackB, count(*) as 'refcount'
   FROM SimilarTrack
   GROUP BY TrackB
   ) sub, Track T,   Artist A
WHERE T.TrackID = sub.TrackB
AND A.ArtistID = T.ArtistID
ORDER BY sub.refcount DESC
 * 
 * This is fast "enough" too; not snappy, but doesn't need to run overnight either.  
 * This takes 60 seconds for a particular dataset.
 * 
 * A number of small things which reduce the data set size can speed things up.
 * Firstly, we want primarily large counts.  Certainly counts of 1 are not interesting...
 * Adding a HAVING count(*) reduces the crucial inner group by size.
 * 
 * Then, we check to make sure that the timestamp is null, we're interested only in those things we haven't already downloaded.
 * 
 * Finally, we add a LIMIT, just to reduce the data size.  this too speeds things up a little.
 * 
 * We could remove the order by, which definitely helps, but that's actually useful.  Raising the threshhold beyond 1 has little effect.
 * 
 * The final result is as follows, taking about 15 seconds - a factor 4 faster.

SELECT A.FullArtist, T.FullTitle, sub.refcount
FROM (
   SELECT TrackB, count(*) as 'refcount'
   FROM SimilarTrack
   GROUP BY TrackB
   HAVING count(*)>1
   ) sub, Track T,   Artist A
WHERE T.TrackID = sub.TrackB
AND T.LookupTimestamp IS NULL
AND A.ArtistID = T.ArtistID
ORDER BY sub.refcount DESC
LIMIT 1000
 * 
 * SQLite should process the group by first; it's the most crucial.
 * 
 * There's a second BIG thing you can't do fast; and that's check all the ratings.  That just takes long. Any reference to the ratings is much more work for SQLite than just counting.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using EmnExtensions;
using System.Text.RegularExpressions;

namespace SimilarityMdsLib
{
    static class BillboardByMdsId
    {
        public static Dictionary<int, SongRef> TracksByMdsId(CachedDistanceMatrix cachedMatrix) {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Loading TrackMapper");
            TrackMapper trainingMapper = cachedMatrix.Settings.LoadTrackMapper();
            timer.TimeMark("Loading Billboard tracks");
            var songrefBySqliteId = WellKnownTracksBySqliteId(cachedMatrix.Settings.Tools);

            var q =  //combines cachedMatrix.Mapping and trainingMapper and songrefBySqliteId to tuples (mdsId, songref)
                from denseID in cachedMatrix.Mapping.CurrentlyMapped
                let sqliteID = trainingMapper.LookupSqliteID(denseID)
                where songrefBySqliteId.ContainsKey(sqliteID)
                select new {
                    MdsId = cachedMatrix.Mapping.GetMap(denseID),
                    Song = songrefBySqliteId[sqliteID]
                };

            var retval = q.ToDictionary(kvp => kvp.MdsId, kvp => kvp.Song);
            timer.Done();
            return retval;
        }

        static Regex wellknown = new Regex(@"(billboard|top100)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static IEnumerable<KeyValuePair<int, SongRef>> FindWellKnown(LastFmTools tools) {

            return
                from songref in
                    (
                        from songdata in tools.DB.Songs
                        where wellknown.IsMatch(songdata.SongPath)
                        select SongRef.Create(songdata)).Distinct()
                where songref != null
                let trackID = tools.SimilarSongs.backingDB.LookupTrackID.Execute(songref)
                where trackID.HasValue
                select new KeyValuePair<int, SongRef>(trackID.Value, songref);
        }

        private static Dictionary<int, SongRef> WellKnownTracksBySqliteId(LastFmTools tools) {
            return FindWellKnown(tools).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

    }
}

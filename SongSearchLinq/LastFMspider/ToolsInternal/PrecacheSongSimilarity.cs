using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;

namespace LastFMspider
{
    internal static partial class ToolsInternal
    {

        public static int PrecacheSongSimilarity(LastFmTools tools) {
            var SimilarSongs= tools.SimilarSongs;
            int songsCached = 0;
            Console.WriteLine("Finding songs without similarities");
            var tracksToGo = SimilarSongs.backingDB.TracksWithoutSimilarityList.Execute(900000);
#if !DEBUG
            tracksToGo.Shuffle();
#endif
            tracksToGo = tracksToGo.Take(300000).ToArray();
            Console.WriteLine("Looking up similarities for {0} tracks...", tracksToGo.Length);
            Parallel.ForEach(tracksToGo, new ParallelOptions { MaxDegreeOfParallelism = 10 }, track => {
                StringBuilder msg = new StringBuilder();
                try {
                    string trackStr = track.SongRef.ToString();
                    msg.AppendFormat("SimTo:{0,-30}", trackStr.Substring(0, Math.Min(trackStr.Length, 30)));
                    TrackSimilarityListInfo listStatus = SimilarSongs.backingDB.LookupSimilarityListAge.Execute(track.SongRef);
                    if (listStatus.LookupTimestamp.HasValue) {
                        msg.AppendFormat("done.");
                    } else {
                        var newEntry = OldApiClient.Track.GetSimilarTracks(track.SongRef);
                        msg.AppendFormat("={0,3} ", newEntry.similartracks.Length);
                        if (newEntry.similartracks.Length > 0)
                            msg.AppendFormat("{1}: {0}", newEntry.similartracks[0].similarsong.ToString().Substring(0, Math.Min(newEntry.similartracks[0].similarsong.ToString().Length, 30)), newEntry.similartracks[0].similarity);

                        SimilarSongs.backingDB.InsertSimilarityList.Execute(newEntry);
                        lock (tracksToGo)
                            songsCached++;
                    }
                } catch (Exception e) {
                    try {
                        SimilarSongs.backingDB.InsertSimilarityList.Execute(SongSimilarityList.CreateErrorList(track.SongRef, 1));//unknown error => code 1
                        lock (tracksToGo)
                            songsCached++;
                    } catch (Exception ee) { Console.WriteLine(ee.ToString()); }
                    msg.AppendFormat("\n{0}: {1}\n", e.GetType().Name, e.Message);
                } finally {
                    Console.WriteLine(msg);
                }
            });
            return songsCached;

        }
    }
}

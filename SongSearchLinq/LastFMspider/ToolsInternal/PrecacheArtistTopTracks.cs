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

        public static int PrecacheArtistTopTracks(LastFmTools tools) {
            var SimilarSongs = tools.SimilarSongs;
            int artistsCached = 0;
            Console.WriteLine("Finding artists without toptracks");
            var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutTopTracksList.Execute(1000000);
#if !DEBUG
            artistsToGo.Shuffle();
#endif
            artistsToGo = artistsToGo.Take(100000).ToArray();
            Console.WriteLine("Looking up top-tracks for {0} artists...", artistsToGo.Length);
            Parallel.ForEach(artistsToGo, artist => {
                StringBuilder msg = new StringBuilder();

                try {
                    msg.AppendFormat("TopOf:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
                    ArtistQueryInfo info = SimilarSongs.backingDB.LookupArtistTopTracksListAge.Execute(artist.ArtistName);
                    if (info.LookupTimestamp.HasValue || info.IsAlternateOf.HasValue) {
                        msg.AppendFormat("done.");
                    } else {

                        var newEntry = OldApiClient.Artist.GetTopTracks(artist.ArtistName);
                        msg.AppendFormat("={0,3} ", newEntry.TopTracks.Length);
                        if (newEntry.TopTracks.Length > 0)
                            msg.AppendFormat("{1}: {0}", newEntry.TopTracks[0].Track.Substring(0, Math.Min(newEntry.TopTracks[0].Track.Length, 30)), newEntry.TopTracks[0].Reach);

                        if (artist.ArtistName.ToLatinLowercase() != newEntry.Artist.ToLatinLowercase())
                            SimilarSongs.backingDB.SetArtistAlternate.Execute(artist.ArtistName, newEntry.Artist);

                        SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(newEntry);
                        lock (artistsToGo)
                            artistsCached++;
                    }
                } catch (Exception e) {
                    try {
                        SimilarSongs.backingDB.InsertArtistTopTracksList.Execute(ArtistTopTracksList.CreateErrorList(artist.ArtistName, 1));
                        lock (artistsToGo)
                            artistsCached++;
                    } catch (Exception ee) { Console.WriteLine(ee.ToString()); }
                    msg.AppendFormat("\n{0}: {1}\n", e.GetType().Name, e.Message);
                } finally {
                    Console.WriteLine(msg);
                }
            });
            return artistsCached;
        }
    }
}

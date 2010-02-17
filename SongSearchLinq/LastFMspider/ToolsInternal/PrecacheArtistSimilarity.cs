﻿using System;
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

        public static int PrecacheArtistSimilarity(LastFmTools tools)
        {
            var SimilarSongs = tools.SimilarSongs;
			DateTime minAge = DateTime.UtcNow - TimeSpan.FromDays(365.0);

			int artistsCached = 0;
			Console.WriteLine("Finding artists without similarities");
			var artistsToGo = SimilarSongs.backingDB.ArtistsWithoutSimilarityList.Execute(1000000, minAge );
#if !DEBUG
			artistsToGo.Shuffle();
#endif
			artistsToGo = artistsToGo.Take(100000).ToArray();
			Console.WriteLine("Looking up similarities for {0} artists...", artistsToGo.Length);
			Parallel.ForEach(artistsToGo, artist => {
				StringBuilder msg = new StringBuilder();
				try {
					msg.AppendFormat("SimTo:{0,-30}", artist.ArtistName.Substring(0, Math.Min(artist.ArtistName.Length, 30)));
					ArtistQueryInfo info = SimilarSongs.backingDB.LookupArtistSimilarityListAge.Execute(artist.ArtistName);
					if ((info.LookupTimestamp.HasValue && info.LookupTimestamp.Value > minAge ) || info.IsAlternateOf.HasValue) {
						msg.AppendFormat("done.");
					} else {
						ArtistSimilarityList newEntry = OldApiClient.Artist.GetSimilarArtists(artist.ArtistName);
						msg.AppendFormat("={0,3} ", newEntry.Similar.Length);
						if (newEntry.Similar.Length > 0)
							msg.AppendFormat("{1}: {0}", newEntry.Similar[0].Artist.Substring(0, Math.Min(newEntry.Similar[0].Artist.Length, 30)), newEntry.Similar[0].Rating);

						if (artist.ArtistName.ToLatinLowercase() != newEntry.Artist.ToLatinLowercase())
							SimilarSongs.backingDB.SetArtistAlternate.Execute(artist.ArtistName, newEntry.Artist);
						SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(newEntry);
						lock (artistsToGo) artistsCached++;
					}
				} catch (Exception e) {
					try {
						SimilarSongs.backingDB.InsertArtistSimilarityList.Execute(ArtistSimilarityList.CreateErrorList(artist.ArtistName, 1));
						lock (artistsToGo) artistsCached++;
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

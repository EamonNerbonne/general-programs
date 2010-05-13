using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;
using System.Data;

namespace LastFMspider.LastFMSQLiteBackend {

	public class LookupArtistSimilarityList : AbstractLfmCacheOperation {
		public LookupArtistSimilarityList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
		public ArtistSimilarityList Execute(ArtistSimilarityListInfo list) {
			if (!list.ListID.HasValue) return null;

			lock (SyncRoot) {
				using (var trans = Connection.BeginTransaction()) {
					var similarto =
						from sim in list.SimilarArtists
						select new SimilarArtist { 
							 Rating = (float) sim.Similarity,
							 Artist =  lfmCache.LookupArtist.Execute(sim.OtherId)
						};

					return new ArtistSimilarityList {
						 Artist = list.ArtistInfo.Artist,
						Similar = similarto.ToArray(),						
						LookupTimestamp = list.LookupTimestamp.Value,
						StatusCode = list.StatusCode,
						
					};
				}
			}
		}
	}
}

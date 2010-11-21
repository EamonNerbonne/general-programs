using System.Linq;

namespace LastFMspider.LastFMSQLiteBackend {

	public class LookupArtistSimilarityList : AbstractLfmCacheOperation {
		public LookupArtistSimilarityList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }
		public ArtistSimilarityList Execute(ArtistSimilarityListInfo list) {
			if (!list.ListID.HasValue) return null;

			return DoInLockedTransaction(() => {
				var similarto =
							from sim in list.SimilarArtists
							select new SimilarArtist {
								Rating = sim.Similarity,
								Artist = lfmCache.LookupArtist.Execute(sim.OtherId)
							};

				return new ArtistSimilarityList {
					Artist = list.ArtistInfo.Artist,
					Similar = similarto.ToArray(),
					LookupTimestamp = list.LookupTimestamp.Value.ToUniversalTime(),
					StatusCode = list.StatusCode,
				};
			});
		}
	}
}

using System;
using LastFMspider.OldApi;

namespace LastFMspider {
	internal static partial class ToolsInternal {
		public static ArtistSimilarityList LookupSimilarArtists(LastFMSQLiteCache LastFmCache, string artist, TimeSpan maxAge = default(TimeSpan)) {
			if (maxAge == default(TimeSpan)) maxAge = NormalMaxAge;
			var simartistInfo = LastFmCache.LookupArtistSimilarityListAge.Execute(artist);
			if (simartistInfo.ListID.HasValue && simartistInfo.LookupTimestamp.HasValue && simartistInfo.LookupTimestamp.Value >= DateTime.UtcNow - maxAge)
				return LastFmCache.LookupArtistSimilarityList.Execute(simartistInfo);
			if (simartistInfo.ArtistInfo.IsAlternateOf.HasValue)
				return LookupSimilarArtists(LastFmCache,LastFmCache.LookupArtist.Execute(simartistInfo.ArtistInfo.IsAlternateOf), maxAge);

			ArtistSimilarityList simartists;
			try {
				simartists = OldApiClient.Artist.GetSimilarArtists(artist);
			} catch (Exception) {
				simartists = ArtistSimilarityList.CreateErrorList(artist, 1);//TODO:statuscodes...
			}
			if (artist.ToLatinLowercase() != simartists.Artist.ToLatinLowercase())
				LastFmCache.SetArtistAlternate.Execute(artist, simartists.Artist);
			LastFmCache.InsertArtistSimilarityList.Execute(simartists);
			return simartists;
		}
	}
}

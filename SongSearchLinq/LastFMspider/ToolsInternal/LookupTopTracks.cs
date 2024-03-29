﻿using System;
using LastFMspider.OldApi;
using SongDataLib;

namespace LastFMspider {
	internal static partial class ToolsInternal {
		static readonly TimeSpan NormalMaxAge = TimeSpan.FromDays(90.0);

		public static ArtistTopTracksList LookupTopTracks(LastFMSQLiteCache LastFmCache, string artist, TimeSpan maxAge = default(TimeSpan)) {
			//artist = artist.ToLatinLowercase();
			if (maxAge == default(TimeSpan)) maxAge = NormalMaxAge;
			var toptracksInfo = LastFmCache.LookupArtistTopTracksListInfo.Execute(artist);
			if (toptracksInfo.IsKnown && toptracksInfo.LookupTimestamp.HasValue && toptracksInfo.LookupTimestamp.Value >= DateTime.UtcNow - maxAge)
				return LastFmCache.LookupArtistTopTracksList.Execute(toptracksInfo);
			if (toptracksInfo.ArtistInfo.IsAlternateOf.HasValue)
				return LookupTopTracks(LastFmCache, LastFmCache.LookupArtist.Execute(toptracksInfo.ArtistInfo.IsAlternateOf), maxAge);

            return ArtistTopTracksList.CreateErrorList(artist, 1);//TODO:statuscodes...

            ArtistTopTracksList toptracks;
			try {
				toptracks = _OldApiClient.Artist.GetTopTracks(artist);
			} catch (Exception) {
				toptracks = ArtistTopTracksList.CreateErrorList(artist, 1);//TODO:statuscodes...
			}
			LastFmCache.DoInLockedTransaction(() => {
				if (artist.ToLatinLowercase() != toptracks.Artist.ToLatinLowercase())
					LastFmCache.SetArtistAlternate.Execute(artist, toptracks.Artist);
				LastFmCache.InsertArtistTopTracksList.Execute(toptracks);
			});
			return toptracks;
		}
	}
}

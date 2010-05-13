﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtistTopTracksList : AbstractLfmCacheOperation {
		public LookupArtistTopTracksList(LastFMSQLiteCache lfmCache) : base(lfmCache) { }

		public ArtistTopTracksList Execute(ArtistTopTracksListInfo artistTTinfo) {
			lock (SyncRoot) {

				using (var trans = Connection.BeginTransaction()) {

					var rankings =
						from toptrack in artistTTinfo.TopTracks
						select new ArtistTopTrack {
							Reach = toptrack.Reach,
							Track = lfmCache.LookupTrack.Execute(toptrack.ForId).Title,
						};

					return new ArtistTopTracksList {
						TopTracks = rankings.ToArray(),
						Artist = artistTTinfo.ArtistInfo.Artist,
						LookupTimestamp = artistTTinfo.LookupTimestamp.Value,
						StatusCode = artistTTinfo.StatusCode,
					};
				}
			}
		}


	}
}

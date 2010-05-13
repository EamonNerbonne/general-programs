using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public struct TrackSimilarityListInfo {
		public readonly SongRef SongRef;
		public readonly SimilarTracksListId ListID;
		public readonly DateTime? LookupTimestamp;
		public readonly int? StatusCode;
		public TrackSimilarityListInfo(SongRef songref, SimilarTracksListId listID, DateTime? lookupTimestamp, int? statusCode) { this.SongRef = songref; this.ListID = listID; this.LookupTimestamp = lookupTimestamp; this.StatusCode = statusCode; }
	}
	public class LookupSimilarityListAge : AbstractLfmCacheQuery {
		public LookupSimilarityListAge(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			lowerArtist = DefineParameter("@lowerArtist");
			lowerTitle = DefineParameter("@lowerTitle");
		}
		protected override string CommandText {
			get {
				return @"
SELECT L.LookupTimestamp, L.StatusCode, L.ListID
FROM Artist A,
     Track T, 
     SimilarTrackList L
WHERE A.LowercaseArtist = @lowerArtist
AND T.ArtistID=A.ArtistID
AND T.LowercaseTitle = @lowerTitle
AND L.ListID = T.CurrentSimilarTrackList
";
			}
		}
		DbParameter lowerTitle, lowerArtist;

		public TrackSimilarityListInfo Execute(SongRef songref) {
			lock (SyncRoot) {

				lowerArtist.Value = songref.Artist.ToLatinLowercase();
				lowerTitle.Value = songref.Title.ToLatinLowercase();
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read())
						return new TrackSimilarityListInfo(songref, new SimilarTracksListId(reader[2].CastDbObjectAs<long?>()), reader[0].CastDbObjectAsDateTime().Value, (int?)reader[1].CastDbObjectAs<long?>());
					else
						return new TrackSimilarityListInfo(songref, default(SimilarTracksListId), null, null);
				}
			}
		}
	}
}

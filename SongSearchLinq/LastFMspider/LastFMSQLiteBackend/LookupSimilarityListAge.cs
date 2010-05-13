using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupSimilarityListAge : AbstractLfmCacheQuery {
		public LookupSimilarityListAge(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			lowerArtist = DefineParameter("@lowerArtist");
			lowerTitle = DefineParameter("@lowerTitle");
		}
		protected override string CommandText {
			get {
				return @"
SELECT L.ListID, L.TrackId, L.LookupTimestamp, L.StatusCode, L.SimilarTracks
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
						return new TrackSimilarityListInfo(
							listID:new SimilarTracksListId((long)reader[0]),
							trackId: new TrackId((long)reader[1]),
							songref:songref,
							lookupTimestamp:  reader[2].CastDbObjectAsDateTime().Value,
							statusCode: (int?)reader[3].CastDbObjectAs<long?>(),
							sims: reader[4].CastDbObjectAs<byte[]>());
					else
												return new TrackSimilarityListInfo(
							listID:new SimilarTracksListId((long)reader[0]),
							trackId: new TrackId((long)reader[1]),
							songref:songref,
							lookupTimestamp:  reader[2].CastDbObjectAsDateTime().Value,
							statusCode: (int?)reader[3].CastDbObjectAs<long?>(),
							sims: reader[4].CastDbObjectAs<byte[]>());

						return TrackSimilarityListInfo.CreateUnknown(songref);
				}
			}
		}
	}
}

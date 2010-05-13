using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtistTopTracksList : AbstractLfmCacheQuery {
		public InsertArtistTopTracksList(LastFMSQLiteCache lfm)
			: base(lfm) {
			lowerArtist = DefineParameter("@lowerArtist");
			lookupTimestamp = DefineParameter("@lookupTimestamp");
			statusCode = DefineParameter("@statusCode");
		}
		protected override string CommandText {
			get {
				return @"
INSERT INTO [TopTracksList] (ArtistID, LookupTimestamp,StatusCode) 
SELECT A.ArtistID, (@lookupTimestamp) AS LookupTimestamp, (@statusCode) AS StatusCode
FROM Artist A
WHERE A.LowercaseArtist = @lowerArtist;

SELECT L.ListID,A.ArtistID
FROM TopTracksList L, Artist A
WHERE A.LowercaseArtist = @lowerArtist
AND L.ArtistID = A.ArtistID
AND L.LookupTimestamp = @lookupTimestamp
";
			}
		}

		DbParameter lowerArtist, lookupTimestamp, statusCode;


		public void Execute(ArtistTopTracksList toptracksList) {
			lock (SyncRoot) {
				using (DbTransaction trans = Connection.BeginTransaction()) {
					lfmCache.InsertArtist.Execute(toptracksList.Artist);
					TopTracksListId listID;
					ArtistId artistID;
					lowerArtist.Value = toptracksList.Artist.ToLatinLowercase();
					lookupTimestamp.Value = toptracksList.LookupTimestamp.Ticks;
					statusCode.Value = toptracksList.StatusCode;
					using (var reader = CommandObj.ExecuteReader()) {
						if (reader.Read()) { //might need to do reader.NextResult();
							listID = new TopTracksListId((long)reader[0]);
							artistID = new ArtistId((long)reader[1]);
						} else {
							throw new Exception("Command failed???");
						}
					}

					if (toptracksList.LookupTimestamp > DateTime.Now - TimeSpan.FromDays(1.0))
						lfmCache.ArtistSetCurrentTopTracks.Execute(listID); //presume if this is recently downloaded, then it's the most current.



					foreach (var toptrack in toptracksList.TopTracks) {
						lfmCache.InsertArtistTopTrack.Execute(listID, artistID, toptrack.Track, toptrack.Reach);
						lfmCache.UpdateTrackCasing.Execute(
							SongRef.Create(toptracksList.Artist,
							toptrack.Track));
					}
					trans.Commit();
				}
			}
		}


	}
}

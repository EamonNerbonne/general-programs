using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertTrack : AbstractLfmCacheQuery {
		public InsertTrack(LastFMSQLiteCache lfm)
			: base(lfm) {
			fullTitle = DefineParameter("@fullTitle");

			lowerTitle = DefineParameter("@lowerTitle");

			lowerArtist = DefineParameter("@lowerArtist");
		}
		protected override string CommandText {
			get {
				return @"
INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
SELECT ArtistID, @fullTitle, @lowerTitle FROM [Artist]
WHERE LowercaseArtist = @lowerArtist
;
SELECT TrackID FROM [Track] NATURAL join [Artist] WHERE LowercaseArtist = @lowerArtist AND LowercaseTitle = @lowerTitle
";
			}
		}
		DbParameter fullTitle, lowerTitle, lowerArtist;

		public TrackId Execute(SongRef songref) {
			lock (SyncRoot) {
				lfmCache.InsertArtist.Execute(songref.Artist);
				fullTitle.Value = songref.Title;
				lowerTitle.Value = songref.Title.ToLatinLowercase();
				lowerArtist.Value = songref.Artist.ToLatinLowercase();
				using (DbTransaction trans = Connection.BeginTransaction()) {
					var retval  =  new TrackId((long)CommandObj.ExecuteScalar());
					trans.Commit();
					return retval;
				}
			}
		}

	}
}

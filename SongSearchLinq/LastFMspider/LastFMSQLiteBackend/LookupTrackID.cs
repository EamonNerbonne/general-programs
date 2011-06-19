using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupTrackID : AbstractLfmCacheQuery {
		public LookupTrackID(LastFMSQLiteCache lfm)
			: base(lfm) {
			lowerTitle = DefineParameter("@lowerTitle");

			lowerArtist = DefineParameter("@lowerArtist");
		}
		protected override string CommandText {
			get {
				return @"
SELECT TrackID FROM [Track] NATURAL join [Artist] WHERE LowercaseArtist = @lowerArtist AND LowercaseTitle = @lowerTitle
";
			}
		}
		readonly DbParameter lowerTitle, lowerArtist;

		public TrackId Execute(SongRef songref) {
			lock (SyncRoot) {

				lowerTitle.Value = songref.Title.ToLatinLowercase();
				lowerArtist.Value = songref.Artist.ToLatinLowercase();
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read()) {
						return new TrackId(reader[0].CastDbObjectAs<long>());
					} else
						return default(TrackId);
				}
			}
		}

	}
}

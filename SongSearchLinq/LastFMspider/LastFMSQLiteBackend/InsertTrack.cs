using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertTrack : AbstractLfmCacheQuery {
		public InsertTrack(LastFMSQLiteCache lfm)
			: base(lfm) {
			fullTitle = DefineParameter("@fullTitle");
			lowerTitle = DefineParameter("@lowerTitle");
			artistId = DefineParameter("@artistId");
		}
		protected override string CommandText {
			get {
				return @"
INSERT OR IGNORE INTO [Track] (ArtistID, FullTitle, LowercaseTitle)
VALUES (@artistId, @fullTitle, @lowerTitle);

SELECT TrackID FROM [Track] WHERE ArtistID=@artistId  AND LowercaseTitle = @lowerTitle
";
			}
		}

		readonly DbParameter fullTitle, lowerTitle, artistId;

		public TrackId Execute(SongRef songref) {
			return DoInLockedTransaction(() => {
				ArtistId artId = lfmCache.InsertArtist.Execute(songref.Artist);
				fullTitle.Value = songref.Title;
				lowerTitle.Value = songref.Title.ToLatinLowercase();
				artistId.Value = artId.Id;
				return new TrackId((long)CommandObj.ExecuteScalar());
			});
		}
	}
}

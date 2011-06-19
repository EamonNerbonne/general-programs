using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class SetArtistAlternate : AbstractLfmCacheQuery {
		public SetArtistAlternate(LastFMSQLiteCache lfm)
			: base(lfm) {
			lowerArtist = DefineParameter("@lowerArtist");
			lowerAltArtist = DefineParameter("@lowerAltArtist");
		}
		protected override string CommandText {
			get {
				return @"
UPDATE Artist 
SET 
	IsAlternateOf = (SELECT A.ArtistID FROM Artist A WHERE A.LowercaseArtist = @lowerAltArtist),
	CurrentSimilarArtistList = NULL,
	CurrentSimilarArtistListTimestamp = NULL,
	CurrentTopTracksList = NULL,
	CurrentTopTracksListTimestamp = NULL,
WHERE LowercaseArtist = @lowerArtist
";
			}
		}
		readonly DbParameter lowerArtist, lowerAltArtist;

		public void Execute(string artist, string isAlternateOfArtist) {
			DoInLockedTransaction(() => {
				lfmCache.InsertArtist.Execute(artist);
				lfmCache.InsertArtist.Execute(isAlternateOfArtist);
				lowerArtist.Value = artist.ToLatinLowercase();
				lowerAltArtist.Value = isAlternateOfArtist.ToLatinLowercase();
				CommandObj.ExecuteNonQuery();
			});
		}
	}
}

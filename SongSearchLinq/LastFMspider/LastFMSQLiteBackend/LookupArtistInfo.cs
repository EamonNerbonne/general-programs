using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public struct ArtistInfo {
		public string Artist;
		public ArtistId ArtistId;
		public ArtistId IsAlternateOf;
	}
	public class LookupArtistInfo : AbstractLfmCacheQuery {
		public LookupArtistInfo(LastFMSQLiteCache lfm)
			: base(lfm) {
			lowerArtist = DefineParameter("@lowerArtist");
		}
		protected override string CommandText {
			get {
				return @"
SELECT ArtistID, IsAlternateOf FROM [Artist] WHERE LowercaseArtist = @lowerArtist
";
			}
		}
		readonly DbParameter lowerArtist;

		public ArtistInfo Execute(string artist) {
			lock (SyncRoot) {

				lowerArtist.Value = artist.ToLatinLowercase();
				var res = CommandObj.ExecuteGetTopRow();
				return res == null
					? new ArtistInfo { Artist = artist }
					: new ArtistInfo { Artist = artist, ArtistId = new ArtistId((long)res[0]), IsAlternateOf = new ArtistId(res[1].CastDbObjectAs<long?>() ) };
			}
		}
	}
}

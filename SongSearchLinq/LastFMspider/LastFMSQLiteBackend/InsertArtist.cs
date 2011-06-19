using System.Data.Common;
using SongDataLib;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtist : AbstractLfmCacheQuery {
		protected override string CommandText {
			get { return @"
INSERT OR IGNORE INTO [Artist](FullArtist, LowercaseArtist) VALUES (@fullname,@lowername);
SELECT ArtistID FROM [Artist] WHERE LowercaseArtist = @lowername
"; }
		}
		public InsertArtist(LastFMSQLiteCache lfm)
			: base(lfm) {
			fullname = DefineParameter("@fullname");
			lowername = DefineParameter("@lowername");
		}

		readonly DbParameter fullname, lowername;
		public ArtistId Execute(string artist) {
			lock (SyncRoot) {
				fullname.Value = artist;
				lowername.Value = artist.ToLatinLowercase();
				return new ArtistId(CommandObj.ExecuteScalar().CastDbObjectAs<long>());
			}
		}
	}
}

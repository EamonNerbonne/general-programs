using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		DbParameter lowerArtist;

		public ArtistInfo Execute(string artist) {
			lock (SyncRoot) {

				this.lowerArtist.Value = artist.ToLatinLowercase();
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read())
						return new ArtistInfo { Artist = artist, ArtistId = new ArtistId((long)reader[0]), IsAlternateOf = new ArtistId((long)reader[1]) };
					else
						return new ArtistInfo { Artist = artist };
				}
			}
		}
	}
}

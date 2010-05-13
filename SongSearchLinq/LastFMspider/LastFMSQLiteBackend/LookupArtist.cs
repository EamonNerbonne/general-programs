using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupArtist : AbstractLfmCacheQuery {
		public LookupArtist(LastFMSQLiteCache lfm)
			: base(lfm) {
			artistID = DefineParameter("@artistID");
		}
		protected override string CommandText {
			get {
				return @"
SELECT FullArtist FROM [Artist] WHERE ArtistID = @artistID
";
			}
		}
		DbParameter artistID;

		public string Execute(ArtistId ArtistID) {
			lock (SyncRoot) {

				artistID.Value = ArtistID.Id;
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read()) {
						return (string)reader[0];
					} else
						return null;
				}
			}
		}

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class LookupTrack : AbstractLfmCacheQuery {
		public LookupTrack(LastFMSQLiteCache lfm)
			: base(lfm) {
			trackID = DefineParameter("@trackID");
		}
		protected override string CommandText {
			get {
				return @"
SELECT FullArtist, FullTitle FROM [Track] NATURAL join [Artist] WHERE TrackID = @trackID
";
			}
		}
		DbParameter trackID;

		public SongRef Execute(TrackId TrackID) {
			lock (SyncRoot) {

				trackID.Value = TrackID.Id;
				using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
					//we expect exactly one hit - or none
					if (reader.Read()) {
						return SongRef.Create((string)reader[0], (string)reader[1]);
					} else
						return null;
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public class InsertArtist : AbstractLfmCacheQuery {
		protected override string CommandText {
			get { return @"INSERT OR IGNORE INTO [Artist](FullArtist, LowercaseArtist) VALUES (@fullname,@lowername)"; }
		}
		public InsertArtist(LastFMSQLiteCache lfm)
			: base(lfm) {
			fullname = DefineParameter("@fullname");
			lowername = DefineParameter("@lowername");
		}

		DbParameter fullname, lowername;
		public void Execute(string artist) {
			lock (SyncRoot) {
				fullname.Value = artist;
				lowername.Value = artist.ToLatinLowercase();
				CommandObj.ExecuteNonQuery();
			}
		}

	}
}

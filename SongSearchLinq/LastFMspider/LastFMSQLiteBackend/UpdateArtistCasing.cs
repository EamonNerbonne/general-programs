using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class UpdateArtistCasing : AbstractLfmCacheQuery
    {
        public UpdateArtistCasing(LastFMSQLiteCache lfm)
            : base(lfm) {
            lowerArtist = DefineParameter("@lowerArtist");
            fullArtist = DefineParameter("@fullArtist");
        }
        protected override string CommandText {
            get {
                return @"
UPDATE Artist SET FullArtist = @fullArtist WHERE LowercaseArtist=@lowerArtist;
";
            }
        }


        DbParameter  lowerArtist,  fullArtist;


        public void Execute(string artistName) {
            lock (SyncRoot) {
                lowerArtist.Value = artistName.ToLatinLowercase();
                fullArtist.Value = artistName;
                CommandObj.ExecuteNonQuery();
            }
        }

    }
}

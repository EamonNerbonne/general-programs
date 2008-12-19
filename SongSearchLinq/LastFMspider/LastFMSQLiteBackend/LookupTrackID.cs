using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class LookupTrackID : AbstractLfmCacheQuery
    {
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
        DbParameter lowerTitle, lowerArtist;

        public int? Execute(SongRef songref) {
            lock (SyncRoot) {

                lowerTitle.Value = songref.Title.ToLatinLowercase();
                lowerArtist.Value = songref.Artist.ToLatinLowercase();
                using (var reader = CommandObj.ExecuteReader())//no transaction needed for a single select!
                {
                    //we expect exactly one hit - or none
                    if (reader.Read()) {
                        return (int)(long)reader[0];
                    } else
                        return null;
                }
            }
        }

    }
}

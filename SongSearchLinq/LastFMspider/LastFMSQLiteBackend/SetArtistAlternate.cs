using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class SetArtistAlternate : AbstractLfmCacheQuery
    {
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
        DbParameter lowerArtist, lowerAltArtist;

        public void Execute(string artist, string isAlternateOfArtist) {
            lock (SyncRoot) {
                using (var trans = Connection.BeginTransaction()) {
                    lfmCache.InsertArtist.Execute(artist);
                    lfmCache.InsertArtist.Execute(isAlternateOfArtist);
                    lowerArtist.Value = artist.ToLatinLowercase();
                    lowerAltArtist.Value = isAlternateOfArtist.ToLatinLowercase();
                    CommandObj.ExecuteNonQuery();
                    trans.Commit();
                }
            }
        }

    }
}

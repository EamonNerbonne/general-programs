using System.Data.Common;
using System;
namespace LastFMspider.LastFMSQLiteBackend {
    public class UpdateTrack : AbstractLfmCacheQuery {
        public UpdateTrack(LastFMSQLiteCache lfm)
            : base(lfm) {
            full = DefineParameter("@full");
            lower = DefineParameter("@lower");
            id = DefineParameter("@id");
            artistID = DefineParameter("@artistID");
            timestamp = DefineParameter("@timestamp");
        }
        protected override string CommandText {
            get { return @"UPDATE Track SET FullTitle=@full, LowercaseTitle=@lower, LookupTimestamp=@timestamp, ArtistID=@artistID WHERE TrackID=@id"; }
        }
        DbParameter full, lower, id,timestamp,artistID;
        public void Execute(TrackRow row) {
            full.Value = row.FullTitle;
            lower.Value = row.LowercaseTitle;
            id.Value = row.TrackID; 
            timestamp.Value = row.LookupTimestamp == null ? DBNull.Value : (object)row.LookupTimestamp.Value.Ticks;
            CommandObj.ExecuteNonQuery();
        }
    }
}

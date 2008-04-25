using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend {
    public class UpdateArtist : AbstractLfmCacheQuery {
        public UpdateArtist(LastFMSQLiteCache lfm) : base(lfm) {
            full=DefineParameter("@full");
            lower=DefineParameter("@lower");
            id=DefineParameter("@id");
        }
        protected override string CommandText {
            get { return @"UPDATE Artist SET FullArtist=@full, LowercaseArtist=@lower WHERE ArtistID=@id"; }
        }
        DbParameter full,lower,id;
        public void Execute(ArtistRow row) {
            full.Value = row.FullArtist;
            lower.Value = row.LowercaseArtist;
            id.Value = row.ArtistID;
            CommandObj.ExecuteNonQuery();
        }
    }
}

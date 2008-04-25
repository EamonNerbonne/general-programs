using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend {
    public class DeleteArtist : AbstractLfmCacheQuery {
        public DeleteArtist(LastFMSQLiteCache lfm)
            : base(lfm) {
            id = DefineParameter("@id");
        }
        protected override string CommandText {
            get { return @"
DELETE FROM SimilarTrack WHERE TrackB in (SELECT T.TrackID FROM Track T WHERE T.ArtistID=@id);   
DELETE FROM SimilarTrack WHERE TrackA in (SELECT T.TrackID FROM Track T WHERE T.ArtistID=@id);   
DELETE FROM Track WHERE ArtistID=@id;
DELETE FROM Artist WHERE ArtistID=@id;
"; }
        }
        DbParameter id;
        public void Execute(int artistID) {
            id.Value = artistID;
            using (var trans = Connection.BeginTransaction()) {
                CommandObj.ExecuteNonQuery();
                trans.Commit();
            }
        }
    }
}

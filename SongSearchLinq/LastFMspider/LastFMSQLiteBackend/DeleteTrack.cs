using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend {
    public class DeleteTrack : AbstractLfmCacheQuery {
        public DeleteTrack(LastFMSQLiteCache lfm)
            : base(lfm) {
            id = DefineParameter("@id");
        }
        protected override string CommandText {
            get {
                return @"
DELETE FROM SimilarTrack WHERE TrackB=@id;   
DELETE FROM SimilarTrack WHERE TrackA=@id;
DELETE FROM Track WHERE TrackID=@id;
";
            }
        }
        DbParameter id;
        public void Execute(int trackID) {
            id.Value = trackID;
            using (var trans = Connection.BeginTransaction()) {
                CommandObj.ExecuteNonQuery();
                trans.Commit();
            }
        }
    }
}

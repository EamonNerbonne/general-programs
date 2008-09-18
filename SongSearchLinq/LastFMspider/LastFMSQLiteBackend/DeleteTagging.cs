using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class DeleteTagging : AbstractLfmCacheQuery
    {
        public DeleteTagging(LastFMSQLiteCache lfm)
            : base(lfm)
        {
            id = DefineParameter("@id");
        }
        protected override string CommandText
        {
            get
            {
                return @"
DELETE FROM TrackTag WHERE TrackTagID = @id;   
";
            }
        }
        DbParameter id;
        public void Execute(int trackTagID)
        {
            id.Value = trackTagID;
            using (var trans = Connection.BeginTransaction())
            {
                CommandObj.ExecuteNonQuery();
                trans.Commit();
            }
        }
    }
}

using System.Data.Common;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class DeleteTag : AbstractLfmCacheQuery
    {
        public DeleteTag(LastFMSQLiteCache lfm)
            : base(lfm)
        {
            id = DefineParameter("@id");
        }
        protected override string CommandText
        {
            get
            {
                return @"
DELETE FROM TrackTag WHERE TagID in (SELECT T.TagID FROM Tag T WHERE T.TagID=@id);   
DELETE FROM Tag WHERE TagID=@id;
";
            }
        }
        DbParameter id;
        public void Execute(int tagID)
        {
            id.Value = tagID;
            using (var trans = Connection.BeginTransaction())
            {
                CommandObj.ExecuteNonQuery();
                trans.Commit();
            }
        }
    }
}

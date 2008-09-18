using System.Collections.Generic;
namespace LastFMspider.LastFMSQLiteBackend
{
    public class TagRow
    {

        public int TagID;
        public string LowercaseTag;

    }
    public class RawTags : AbstractLfmCacheQuery
    {
        public RawTags(LastFMSQLiteCache lfm) : base(lfm) { }
        protected override string CommandText
        {
            get { return @"SELECT TagID, LowercaseTag FROM Tag"; }
        }

        public TagRow[] Execute()
        {
            var tags = new List<TagRow>();
            using (var reader = CommandObj.ExecuteReader())
            {//no transaction needed for a single select!
                while (reader.Read())
                    tags.Add(new TagRow
                    {
                        TagID = (int)(long)reader[0],
                        LowercaseTag = (string)reader[1]
                    });
            }
            return tags.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class InsertTagging : AbstractLfmCacheQuery
    {
        public InsertTagging(LastFMSQLiteCache lfm)
            : base(lfm)
        {

            tagCount = DefineParameter("@tagcount");

            lowerArtist = DefineParameter("@lowerArtist");
            lowerTitle  = DefineParameter("@lowerTitle");

            lowerTag    = DefineParameter("@lowerTag");
        }
        protected override string CommandText
        {
            get
            {
                return @"
INSERT OR REPLACE INTO [TrackTag] (TagID, TrackID, TagCount) 
SELECT A.TrackID, B.TagID, (@tagCount) AS TagCount
FROM Track A, Tag B, Artist AsArtist, WHERE A.ArtistID = AsArtist.ArtistID 
  AND AsArtist.LowercaseArtist = @lowerArtist AND A.LowercaseTitle == @lowerTitle 
  AND B.LowercaseTag = @lowerTag 
";
            }
        }

        DbParameter lowerTitle, lowerArtist, lowerTag, tagCount;



    /*    public void Execute(SongRef songRef, TagRef tagRef, double tagCount)
        {
            this.tagCount.Value = tagCount;
            lowerArtist.Value = songRef.Artist.ToLowerInvariant();
            lowerTitle.Value  = songRef.Title.ToLowerInvariant();
            lowerTag.Value    = tagRef.Tag.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();
        }*/

    }
}

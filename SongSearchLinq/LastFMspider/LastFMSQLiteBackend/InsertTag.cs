using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public class InsertTag : AbstractLfmCacheQuery
    {
        protected override string CommandText
        {
            get { return @"INSERT OR IGNORE INTO [Tag](LowercaseTag) VALUES (@lowertag)"; }
        }
        public InsertTag(LastFMSQLiteCache lfm)
            : base(lfm)
        {
            lowername = DefineParameter("@lowername");
        }

        DbParameter lowername;
        public void Execute(string tag)
        {
            lowername.Value = tag.ToLowerInvariant();
            CommandObj.ExecuteNonQuery();
        }

    }
}

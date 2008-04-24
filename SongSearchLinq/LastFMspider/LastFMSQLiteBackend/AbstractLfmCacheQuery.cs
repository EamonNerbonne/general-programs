using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
    public abstract class AbstractLfmCacheQuery {
        readonly protected LastFMSQLiteCache lfmCache;
        readonly protected DbCommand CommandObj;
        protected DbConnection Connection { get { return lfmCache.Connection; } }
        public AbstractLfmCacheQuery(LastFMSQLiteCache lfmCache) {
            this.lfmCache = lfmCache;
            CommandObj = Connection.CreateCommand();
            CommandObj.CommandText = CommandText;
        }
        protected abstract string CommandText { get; }
        protected DbParameter DefineParameter(string name) {
            DbParameter param = CommandObj.CreateParameter();
            param.ParameterName = name;
            CommandObj.Parameters.Add(param);
            return param;
        }


    }
}

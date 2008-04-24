using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
    public abstract class AbstractLfmCacheQuery :AbstractLfmCacheOperation{
        readonly protected DbCommand CommandObj;
        public AbstractLfmCacheQuery(LastFMSQLiteCache lfmCache):base(lfmCache) {
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

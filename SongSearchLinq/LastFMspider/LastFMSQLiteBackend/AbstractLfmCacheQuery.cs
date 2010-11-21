using System.Data;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend
{
    public abstract class AbstractLfmCacheQuery : AbstractLfmCacheOperation
    {
        readonly protected DbCommand CommandObj;
        protected AbstractLfmCacheQuery(LastFMSQLiteCache lfmCache)
            : base(lfmCache) {
            CommandObj = Connection.CreateCommand();
			
            CommandObj.CommandText = CommandText;
        }
        protected abstract string CommandText { get; }
        protected DbParameter DefineParameter(string name,DbType type = DbType.Object) {
            DbParameter param = CommandObj.CreateParameter();
            param.ParameterName = name;
			param.DbType = type;
            CommandObj.Parameters.Add(param);
            return param;
        }
    }
}

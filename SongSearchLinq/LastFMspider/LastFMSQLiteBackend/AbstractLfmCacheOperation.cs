using System;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public abstract class AbstractLfmCacheOperation {
		readonly protected LastFMSQLiteCache lfmCache;
		protected object SyncRoot { get { return lfmCache.SyncRoot; } }
		protected DbConnection Connection { get { return lfmCache.Connection; } }
		protected AbstractLfmCacheOperation(LastFMSQLiteCache lfmCache) { this.lfmCache = lfmCache; }

		protected TOut DoInLockedTransaction<TOut>(Func<TOut> func) {
			lock (SyncRoot)
				using (var trans = Connection.BeginTransaction()) {
					TOut retval = func();
					trans.Commit();
					return retval;
				}
		}
		protected void DoInLockedTransaction(Action func) {
			lock (SyncRoot)
				using (var trans = Connection.BeginTransaction()) {
					func();
					trans.Commit();
				}
		}
	}
}

using System;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public abstract class AbstractLfmCacheOperation {
		readonly protected LastFMSQLiteCache lfmCache;
		protected object SyncRoot { get { return lfmCache.SyncRoot; } }
//		protected DbConnection Connection { get { return lfmCache.Connection; } }
		protected AbstractLfmCacheOperation(LastFMSQLiteCache lfmCache) { this.lfmCache = lfmCache; }

		protected TOut DoInLockedTransaction<TOut>(Func<TOut> func) { return lfmCache.DoInLockedTransaction(func); }
		protected void DoInLockedTransaction(Action action) { lfmCache.DoInLockedTransaction(action); }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace LastFMspider.LastFMSQLiteBackend {
	public abstract class AbstractLfmCacheOperation {
		readonly protected LastFMSQLiteCache lfmCache;
		protected object SyncRoot { get { return lfmCache.SyncRoot; } }
		protected DbConnection Connection { get { return lfmCache.Connection; } }
		public AbstractLfmCacheOperation(LastFMSQLiteCache lfmCache) { this.lfmCache = lfmCache; }


		protected TOut DoInTransaction<TOut>(Func<TOut> func) {
			using (var trans = Connection.BeginTransaction()) {
				TOut retval = func();
				trans.Commit();
				return retval;
			}
		}
		protected TOut DoInLockedTransaction<TOut>(Func<TOut> func) { lock (SyncRoot) return DoInTransaction(func); }

	}
}

//using System;
//using System.Collections.Concurrent;
//using System.Threading;
//using SongDataLib;

//namespace LastFMspider {
//    public class ConnectionHolder : IDisposable {
//        public readonly LastFMSQLiteCache DB;
//        readonly DbPool parent;
//        internal ConnectionHolder(DbPool parent, LastFMSQLiteCache db) { this.parent = parent; DB = db; }

//        public void Dispose() {
//            parent.dbs.Add(DB);
//            Thread.MemoryBarrier();
//            if (parent.dispose) parent.Dispose();
//        }
//    }

//    internal class DbPool : IDisposable {
//        internal bool dispose;
//        internal int created;
//        const int MAXCREATED = 4;
//        internal readonly ConcurrentBag<LastFMSQLiteCache> dbs = new ConcurrentBag<LastFMSQLiteCache>();
//        readonly SongDataConfigFile configFile;
//        public DbPool(SongDataConfigFile configFile) { this.configFile = configFile; }

//        public ConnectionHolder GetDb() {
//            LastFMSQLiteCache conn;
//            if (dbs.TryTake(out conn))
//                return new ConnectionHolder(this, conn);
//            else {
//                Interlocked.Increment(ref created);
//                return new ConnectionHolder(this, new LastFMSQLiteCache(configFile, true));
//            }
//        }

//        public void Dispose() {
//            dispose = true;
//            Thread.MemoryBarrier();
//            LastFMSQLiteCache conn;
//            while (dbs.TryTake(out conn))
//                conn.Dispose();
//        }
//    }
//}

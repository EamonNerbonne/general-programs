using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SQLite;

namespace LastFMspider.LastFMSQLiteBackend {
	internal class LoadOldSimList : AbstractLfmCacheQuery {
		public LoadOldSimList(LastFMSQLiteCache lfmCache)
			: base(lfmCache) {
			listIdA = DefineParameter("@listIdA");
			listIdB = DefineParameter("@listIdB");
		}
		protected override string CommandText {
			get {
				return @"
SELECT TT.ListID, TT.TrackID, TT.Reach
FROM TopTracks TT
WHERE TT.ListID >= @listIdA
AND TT.ListID < @listIdB
";
			}
		}
		DbParameter listIdA;
		DbParameter listIdB;

		struct entry { public uint listid; public uint elemid; public long reach;}
		public Tuple<ReachList<TrackId, TrackId.Factory>, TopTracksListId>[] Execute(uint listIdStart, uint listIdEnd) {
			List<entry> entries = new List<entry>();
			lock (SyncRoot) {
				using (var trans = Connection.BeginTransaction()) {
					listIdA.Value = listIdStart;
					listIdB.Value = listIdEnd;
					using (var reader = CommandObj.ExecuteReader()) {
						while (reader.Read())
							entries.Add(new entry {
								listid = (uint)(long)reader[0],
								elemid = (uint)(long)reader[1],
								reach = (long)reader[2]
							});
					}
				}
			}
			return
				(from e in entries
				 group e by e.listid into list
				 select
						Tuple.Create(
							new ReachList<TrackId, TrackId.Factory>(
								from e in list
								select new HasReach<TrackId>(new TrackId(e.elemid), e.reach)
							)
							, new TopTracksListId(list.Key)
						)
				).ToArray();
		}
	}
}

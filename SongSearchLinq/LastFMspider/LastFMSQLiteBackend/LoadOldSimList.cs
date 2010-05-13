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
SELECT S.ListID,S.ArtistB, S.Rating
FROM SimilarArtist S
WHERE S.ListID >= @listIdA
AND S.ListID < @listIdB
";
			}
		}
		DbParameter listIdA;
		DbParameter listIdB;

		struct entry { public uint listid; public uint elemid; public float rating;}
		public Tuple<SimilarityList<ArtistId,ArtistId.Factory>, SimilarArtistsListId>[] Execute(uint listIdStart, uint listIdEnd) {
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
								rating = (float)reader[2]
							});
					}
				}
			}
			return
				(from e in entries
				 group e by e.listid into list
				 select
						Tuple.Create(
							new SimilarityList<ArtistId,ArtistId.Factory>(
								from e in list
								select new SimilarityTo<ArtistId>(new ArtistId(e.elemid), e.rating)
							)
							, new SimilarArtistsListId(list.Key)
						)
				).ToArray();
		}
	}
}

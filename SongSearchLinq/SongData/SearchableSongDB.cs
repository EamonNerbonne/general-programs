using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SongDataLib
{
	public class SearchableSongDB
	{
		public ISongSearcher searchMethod;
		public SongDB db;
		public SearchableSongDB(SongDB db, ISongSearcher searchMethod) {
			this.db = db;
			this.searchMethod = searchMethod;
			searchMethod.Init(db);
		}

		public IEnumerable<ISongData> Search(string query) {
			return Matches(query).Select(i => db.songs[i]);
		}


		IEnumerable<int> Matches(string querystring) {
			byte[][] query =
					 querystring
					 .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					 .Select(q => SongUtil.CanonicalizedSearchStr(q))
					 .ToArray();
			if(query.Length == 0) return Enumerable.Range(0, db.songs.Length);
			SearchResult[] res = query.Select(q => searchMethod.Query(q)).ToArray();
			return MatchAll(res, query);
		}

		IEnumerable<int> MatchAll(SearchResult[] results, byte[][] queries) {
			Array.Sort(results, queries);
			IEnumerable<int> smallestMatch = results[0].songIndexes;


			foreach(int si in smallestMatch) {//TODO: use better set intersection logic.  Either enforce results to come in-order so you can "zip" em up, or use hashing.
				byte[] songtext = db.NormalizedSong(si);
				if(queries.Skip(1).All(q => songtext.Contains(q)))
					yield return si;
			}/**/
		}

	}
}

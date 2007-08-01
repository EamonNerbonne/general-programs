using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.Text;

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

		public IEnumerable<SongData> Search(string query) {
			return Matches(query).Select(i => db.songs[i]);
		}


		IEnumerable<int> Matches(string querystring) {
			string[] query = querystring.Split(' ').Select(q => Canonicalize.Basic(q)).ToArray();
			SearchResult[] res = query.Select(q => searchMethod.Query(q)).ToArray();
			return MatchAll(res, query);
		}

		IEnumerable<int> MatchAll(SearchResult[] results, string[] queries) {
			Array.Sort(results, queries);
			IEnumerable<int> smallestMatch = results[0].songIndexes;
			//queries are still in the "best" possible order!
			foreach(int si in smallestMatch) {
				string songtext = db.NormalizedSong(si);
				if(queries.Skip(1).All(q => songtext.Contains(q)))
					yield return si;
			}
		}

	}
}

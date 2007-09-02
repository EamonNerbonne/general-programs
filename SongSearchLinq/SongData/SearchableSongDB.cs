using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EamonExtensionsLinq.Text;
using System.Collections;

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
			string[] query = querystring.Split(new char[]{' '},StringSplitOptions.RemoveEmptyEntries).Select(q => Canonicalize.Basic(q)).ToArray();
			if(query.Length == 0) return Enumerable.Range(0, db.songs.Length);
			//*
			BitArray result = new BitArray(db.songs.Length, false), filter = new BitArray(db.songs.Length, true);
			foreach(string q in query.Take(query.Length-1)) {
				searchMethod.CompleteQuery(q, filter, result).songIndexes.Count();
				BitArray temp;
				temp = filter;
				filter = result;
				result = temp;
				result.SetAll(false);
			}
			return searchMethod.CompleteQuery(query[query.Length - 1], filter, result).songIndexes;
			/*/
			SearchResult[] res = query.Select(q => searchMethod.Query(q)).ToArray();
			return MatchAll(res, query);
			 /**/
		}

		IEnumerable<int> MatchAll(SearchResult[] results, string[] queries) {
			Array.Sort(results, queries);
			IEnumerable<int> smallestMatch = results[0].songIndexes;


			//queries are still in the "best" possible order!
			/*
			return results.Select(sr => sr.songIndexes).Aggregate((a, b) => SongUtil.ZipIntersect(a, b));
			/*/
			   foreach(int si in smallestMatch) {//TODO: use better set intersection logic.  Either enforce results to come in-order so you can "zip" em up, or use hashing.
				string songtext = db.NormalizedSong(si);
				if(queries.Skip(1).All(q => songtext.Contains(q)))
					yield return si;
			}/**/
		}

	}
}

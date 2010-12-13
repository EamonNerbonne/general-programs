using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SongDataLib {
	public class SearchableSongFiles {
		readonly ISongFileSearchEngine searchMethod;
		public readonly SongFilesSearchData db;
		public SearchableSongFiles(SongFilesSearchData db, ISongFileSearchEngine searchMethod) {
			this.db = db;
			this.searchMethod = searchMethod;
			if (searchMethod != null)
				searchMethod.Init(db);
		}

		public IEnumerable<ISongFileData> Search(string query) {
			return Matches(query).Select(i => db.songs[i]);
		}


		IEnumerable<int> Matches(string querystring) {
			byte[][] queries =
					 querystring
					 .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					 .Select(StringAsBytesCanonicalization.Canonicalize)
					 .ToArray();
			if (queries.Length == 0) return Enumerable.Range(0, db.songs.Length);
			if (searchMethod == null) {
				IBitapMatcher[] qMatchers = queries.OrderByDescending(q=>q.Length).Select(BitapSearch.MatcherFor).ToArray();
				return from songIndexAndBytes in db.AllNormalizedSongs
					   where qMatchers.All(qMatcher => qMatcher.BitapMatch(songIndexAndBytes.bytes))
					   select songIndexAndBytes.index;
			} else {
				SearchResult[] res = queries.Select(q => searchMethod.Query(q)).ToArray();
				return MatchAll(res, queries);
			}
		}

		IEnumerable<int> MatchAll(SearchResult[] results, byte[][] queries) {
			Array.Sort(results, queries);
			IEnumerable<int> smallestMatch = results[0].songIndexes;
			var otherQueries = queries.Skip(1).ToArray();
			//return SortedIntersectionAlgorithm.SortedIntersection(results.Select(result => result.songIndexes).ToArray(), true);

			return
				from si in smallestMatch
				let songtext = db.NormalizedSong(si)
				where otherQueries.All(q => songtext.Contains(q))
				select si;
		}
	}
}

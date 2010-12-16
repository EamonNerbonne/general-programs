using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

		public IEnumerable<ISongFileData> Search(string query, int[] rankmap = null) {
			return Matches(query, rankmap).Select(i => db.songs[i]);
		}

		static readonly Regex querySplitter = new Regex(@"\""[^""]*(\""|$)|\'[^']*(\'|$)|\S+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
		IEnumerable<int> Matches(string querystring, int[] rankmap) {
			byte[][] queries =
				querySplitter.Matches(querystring).Cast<Match>()
				.Select(m => m.Value.Trim('\'','\"'))
				.Where(s => s.Length > 0)
				 .Select(StringAsBytesCanonicalization.Canonicalize)
				 .ToArray();
			if (searchMethod == null || queries.Length == 0) {
				IBitapMatcher[] qMatchers = queries.OrderByDescending(q => q.Length).Select(BitapSearch.MatcherFor).ToArray();
				return
					db.AllNormalizedSongs
						.Where(songIndexAndBytes => qMatchers.All(qMatcher => qMatcher.BitapMatch(songIndexAndBytes.bytes)))
						.Select(songIndexAndBytes => songIndexAndBytes.index)
						.OrderBy(index => rankmap[index]);
			} else {
				SearchResult[] res = queries.Select(q => searchMethod.Query(q)).ToArray();
				return MatchAll(res, queries).OrderBy(index => rankmap[index]);
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

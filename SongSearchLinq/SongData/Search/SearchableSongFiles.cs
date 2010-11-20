using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SongDataLib
{
	public class SearchableSongFiles
	{
		public ISongFileSearchEngine searchMethod;
		public SongFilesSearchData db;
		public SearchableSongFiles(SongFilesSearchData db, ISongFileSearchEngine searchMethod) {
			this.db = db;
			this.searchMethod = searchMethod;
			searchMethod.Init(db);
		}

		public IEnumerable<ISongFileData> Search(string query) {
			return Matches(query).Select(i => db.songs[i]);
		}


		IEnumerable<int> Matches(string querystring) {
			byte[][] query =
					 querystring
					 .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
					 .Select(SongUtil.CanonicalizedSearchStr)
					 .ToArray();
			if(query.Length == 0) return Enumerable.Range(0, db.songs.Length);
			SearchResult[] res = query.Select(q => searchMethod.Query(q)).ToArray();
			return MatchAll(res, query);
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

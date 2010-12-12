using System.Collections.Generic;
using System.Linq;
using SongDataLib;

namespace SuffixTreeLib {

	public sealed class SuffixTreeLeafNode : ISuffixTreeNode {
		IList<Suffix> hits = new List<Suffix>();//in-order!

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits = hits.ToArray();
			return hits.Count;
		}

		public ISuffixTreeNode AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix) { hits.Add(suffix); return OptimalRep(sssm); }

		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm) { return sssm.db.GetSongIndexes(hits); }
		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if (query.Length == curdepth) {
				return new SearchResult { cost = hits.Count, songIndexes = GetAllSongs(sssm) };
			} else {//curdepth<query.Length
				return new SearchResult { cost = hits.Count / 5, songIndexes = FilterBy(sssm, curdepth, query).Distinct() };
			}
		}

		public IEnumerable<ISuffixTreeNode> Desc { get { yield return this; } }

		private IEnumerable<int> FilterBy(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			int songIndex = 0;
			foreach (Suffix suf in hits) {
				if (sssm.MatchSuffix(suf, query, curdepth)) {
					songIndex = sssm.db.GetSongIndex(suf, songIndex);
					yield return songIndex;
				}
			}
		}

		ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm) {
			if (hits.Count < 1000)
				return this;
			else {
				SuffixTreeNode newnode = new SuffixTreeNode();
				foreach (Suffix t in hits)
					newnode.AddSuffix(sssm, t); //always returns this, no need to update.
				return newnode;
			}
		}
	}
}

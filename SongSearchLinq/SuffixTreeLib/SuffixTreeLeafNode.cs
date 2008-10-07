using System.Collections.Generic;
using System.Linq;
using SongDataLib;

namespace EamonExtensionsLinq.Algorithms.SuffixTreeInternals
{

	public class SuffixTreeLeafNode : ISuffixTreeNode
	{
		internal List<Suffix> hits = new List<Suffix>();//in-order!

		public int CompactAndCalcCost(SuffixTreeSongSearcher sssm) {
			hits.Capacity = hits.Count;
			return hits.Count;
		}

		public void AddSuffix(SuffixTreeSongSearcher sssm, Suffix suffix) {
			hits.Add(suffix);
		}

		public IEnumerable<int> GetAllSongs(SuffixTreeSongSearcher sssm) {
			return sssm.db.GetSongIndexes(hits);
		}
		public SearchResult Match(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			if(query.Length == curdepth) {
				return new SearchResult { cost = hits.Count, songIndexes = GetAllSongs(sssm) };
			} else {//curdepth<query.Length
				return new SearchResult { cost = hits.Count / 5, songIndexes = FilterBy(sssm, curdepth, query).Distinct() };
			}
		}


		public IEnumerable<ISuffixTreeNode> Desc {
			get { yield return this; }
		}

		private IEnumerable<int> FilterBy(SuffixTreeSongSearcher sssm, int curdepth, byte[] query) {
			int songIndex = 0;
			foreach(Suffix suf in hits) {
				if(sssm.MatchSuffix(suf, query, curdepth)) {
					songIndex = sssm.db.GetSongIndex(suf, songIndex);
					yield return songIndex;
				}
			}
		}
		public ISuffixTreeNode OptimalRep(SuffixTreeSongSearcher sssm) {
			if(hits.Count < 1000) {
				return this;
			} else {
				SuffixTreeNode newnode = new SuffixTreeNode();
				newnode.AddSuffix(sssm, hits[0]);
				for(int hi = 1; hi < hits.Count; hi++) {
					newnode.AddSuffix(sssm, hits[hi]);
				}
				return newnode;
			}
		}
		public bool IsLeaf {
			get { return true; }
		}
		public IEnumerable<ISuffixTreeNode> Children {
			get { return Enumerable.Empty<ISuffixTreeNode>(); }
		}
		public IEnumerable<Suffix> LocalSuffixes {//in-order!
			get { return hits; }
		}
	}
}

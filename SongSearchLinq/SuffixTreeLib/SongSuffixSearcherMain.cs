using System.Collections.Generic;
using System.Linq;
//using  System.Query;
//using  System.Xml.XLinq;
//using  System.Xml;
using SongDataLib;

namespace SuffixTreeLib
{
	public class SuffixTreeSongSearcher : ISongFileSearchEngine
	{
		ISuffixTreeNode tree;
		public SongFilesSearchData db;
		byte[] normed;
		Suffix[] songBoundaries;

		public void Init(SongFilesSearchData db) {
			this.db = db;
			normed = db.NormedSongs;
			songBoundaries = db.SongBoundaries;
			tree = new SuffixTreeNode();


			for(Suffix suf = (Suffix)0; suf < songBoundaries[db.SongCount]; suf = suf.Next) {
				if(normed[(int)suf] == SongUtil.TERMINATOR) continue;
				tree.AddSuffix(this, suf);
			}
			tree.CompactAndCalcCost(this);
		}

		public SearchResult Query(byte[] query) {
			return tree.Match(this, 0, query);
		}

		public byte GetNormedChar(Suffix pos) { return normed[(int)pos]; }
		IEnumerable<byte> GetSuffixChars(Suffix suf) {
			int pos = (int)suf;
			while(normed[pos] != SongUtil.TERMINATOR) yield return normed[pos++];
		}
		public bool MatchSuffix(Suffix suf, byte[] query, int curpos) {
			return GetSuffixChars(suf).Take(query.Length - curpos).SequenceEqual(query.Skip(curpos));
		}

	}
}



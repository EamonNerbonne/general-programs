using System;
using System.Collections.Generic;
using System.Text;
//using System.Query;
//using System.Xml.XLinq;
//using System.Xml;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq.Filesystem;
using EamonExtensionsLinq;
using System.IO;
using System.Globalization;
using SongDataLib;
using System.Linq;

namespace SuffixTreeLib
{

	public class SuffixTreeSongSearcher : ISongSearcher
	{
		SuffixTree tree;
		SongDB db;
		byte[][] normed;

		public SuffixTreeSongSearcher() { }
		public void Init(SongDB db) {
			this.db = db;
			tree = new SuffixTree();
			normed = new byte[db.songs.Length][];

			for(int si = 0; si < db.songs.Length; si++) {
				byte[] buf = normed[si] = SongUtil.str2byteArr(db.NormalizedSong(si));
				for(int i = 0; i < buf.Length; i++)
					tree.AddSuffix(this, 0, new Suffix { songIndex = si, startPos = i });
			}
			/*long[] charcount = new long[256];
			foreach(byte[] norm in normed)
				foreach(byte letter in norm)
					charcount[letter]++;
			Console.WriteLine(string.Join("\n", charcount.Select((c, bt) => bt.ToString() + " \'" + ((char)bt) + "\' " + c).ToArray()));*/
			normed = null;
			tree.CompactAndCalcSize();

			var treeNodes = tree.Desc.ToArray();
			var leaves = treeNodes.Where(n=>n.children==null).ToArray();
			var nonleaves = treeNodes.Where(n=>n.children!=null).ToArray();
			Console.WriteLine("Number: " + treeNodes.Length);
			Console.WriteLine("Of which leaves: " + leaves.Length);
			Console.WriteLine("Of which nonleaves: " + nonleaves.Length);
			Console.WriteLine("Av Child#NonLeaf: " + nonleaves.Select(n => n.children.Where(c => c != null).Count()).Average());
			Console.WriteLine("Av Suf#Leaf: " + leaves.Select(n => n.hits.Count).Average());
			Console.WriteLine("Av Suf#NonLeaf: " + nonleaves.Select(n => n.hits.Count).Average());
			Console.WriteLine("Av Song#Leaf: " + leaves.Select(n => n.hits.Select(suf => suf.songIndex).Distinct().Count()).Average());
			Console.WriteLine("Av Song#NonLeaf: " + nonleaves.Select(n => n.hits.Select(suf => suf.songIndex).Distinct().Count()).Average());



		}

		public SearchResult Query(string strquery) {
			byte[] query = SongUtil.str2byteArr(strquery);
			return tree.Match(this, 0, query);
		}

		public byte[] GetNormSong(int si) {
			if(normed != null)
				return normed[si];
			else
				return SongUtil.str2byteArr(db.NormalizedSong(si));
		}
	}
}



using System;
using System.Collections.Generic;
using System.Text;
//using System.Query;
//using System.Xml.XLinq;
using System.Xml;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq.Filesystem;
using EamonExtensionsLinq;
using System.IO;
using System.Globalization;
using SongDataLib;

namespace SuffixTreeLib {

    public class SuffixTreeSongSearcher:ISongSearcher {
        SuffixTree tree;
        SongDB db;
        byte[][] normed;

        public SuffixTreeSongSearcher(){}
        public void Init(SongDB db) {
            this.db = db;
            tree = new SuffixTree();
            normed = new byte[db.songs.Length][];

            for (int si = 0; si < db.songs.Length; si++) {
                byte[] buf = normed[si] = db.NormalizedSong(si);
                SongData song = db.songs[si];
                for (int i = 0; i < buf.Length; i++)
                    tree.AddSuffix(this, 0, new Suffix { songIndex = si, startPos = i });
                si++;
            }
            normed = null;
            tree.CompactAndCalcSize();
        }

        public SearchResult Query(byte[] query) {
            return tree.Match(this, 0, query);
        }

        public byte[] GetNormSong(int si) {
            if (normed != null)
                return normed[si];
            else
                return db.NormalizedSong(si);
        }
    }
}

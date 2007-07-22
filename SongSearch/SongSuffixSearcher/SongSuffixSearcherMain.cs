using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Xml;
using EamonExtensions.DebugTools;
using EamonExtensions.Filesystem;
using EamonExtensions;
using System.IO;
using System.Globalization;
using SongDataLib;
using System.Xml.XPath;

namespace SongSuffixSearcher {

    public class SongSuffixSearcherMain:ISongSearcher {
        SuffixTree tree;
        NormalizerDelegate norm;
        SongDB db;
        byte[][] normed;

        public SongSuffixSearcherMain(){}
        public void Init(SongDB db) {
            tree = new SuffixTree();
            normed = new byte[db.songs.Length][];

            for (int si = 0; si < db.songs.Length; si++) {
                byte[] buf = normed[si] = norm(db.songs[si].FullInfo).ToArray();
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
                return norm(db.songs[si].FullInfo).ToArray();
        }
    }
}

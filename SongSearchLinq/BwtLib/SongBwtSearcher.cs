using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq.Filesystem;
using System.IO;
using System.Globalization;
using SongDataLib;

namespace BwtLib {

    public class SongBwtSearcher : ISongSearcher {
        SongDB db;
        int[] fl;
        int[] firstIndexOfByte;
        int[] fl0toSong;
        public SearchResult Query(byte[] query) {
            int start = 0, end = fl.Length;
            foreach (byte b in query.Reverse()) {//back to front...
                int newBstart = firstIndexOfByte[b];
                int newBend = firstIndexOfByte[b + 1];
                int resS = Array.BinarySearch<int>(fl, newBstart, newBend - newBstart, start);
                if (resS < 0)
                    resS = ~resS;
                int resE = Array.BinarySearch<int>(fl, newBstart, newBend - newBstart, end);
                if (resE < 0)
                    resE = ~resE;
                start = resS;
                end = resE;
            }
            return new SearchResult { cost = end -start, songIndexes = RangeToSongIndex(start, end).Distinct() };
        }

        private IEnumerable<int> RangeToSongIndex(int start, int end) {
            for (int i = start; i < end; i++) {
                yield return ResolveSong(i);
            }
        }
        private int ResolveSong(int bwtIndex) {
            while (bwtIndex < firstIndexOfByte[TERMINATOR] || bwtIndex >= firstIndexOfByte[TERMINATOR+1]) {
                bwtIndex = fl[bwtIndex];
            }
            return fl0toSong[bwtIndex-firstIndexOfByte[TERMINATOR]];
        }
        public SongBwtSearcher() { }
        public const byte TERMINATOR = (byte)(SongUtil.MAXCANONBYTE + 1);
        public void Init(SongDB db) {
            this.db = db;
            int songCount = db.songs.Length;
            byte[][] normed = db.NormalizedSongs.ToArray();
            List<byte> bigstring = new List<byte>();
            Dictionary<int, int> songEnds = new Dictionary<int, int>();
            for (int i = 0; i < songCount; i++) {
                foreach (byte b in normed[i])
                    bigstring.Add(b);
                bigstring.Add(TERMINATOR);//separator which can't occur in the original data
                songEnds[bigstring.Count - 1] = i;//store lookup from song-TERMINATOR position to songID
            }

            byte[] origdata = bigstring.ToArray();
            Console.WriteLine("converted to origdata[]");

            bigstring = null;
            int[] suffixes = Enumerable.Range(0, origdata.Length).ToArray();
            Console.WriteLine("made suffixarray, sorting:");
            Array.Sort<int>(suffixes, delegate(int a, int b) {//this is like REALLY REALLY slow, takes AGES, essentially all time is in here.
                while (true) {
                    int diff = (int)origdata[a] - (int)origdata[b];
                    if (diff != 0)
                        return diff;
                    if (diff == 0 && origdata[a] == TERMINATOR)
                        return 0;
                    a++;
                    b++;
                }
            });
            Console.WriteLine("sorted suffixarray");

            firstIndexOfByte = new int[257];
            int pos = 0;
            for (int i = 0; i < 256; i++) {
                while (pos < origdata.Length && origdata[suffixes[pos]] < i)
                    pos++;
                firstIndexOfByte[i] = pos;
            }
            firstIndexOfByte[256] = origdata.Length;
            Console.WriteLine("determined starting position of each char");

            fl0toSong = new int[songCount];
            for (int i = 0; i < songCount; i++) {
                fl0toSong[i] = songEnds[suffixes[firstIndexOfByte[TERMINATOR]+i]];
            }
//            songEnds = null;

            Console.WriteLine("Mapped i-th terminator to song index");

            int[] whereami = new int[origdata.Length];
            for (int i = 0; i < whereami.Length; i++)
                whereami[suffixes[i]] = i;
            Console.WriteLine("Told chars where they are in the suffixarray");
            fl = new int[origdata.Length];
            for (int i = 0; i < origdata.Length - 1; i++) {
                fl[whereami[i]] = whereami[i + 1];
            }

            fl[whereami[origdata.Length - 1]] = whereami[0];
        }
    }
}

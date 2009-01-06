using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SongDataLib;
using LastFMspider;
using EmnExtensions;

namespace DeURLizeM3U
{
    class Program
    {
        static void Main(string[] args) {
            if (args.Length == 1 && Directory.Exists(args[0]))
                args = Directory.GetFiles(args[0]);
            LastFmTools tools = new LastFmTools(new SongDatabaseConfigFile(false));
            foreach (var m3ufilename in args) {
                try {
                    Console.WriteLine("\n\n\n\nprocessing: {0}", m3ufilename);
                    FileInfo fi = new FileInfo(m3ufilename);
                    if (!fi.Exists) {
                        Console.WriteLine("Not found");
                        continue;
                    }
                    MinimalSongData[] playlistfixed;
                    PartialSongData[] playlist = LoadExtM3U(fi);
                    playlistfixed = new MinimalSongData[playlist.Length];
                    int nulls = 0, nulls2 = 0;
                    int idx = 0;
                    foreach (var song in playlist) {
                        SongData best = FindBestMatch(tools, song);
                        if (best == null) {
                            nulls++;
                            best = FindBestMatch2(tools, song);
                            if (best == null) {
                                Console.WriteLine("XXX:({1}) {0}  ===  {2}\n", NormalizedFileName(song.SongPath), song.length, song.HumanLabel);
                                nulls2++;
                            } else
                                Console.WriteLine("!!!:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath), NormalizedFileName(best.SongPath), song.length, best.Length);
                        } else {
                            Console.WriteLine("Was:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath), NormalizedFileName(best.SongPath), song.length, best.Length);
                        }

                        if (best == null)
                            playlistfixed[idx] = song;
                        else
                            playlistfixed[idx] = best;
                        idx++;

                    }
                    Console.WriteLine(nulls);
                    Console.WriteLine(nulls2);
                    FileInfo outputplaylist = new FileInfo(Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + "-fixed.m3u"));
                    using (var stream = outputplaylist.OpenWrite())
                    using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
                        writer.WriteLine("#EXTM3U");
                        foreach (var track in playlistfixed) {
                            writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\r\n" + track.SongPath);
                        }
                    }

                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }

            }

        }

        //modified from:http://www.merriampark.com/ldcsharp.htm
        public static int LD(string s, string t) {
            int n = s.Length; //length of s
            int m = t.Length; //length of t
            int[,] d = new int[n + 1, m + 1]; // matrix
            int cost; // cost
            // Step 1
            if (n == 0) return m;
            if (m == 0) return n;
            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            // Step 3
            for (int i = 0; i < n; i++) {
                //Step 4
                for (int j = 0; j < m; j++) {
                    // Step 5
                    cost = (t[j] == s[i] ? 0 : 1);
                    // Step 6
                    d[i + 1, j + 1] = System.Math.Min(System.Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1), d[i, j] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        static SongData FindBestMatch(LastFmTools tools, PartialSongData songToFind) {
            var q = from songrefOpt in PossibleSongRefs(songToFind.HumanLabel)
                    where tools.Lookup.dataByRef.ContainsKey(songrefOpt)
                    from songdataOpt in tools.Lookup.dataByRef[songrefOpt]
                    let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
                    let filenameDiff = LD(NormalizedFileName(songToFind.SongPath), NormalizedFileName(songdataOpt.SongPath))
                    select new { SongData = songdataOpt, Cost = lengthDiff*2 + filenameDiff };
            return q.Aggregate(new { SongData = (SongData)null, Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b).SongData;
        }
        static SongData FindBestMatch2(LastFmTools tools, PartialSongData songToFind) {
            string fileName = NormalizedFileName(songToFind.SongPath);
            var q = from songdataOpt in tools.DB.Songs
                    let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
                    where lengthDiff < 15
                    let nameDiff = LD(fileName, Path.GetFileName(songdataOpt.SongPath))
                    where nameDiff < 35
                    let labelDiff = LD(songdataOpt.HumanLabel, songToFind.HumanLabel)
                    select new {
                        SongData = songdataOpt,
                        Cost = lengthDiff
                        + nameDiff
                        + labelDiff
                    };
            return q.Aggregate(new { SongData = (SongData)null, Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b).SongData;
        }

        static PartialSongData[] LoadExtM3U(FileInfo m3ufile) {
            List<PartialSongData> m3usongs = new List<PartialSongData>();
            using (var m3uStream = m3ufile.OpenRead()) {
                SongDataFactory.LoadSongsFromM3U(
                    m3uStream,
                    delegate(ISongData newsong, double completion) {
                        if (newsong is PartialSongData)
                            m3usongs.Add((PartialSongData)newsong);
                    },
                    Encoding.GetEncoding(1252),
                    true
                    );
            }
            return m3usongs.ToArray();
        }
        public static IEnumerable<SongRef> PossibleSongRefs(string humanlabel) {
            int idxFound = -1;
            while (true) {
                idxFound = humanlabel.IndexOf(" - ", idxFound + 1);
                if (idxFound < 0) yield break;
                yield return SongRef.Create(humanlabel.Substring(0, idxFound), humanlabel.Substring(idxFound + 3));
                //yield return SongRef.Create( humanlabel.Substring(idxFound + 3),humanlabel.Substring(0, idxFound));
            }
        }

        static char[] pathSep = { '\\', '/' };
        public static string NormalizedFileName(string origpath) {
            return Uri.UnescapeDataString(origpath.Substring(origpath.LastIndexOfAny(pathSep) + 1));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SongDataLib;
using LastFMspider;
using EmnExtensions;
using EmnExtensions.Text;
using System.Threading;

namespace DeURLizeM3U
{
    class Program
    {
        static void Main(string[] args) {
			if (args.Length == 1 && Directory.Exists(args[0])) {

				args = Directory.GetFiles(args[0], "*.m3u")
					//.Where(fi => !Path.GetFileNameWithoutExtension(fi).EndsWith("-fixed"))
					.ToArray();
			}
            LastFmTools tools = new LastFmTools(new SongDatabaseConfigFile(false));
            int nulls2 = 0, fine = 0;
            //Parallel.ForEach(args, m3ufilename => {
            List<SongMatch> 
                toobadL = new List<SongMatch>(),
                hmmL = new List<SongMatch>();
            //List<PartialSongData> errL = new List<PartialSongData>();
            File.Delete("m3ufixer-err.log");
            File.Delete("m3ufixer-ok.log");

            foreach (var m3ufilename in args) {
                try {
                    Console.WriteLine("\nprocessing: {0}", m3ufilename);
                    FileInfo fi = new FileInfo(m3ufilename);
                    if (!fi.Exists) {
                        Console.WriteLine("Not found");
                    } else {
						if (fi.Name.EndsWith("-fixed.m3u")) {
							string newPath = Path.Combine(
								fi.Directory.FullName,
								fi.Name.Substring(0, fi.Name.Length - "-fixed.m3u".Length)
								+ ".m3u");
							if (!File.Exists(newPath))
								fi.MoveTo(newPath);
						}

                        MinimalSongData[] playlistfixed;
                        PartialSongData[] playlist = LoadExtM3U(fi);
                        playlistfixed = new MinimalSongData[playlist.Length];
                        int idx = 0;
                        foreach (var song in playlist) {
                            SongMatch best = FindBestMatch(tools, song);
                            if (best.SongData == null) {

                                best = FindBestMatch2(tools, song);
                                if (best.SongData == null) {
                                    Console.WriteLine("XXX:({1}) {0}  ===  {2}\n", NormalizedFileName(song.SongPath), song.length, song.HumanLabel);
                                    File.AppendAllText("m3ufixer-err.log", SongMatch.ToString(song));
                                    nulls2++;
                                } else if (best.Cost > 8) {
                                    Console.WriteLine("!!!{4}:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath) + ": " + song.HumanLabel, NormalizedFileName(best.SongData.SongPath) + ": " + best.SongData.HumanLabel, song.length, best.SongData.Length, best.Cost);
                                    toobadL.Add(best);

                                    best = new SongMatch { SongData = null };
                                } else {
                                    Console.WriteLine("___:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath) + ": " + song.HumanLabel, NormalizedFileName(best.SongData.SongPath) + ": " + best.SongData.HumanLabel, song.length, best.SongData.Length);
                                    hmmL.Add(best);
                                }
                            } else {
                                fine++;
                                File.AppendAllText("m3ufixer-ok.log", NormalizedFileName(song.SongPath) + "(" + TimeSpan.FromSeconds(song.Length).ToString() + "): " + song.HumanLabel + "\t==>\t" + NormalizedFileName(best.SongData.SongPath) + "(" + TimeSpan.FromSeconds(best.SongData.Length).ToString() + "): " + best.SongData.HumanLabel + "\n");

                                //  Console.WriteLine("Was:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath), NormalizedFileName(best.SongPath), song.length, best.Length);
                            }

                            if (best.SongData == null)
                                playlistfixed[idx] = song;
                            else
                                playlistfixed[idx] = best.SongData;
                            idx++;

                        }
                        Console.WriteLine("Fine: {0}, Rough: {1}, Too bad: {2},  No-match: {3}", fine, hmmL.Count,toobadL.Count, nulls2);
                        FileInfo outputplaylist = new FileInfo(Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + ".m3u"));
                        using (var stream = outputplaylist.Open(FileMode.Create,FileAccess.Write))
                        using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
                            writer.WriteLine("#EXTM3U");
                            foreach (var track in playlistfixed) {
                                writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\r\n" + track.SongPath);
                            }
                        }
                        hmmL.Sort((a, b) => b.Cost.CompareTo(a.Cost));
                        toobadL.Sort((a, b) => a.Cost.CompareTo(b.Cost));
                        using (var stream = File.Open("m3ufixer-hmm.log",FileMode.Create,FileAccess.Write))
                        using (var writer = new StreamWriter(stream))
                            foreach (var match in hmmL)
                                writer.WriteLine(match.ToString());
                        using (var stream = File.Open("m3ufixer-toobad.log", FileMode.Create, FileAccess.Write))
                        using (var writer = new StreamWriter(stream))
                            foreach (var match in toobadL)
                                writer.WriteLine(match.ToString());

                        


                    }
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }

            };

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
                    cost = (t[j] == s[i] ? 0 : 2);//substitution will be cost 2.
                    // Step 6
                    d[i + 1, j + 1] = System.Math.Min(System.Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1), d[i, j] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
        struct SongMatch
        {
            public static SongMatch? Compare(PartialSongData src, string filename, string normlabel, SongData opt) {
                double lenC = Math.Abs(src.Length - opt.Length);
                if (lenC > 5) return null;
                string optFileName = Path.GetFileName(opt.SongPath);
                string optBasicLabel = Canonicalize.Basic(opt.HumanLabel);
                double nameC = LD(filename, optFileName) / (double)(filename.Length + optFileName.Length);
                double labelC = LD(optBasicLabel, normlabel) / (double)(normlabel.Length + optBasicLabel.Length);
                return new SongMatch {
                    SongData = opt,
                    Orig = src,
                    LenC = lenC,
                    NameC = nameC,
                    TagC = labelC,
                    Cost = lenC/2.0 + Math.Sqrt(50 * Math.Min(nameC, labelC)) + Math.Sqrt(50 * labelC)
                };
            }
            public SongData SongData;
            public PartialSongData Orig;
            public double Cost, LenC, NameC, TagC;
            public override string ToString() {
                return string.Format("{0,7:g5} {1,7:g5} {2,7:g5} {3,7:g5} {4} ==> {5} ", Cost, LenC, NameC, TagC, ToString(Orig), ToString(SongData));
            }
            public static string ToString(ISongData song) {
                return NormalizedFileName(song.SongPath) + ": " + song.HumanLabel + " (" + TimeSpan.FromSeconds(song.Length) + ")";
            }
        }

        static SongMatch FindBestMatch(LastFmTools tools, PartialSongData songToFind) {
            var q = from songrefOpt in PossibleSongRefs(songToFind.HumanLabel)
                    where tools.Lookup.dataByRef.ContainsKey(songrefOpt)
                    from songdataOpt in tools.Lookup.dataByRef[songrefOpt]
                    let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
                    let filenameDiff = LD(NormalizedFileName(songToFind.SongPath), NormalizedFileName(songdataOpt.SongPath))
                    select new SongMatch { SongData = songdataOpt, Orig = songToFind, Cost = lengthDiff * 0.5 + filenameDiff * 0.2 };
            return q.Aggregate(new SongMatch { SongData = (SongData)null, Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
        }
        static SongMatch FindBestMatch2(LastFmTools tools, PartialSongData songToFind) {
            string fileName = NormalizedFileName(songToFind.SongPath);
            string basicLabel = Canonicalize.Basic(songToFind.HumanLabel);
            var q = from songdataOpt in tools.DB.Songs
                    let songmatch = SongMatch.Compare(songToFind, fileName, basicLabel, songdataOpt)
                    where songmatch.HasValue
                    select songmatch.Value;
            return q.Aggregate(new SongMatch { SongData = (SongData)null, Cost = (double)int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
        }

        static PartialSongData[] LoadExtM3U(FileInfo m3ufile) {
            List<PartialSongData> m3usongs = new List<PartialSongData>();
            using (var m3uStream = m3ufile.OpenRead()) {
                SongDataFactory.LoadSongsFromM3U(
                    m3uStream,
                    delegate(ISongData newsong, double completion) {
                        if (newsong is PartialSongData)
                            m3usongs.Add((PartialSongData)newsong);
                        else throw new Exception("No partial song data for fuzzy comparisons; are you loading a real #EXTM3U file?");
                    },
                    Encoding.GetEncoding(1252),
                    null
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
            string filename = origpath.Substring(origpath.LastIndexOfAny(pathSep) + 1);
            try {
                return Uri.UnescapeDataString(filename.Replace("100%", "100%25").Replace("%%", "%25%"));
            } catch {//if the not-so-solid uri unescaper can't handle it, assume it's not encoded.  It's no biggy anyhow, this is just normalization.
                return filename;
            }
        }
    }
}

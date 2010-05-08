using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SongDataLib;
using LastFMspider;
using EmnExtensions;
using EmnExtensions.Text;
using EmnExtensions.Algorithms;
//using System.Threading;

namespace PlaylistFixer
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
					ProcessM3U(tools, m3ufilename, () => { nulls2++; }, toobadL.Add, hmmL.Add, () => { fine++; });
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
					Console.WriteLine("(Press any key to continue)");
					Console.ReadKey();
				}
				Console.WriteLine("Fine: {0}, Rough: {1}, Too bad: {2},  No-match: {3}", fine, hmmL.Count, toobadL.Count, nulls2);

			};
			hmmL.Sort((a, b) => b.Cost.CompareTo(a.Cost));
			toobadL.Sort((a, b) => a.Cost.CompareTo(b.Cost));
			using (var stream = File.Open("m3ufixer-hmm.log", FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
				foreach (var match in hmmL)
					writer.WriteLine(match.ToString());
			using (var stream = File.Open("m3ufixer-toobad.log", FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
				foreach (var match in toobadL)
					writer.WriteLine(match.ToString());


			Console.WriteLine("done! (Press any key to close this window)");
			Console.ReadKey();
		}

		static void ProcessM3U(LastFmTools tools, string m3ufilename, Action nomatch, Action<SongMatch> toobad, Action<SongMatch> iffy,Action matchfound) {
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
				MinimalSongData[] playlist = LoadExtM3U(fi);
				playlistfixed = new MinimalSongData[playlist.Length];
				int idx = 0;
				foreach (var songMin in playlist) {
					MinimalSongData decentMatch = null;
					if (tools.Lookup.dataByPath.ContainsKey(songMin.SongUri.ToString()))
						decentMatch = tools.Lookup.dataByPath[songMin.SongUri.ToString()];
					else if (songMin is PartialSongData) {
						PartialSongData song = (PartialSongData)songMin;
						SongMatch best = FindBestMatch(tools, song);
						if (best.SongData == null) {

							best = FindBestMatch2(tools, song);
							if (best.SongData == null) {
								Console.WriteLine("XXX:({1}) {0}  ===  {2}\n", NormalizedFileName(song.SongUri.LocalPath), song.length, song.HumanLabel);
								File.AppendAllText("m3ufixer-err.log", SongMatch.ToString(song));
								nomatch();
							} else if (best.Cost > 7.5) {
								Console.WriteLine("!!!{4}:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongUri.LocalPath) + ": " + song.HumanLabel, NormalizedFileName(best.SongData.SongUri.LocalPath) + ": " + best.SongData.HumanLabel, song.length, best.SongData.Length, best.Cost);
								toobad(best);

								best = new SongMatch { SongData = null };
							} else {
								Console.WriteLine("___:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongUri.LocalPath) + ": " + song.HumanLabel, NormalizedFileName(best.SongData.SongUri.LocalPath) + ": " + best.SongData.HumanLabel, song.length, best.SongData.Length);
								iffy(best);
							}
						} else {
							matchfound();
							File.AppendAllText("m3ufixer-ok.log", NormalizedFileName(song.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(song.Length).ToString() + "): " + song.HumanLabel + "\t==>\t" + NormalizedFileName(best.SongData.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(best.SongData.Length).ToString() + "): " + best.SongData.HumanLabel + "\n");

							//  Console.WriteLine("Was:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath), NormalizedFileName(best.SongPath), song.length, best.Length);
						}

						decentMatch = best.SongData;
					} else {
						SongData[] exactFilenameMatch = tools.DB.Songs.Where(sd => Path.GetFileName(sd.SongUri.ToString()) == Path.GetFileName(songMin.SongUri.ToString())).ToArray();
						if (exactFilenameMatch.Length == 1)
							decentMatch = exactFilenameMatch[0];
					}
					playlistfixed[idx++] = decentMatch ?? songMin;
				}

				FileInfo outputplaylist = new FileInfo(Path.Combine(fi.DirectoryName, Path.GetFileNameWithoutExtension(fi.Name) + ".m3u"));
				using (var stream = outputplaylist.Open(FileMode.Create, FileAccess.Write))
				using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
					writer.WriteLine("#EXTM3U");
					foreach (var track in playlistfixed) {
						writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\r\n" + track.SongUri.ToString());
					}
				}
			}
		}

		struct SongMatch
		{
			public static SongMatch? Compare(PartialSongData src, string filename, string normlabel, SongData opt) {
				double lenC = Math.Abs(src.Length - opt.Length);
				if (lenC > 15) return null;
				string optFileName = Path.GetFileName(opt.SongUri.ToString());
				string optBasicLabel = Canonicalize.Basic(opt.HumanLabel);
				double nameC = filename.LevenshteinDistance(optFileName) / (double)(filename.Length + optFileName.Length);
				double labelC = optBasicLabel.LevenshteinDistance(normlabel) / (double)(normlabel.Length + optBasicLabel.Length);
				return new SongMatch {
					SongData = opt,
					Orig = src,
					LenC = lenC,
					NameC = nameC,
					TagC = labelC,
					Cost = lenC / 5.0 + Math.Sqrt(50 * Math.Min(nameC, labelC)) + Math.Sqrt(50 * labelC)
				};
			}
			public SongData SongData;
			public PartialSongData Orig;
			public double Cost, LenC, NameC, TagC;
			public override string ToString() {
				return string.Format("{0,7:g5} {1,7:g5} {2,7:g5} {3,7:g5} {4} ==> {5} ", Cost, LenC, NameC, TagC, ToString(Orig), ToString(SongData));
			}
			public static string ToString(ISongData song) {
				return NormalizedFileName(song.SongUri.ToString()) + ": " + song.HumanLabel + " (" + TimeSpan.FromSeconds(song.Length) + ")";
			}
		}

		static SongMatch FindBestMatch(LastFmTools tools, PartialSongData songToFind) {
			var q = from songrefOpt in PossibleSongRefs(songToFind.HumanLabel)
					where tools.Lookup.dataByRef.ContainsKey(songrefOpt)
					from songdataOpt in tools.Lookup.dataByRef[songrefOpt]
					let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
					let filenameDiff = NormalizedFileName(songToFind.SongUri.ToString()).LevenshteinDistance(NormalizedFileName(songdataOpt.SongUri.ToString()))
					select new SongMatch { SongData = songdataOpt, Orig = songToFind, Cost = lengthDiff * 0.5 + filenameDiff * 0.2 };
			return q.Aggregate(new SongMatch { SongData = (SongData)null, Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
		}
		static SongMatch FindBestMatch2(LastFmTools tools, PartialSongData songToFind) {
			string fileName = NormalizedFileName(songToFind.SongUri.ToString());
			string basicLabel = Canonicalize.Basic(songToFind.HumanLabel);
			var q = from songdataOpt in tools.DB.Songs
					let songmatch = SongMatch.Compare(songToFind, fileName, basicLabel, songdataOpt)
					where songmatch.HasValue
					select songmatch.Value;
			return q.Aggregate(new SongMatch { SongData = (SongData)null, Cost = (double)int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
		}

		static MinimalSongData[] LoadExtM3U(FileInfo m3ufile) {
			List<MinimalSongData> m3usongs = new List<MinimalSongData>();
			using (var m3uStream = m3ufile.OpenRead()) {
				SongDataFactory.LoadSongsFromM3U(
					m3uStream,
					delegate(ISongData newsong, double completion) {
						if (newsong is MinimalSongData)
							m3usongs.Add((MinimalSongData)newsong);
					},
					m3ufile.Extension.ToLowerInvariant() == "m3u8" ? Encoding.UTF8 : Encoding.GetEncoding(1252),
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SongDataLib;
using LastFMspider;
using EmnExtensions.Text;
using EmnExtensions.Algorithms;

namespace PlaylistFixer {
	static class Program {
		static void Main(string[] args) {
			if (args.Length == 1 && Directory.Exists(args[0])) {

				args = Directory.GetFiles(args[0], "*.m3u")
					//.Where(fi => !Path.GetFileNameWithoutExtension(fi).EndsWith("-fixed"))
					.ToArray();
			}
			SongTools tools = new SongTools(new SongDataConfigFile(false));
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
			}
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

		static void ProcessM3U(SongTools tools, string m3ufilename, Action nomatch, Action<SongMatch> toobad, Action<SongMatch> iffy, Action matchfound) {
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

				MinimalSongFileData[] playlist = LoadExtM3U(fi);
				MinimalSongFileData[] playlistfixed = new MinimalSongFileData[playlist.Length];
				int idx = 0;
				foreach (var songMin in playlist) {
					MinimalSongFileData decentMatch = null;
					if (tools.FindByPath.ContainsKey(songMin.SongUri.ToString()))
						decentMatch = tools.FindByPath[songMin.SongUri.ToString()];
					else if (songMin is PartialSongFileData) {
						PartialSongFileData song = (PartialSongFileData)songMin;
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
							File.AppendAllText("m3ufixer-ok.log", NormalizedFileName(song.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(song.Length) + "): " + song.HumanLabel + "\t==>\t" + NormalizedFileName(best.SongData.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(best.SongData.Length) + "): " + best.SongData.HumanLabel + "\n");

							//  Console.WriteLine("Was:({2}) {0}\n Is:({3}) {1}\n", NormalizedFileName(song.SongPath), NormalizedFileName(best.SongPath), song.length, best.Length);
						}

						decentMatch = best.SongData;
					} else {
						SongFileData[] exactFilenameMatch = tools.SongFilesSearchData.Songs.Where(sd => Path.GetFileName(sd.SongUri.ToString()) == Path.GetFileName(songMin.SongUri.ToString())).ToArray();
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
						writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\r\n" + track.SongUri);
					}
				}
			}
		}

		struct SongMatch {
			public static SongMatch? Compare(PartialSongFileData src, string filename, string normlabel, SongFileData opt) {
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
			public SongFileData SongData;
			public PartialSongFileData Orig;
			public double Cost;
			double LenC, NameC, TagC;

			public override string ToString() {
				return string.Format("{0,7:g5} {1,7:g5} {2,7:g5} {3,7:g5} {4} ==> {5} ", Cost, LenC, NameC, TagC, ToString(Orig), ToString(SongData));
			}
			public static string ToString(ISongFileData song) {
				return NormalizedFileName(song.SongUri.ToString()) + ": " + song.HumanLabel + " (" + TimeSpan.FromSeconds(song.Length) + ")";
			}
		}

		static SongMatch FindBestMatch(SongTools tools, PartialSongFileData songToFind) {
			var q = from songrefOpt in PossibleSongRefs(songToFind.HumanLabel)
					from songdataOpt in tools.FindByName[songrefOpt]
					let lengthDiff = Math.Abs(songToFind.Length - songdataOpt.Length)
					let filenameDiff = NormalizedFileName(songToFind.SongUri.ToString()).LevenshteinDistance(NormalizedFileName(songdataOpt.SongUri.ToString()))
					select new SongMatch { SongData = songdataOpt, Orig = songToFind, Cost = lengthDiff * 0.5 + filenameDiff * 0.2 };
			return q.Aggregate(new SongMatch { SongData = default(SongFileData), Cost = int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
		}
		static SongMatch FindBestMatch2(SongTools tools, PartialSongFileData songToFind) {
			string fileName = NormalizedFileName(songToFind.SongUri.ToString());
			string basicLabel = Canonicalize.Basic(songToFind.HumanLabel);
			var q = from songdataOpt in tools.SongFilesSearchData.Songs
					let songmatch = SongMatch.Compare(songToFind, fileName, basicLabel, songdataOpt)
					where songmatch.HasValue
					select songmatch.Value;
			// ReSharper disable RedundantCast
			return q.Aggregate(new SongMatch { SongData = default(SongFileData), Cost = (double)int.MaxValue }, (a, b) => a.Cost < b.Cost ? a : b);
			// ReSharper restore RedundantCast
		}

		static MinimalSongFileData[] LoadExtM3U(FileInfo m3ufile) {
			List<MinimalSongFileData> m3usongs = new List<MinimalSongFileData>();
			using (var m3uStream = m3ufile.OpenRead()) {
				SongFileDataFactory.LoadSongsFromM3U(
					m3uStream,
					delegate(ISongFileData newsong, double completion) {
						if (newsong is MinimalSongFileData)
							m3usongs.Add((MinimalSongFileData)newsong);
					},
					m3ufile.Extension.ToLowerInvariant() == "m3u8" ? Encoding.UTF8 : Encoding.GetEncoding(1252),
					null
					);
			}
			return m3usongs.ToArray();
		}

		static IEnumerable<SongRef> PossibleSongRefs(string humanlabel) {
			int idxFound = -1;
			while (true) {
				idxFound = humanlabel.IndexOf(" - ", idxFound + 1);
				if (idxFound < 0) yield break;
				yield return SongRef.Create(humanlabel.Substring(0, idxFound), humanlabel.Substring(idxFound + 3));
				//yield return SongRef.Create( humanlabel.Substring(idxFound + 3),humanlabel.Substring(0, idxFound));
			}
		}

		static readonly char[] pathSep = { '\\', '/' };
		static string NormalizedFileName(string origpath) {
			string filename = origpath.Substring(origpath.LastIndexOfAny(pathSep) + 1);
			try {
				return Uri.UnescapeDataString(filename.Replace("100%", "100%25").Replace("%%", "%25%"));
			} catch {//if the not-so-solid uri unescaper can't handle it, assume it's not encoded.  It's no biggy anyhow, this is just normalization.
				return filename;
			}
		}
	}
}

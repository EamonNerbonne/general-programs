using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SongDataLib;
using LastFMspider;
using EmnExtensions.Text;
using EmnExtensions.Algorithms;
using EmnExtensions;

namespace PlaylistFixer {
	static class Program {
		static void Main(string[] args) {
			if (args.Length == 1 && Directory.Exists(args[0])) {

				args = Directory.GetFiles(args[0], "*.m3u")
					//.Where(fi => !Path.GetFileNameWithoutExtension(fi).EndsWith("-fixed"))
					.ToArray();
			}
			SongTools tools = new SongTools(new SongDataConfigFile(false));
			FuzzySongSearcher searchEngine = new FuzzySongSearcher(tools.SongFilesSearchData.Songs);
			Func<Uri, SongFileData> findByUri = uri => { SongFileData retval; tools.FindByPath.TryGetValue(uri.ToString(), out retval); return retval; };

			int nulls2 = 0, fine = 0;
			//Parallel.ForEach(args, m3ufilename => {
			List<PlaylistSongMatch>
				toobadL = new List<PlaylistSongMatch>(),
				hmmL = new List<PlaylistSongMatch>();
			//List<PartialSongData> errL = new List<PartialSongData>();
			File.Delete("m3ufixer-err.log");
			File.Delete("m3ufixer-ok.log");

			foreach (var m3ufilename in args) {
				try {
					ProcessM3U(searchEngine, findByUri, new FileInfo(m3ufilename), unmatchedSong => {
						Console.WriteLine("XXX:({1}) {0}  ===  {2}\n", RepairPlaylist.NormalizedFileName(unmatchedSong.SongUri.LocalPath), unmatchedSong.length, unmatchedSong.HumanLabel);
						File.AppendAllText("m3ufixer-err.log", PlaylistSongMatch.ToString(unmatchedSong) + "\n");
						nulls2++;
					}, badMatch => {
						toobadL.Add(badMatch);
						Console.WriteLine("!!!{4}:({2}) {0}\n Is:({3}) {1}\n", RepairPlaylist.NormalizedFileName(badMatch.Orig.SongUri.LocalPath) + ": " + badMatch.Orig.HumanLabel, RepairPlaylist.NormalizedFileName(badMatch.SongData.SongUri.LocalPath) + ": " + badMatch.SongData.HumanLabel, badMatch.Orig.Length, badMatch.SongData.Length, badMatch.Cost);
					}, iffyMatch => {
						hmmL.Add(iffyMatch);
						Console.WriteLine("___:({2}) {0}\n Is:({3}) {1}\n", RepairPlaylist.NormalizedFileName(iffyMatch.Orig.SongUri.LocalPath) + ": " + iffyMatch.Orig.HumanLabel, RepairPlaylist.NormalizedFileName(iffyMatch.SongData.SongUri.LocalPath) + ": " + iffyMatch.SongData.HumanLabel, iffyMatch.Orig.Length, iffyMatch.SongData.Length);
					}, goodMatch => {
						File.AppendAllText("m3ufixer-ok.log", RepairPlaylist.NormalizedFileName(goodMatch.Orig.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(goodMatch.Orig.Length) + "): " + goodMatch.Orig.HumanLabel + "\t==>\t" + RepairPlaylist.NormalizedFileName(goodMatch.SongData.SongUri.LocalPath) + "(" + TimeSpan.FromSeconds(goodMatch.SongData.Length) + "): " + goodMatch.SongData.HumanLabel + "\n");
						fine++;
					});
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

		static void ProcessM3U(FuzzySongSearcher fuzzySearcher, Func<Uri, SongFileData> findByUri, FileInfo fi, Action<PartialSongFileData> nomatch, Action<PlaylistSongMatch> toobad, Action<PlaylistSongMatch> iffy, Action<PlaylistSongMatch> matchfound) {
			Console.WriteLine("\nprocessing: {0}", fi.FullName);
			if (!fi.Exists) {
				Console.WriteLine("Not found");
			} else {
				ISongFileData[] playlist = SongFileDataFactory.LoadExtM3U(fi);
				ISongFileData[] playlistfixed = RepairPlaylist.GetPlaylistFixed(playlist, fuzzySearcher, findByUri, nomatch, toobad, iffy, matchfound);

				FileInfo outputplaylist = new FileInfo(Path.ChangeExtension(fi.FullName, ".m3u8"));
				using (var stream = outputplaylist.Open(FileMode.Create, FileAccess.Write))
				using (var writer = new StreamWriter(stream, Encoding.UTF8))
					SongFileDataFactory.WriteSongsToM3U(writer, playlistfixed);
				if (fi.Extension == ".m3u")
					fi.Delete();
			}
		}
	}
}

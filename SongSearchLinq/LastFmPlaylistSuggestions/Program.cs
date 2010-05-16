using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EmnExtensions;
using EmnExtensions.Algorithms;
using EmnExtensions.Collections;
using EmnExtensions.DebugTools;
using EmnExtensions.Text;
using LastFMspider;
using MoreLinq;
using SongDataLib;

namespace LastFmPlaylistSuggestions {


	class Program {
		static void Main(string[] args) {
			//args.PrintAllDebug();
			//Console.ReadLine();
			//Console.WriteLine(CharMap.MapSize);
			RunNew(new LastFmTools(new SongDatabaseConfigFile(false)), args);
			//Console.ReadKey();
		}
		static void RunNew(LastFmTools tools, string[] args) {
			var dir = tools.DB.DatabaseDirectory.CreateSubdirectory("inputlists");
			var m3us = args.Length == 0 ? dir.GetFiles("*.m3u?") : args.Select(s => new FileInfo(s)).Where(f => f.Exists);
			DirectoryInfo m3uDir = args.Length == 0 ? tools.DB.DatabaseDirectory.CreateSubdirectory("similarlists") : new DirectoryInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

			FuzzySongSearcher searchEngine = new FuzzySongSearcher(tools.DB.Songs);

			foreach (var m3ufile in m3us) {
				try {
					using (var dTimer = new DTimer("Processing " + m3ufile.Name))
						ProcessM3U(tools, searchEngine.FindBestMatch, m3ufile, m3uDir);
				} catch (Exception e) {
					Console.WriteLine("Unexpected error on processing " + m3ufile);
					Console.WriteLine(e.ToString());
				}
			}
		}

		static void FindPlaylistSongLocally(LastFmTools tools, ISongData playlistEntry, Action<SongData> ifFound, Action<SongRef> ifNotFound, Action<ISongData> cannotParse) {
			SongData bestMatch = null;
			int artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ");
			if (tools.Lookup.dataByPath.ContainsKey(playlistEntry.SongUri.ToString()))
				ifFound(tools.Lookup.dataByPath[playlistEntry.SongUri.ToString()]);
			else if (playlistEntry.IsLocal && File.Exists(playlistEntry.SongUri.LocalPath)) {
				ifFound((SongData)SongDataFactory.ConstructFromFile(new FileInfo(playlistEntry.SongUri.LocalPath)));
			} else if (playlistEntry is PartialSongData) {
				int bestMatchVal = Int32.MaxValue;
				while (artistTitleSplitIndex != -1) {
					SongRef songref = SongRef.Create(playlistEntry.HumanLabel.Substring(0, artistTitleSplitIndex), playlistEntry.HumanLabel.Substring(artistTitleSplitIndex + 3));
					if (tools.Lookup.dataByRef.ContainsKey(songref)) {
						foreach (var songCandidate in tools.Lookup.dataByRef[songref]) {
							int candidateMatchVal = 100 * Math.Abs(playlistEntry.Length - songCandidate.Length) + Math.Min(199, Math.Abs(songCandidate.bitrate - 224));
							if (candidateMatchVal < bestMatchVal) {
								bestMatchVal = candidateMatchVal;
								bestMatch = songCandidate;
							}
						}
					}
					artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ", artistTitleSplitIndex + 3);
				}
				if (bestMatch != null) ifFound(bestMatch);
				else {
					artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ");
					if (artistTitleSplitIndex >= 0) ifNotFound(SongRef.Create(playlistEntry.HumanLabel.Substring(0, artistTitleSplitIndex), playlistEntry.HumanLabel.Substring(artistTitleSplitIndex + 3)));
					else cannotParse(playlistEntry);
				}
			} else {
				cannotParse(playlistEntry);
			}
		}

		static void ProcessM3U(LastFmTools tools, Func<SongRef,SongMatch> fuzzySearch, FileInfo m3ufile, DirectoryInfo m3uDir) {
			Console.WriteLine("Trying " + m3ufile.FullName);
			ISongData[] playlist = LoadExtM3U(m3ufile);
			var known = new List<SongData>();
			var unknown = new List<SongRef>();
			foreach (var song in playlist)
				FindPlaylistSongLocally(tools, song,
					(songData) => { known.Add(songData); },
					(songRef) => { unknown.Add(songRef); },
					(oopsSong) => { Console.WriteLine("Can't deal with: " + oopsSong.HumanLabel + "\nat:" + oopsSong.SongUri.ToString()); }
				);

			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			var res = FindSimilarPlaylist.ProcessPlaylist(tools, fuzzySearch, known, unknown, 1000);

			FileInfo outputplaylist = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.m3u"));
			using (var stream = outputplaylist.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
				writer.WriteLine("#EXTM3U");
				foreach (var track in res.knownTracks) {
					writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\n" + track.SongUri.LocalPath);
				}
			}
			FileInfo outputsimtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.txt"));
			using (var stream = outputsimtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				foreach (var track in res.similarList) {
					writer.WriteLine("{0} {2} {1}    [{3}]",
							track.cost,
							track.songref.ToString(),
							tools.Lookup.dataByRef.ContainsKey(track.songref) ? "" : "***",
							string.Join(";  ", track.basedOn.Select(sr => sr.ToString()).ToArray())
						);
				}
			}
			FileInfo outputmissingtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-missing.txt"));
			//using(new DTimer("finding missing tracks"))
			using (var stream = outputmissingtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				//var q = from songdata in tools.DB.Songs.Where(songdata=>songdata.performer!=null&& songdata.title!=null)
				//        from trigram in 
				foreach (var missingTrack in
					from track in res.unknownTracks
					let bestMatch = fuzzySearch(track)
					let orderCost = bestMatch.Cost
					orderby orderCost
					select new { Search = track, Match = bestMatch.Song, Cost = bestMatch.Cost, Explain = bestMatch.Explain }
					) {
					if (missingTrack.Match != null) {
						writer.WriteLine("{0}      {1}    ||{2}: {3} - {4}   --  {5} at {6}kbps ||{7}", missingTrack.Search.Artist, missingTrack.Search.Title, missingTrack.Cost, missingTrack.Match.artist ?? "?", missingTrack.Match.title ?? "?", TimeSpan.FromSeconds(missingTrack.Match.Length), missingTrack.Match.bitrate, missingTrack.Explain);
					} else {
						writer.WriteLine("{0}      {1}", missingTrack.Search.Artist, missingTrack.Search.Title);
					}
				}
			}

		}



		static ISongData[] LoadExtM3U(FileInfo m3ufile) {
			List<ISongData> m3usongs = new List<ISongData>();
			using (var m3uStream = m3ufile.OpenRead()) {
				SongDataFactory.LoadSongsFromM3U(
					m3uStream, (newsong, completion) => { m3usongs.Add(newsong); },
					m3ufile.Extension.EndsWith("8") ? Encoding.UTF8 : Encoding.GetEncoding(1252),
					true
					);
			}
			return m3usongs.ToArray();
		}
	}
}

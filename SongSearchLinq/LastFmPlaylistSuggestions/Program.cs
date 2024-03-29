﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EmnExtensions.DebugTools;
using EmnExtensions.IO;
using LastFMspider;
using SongDataLib;

namespace LastFmPlaylistSuggestions {
	static class Program {
		static void Main(string[] args) {
			//args.PrintAllDebug();
			//Console.ReadLine();
			//Console.WriteLine(CharMap.MapSize);
			RunNew(new SongTools(new SongDataConfigFile(false)), args);
			//Console.ReadKey();
		}
		static void RunNew(SongTools tools, string[] args) {
			var dir = tools.ConfigFile.DataDirectory.CreateSubdirectory("inputlists");
			var m3us = args.Length == 0 ? dir.GetFiles("*.m3u?") : args.Select(s => new LFile(s)).Where(f => f.Exists);
			LDirectory m3uDir = args.Length == 0 ? tools.ConfigFile.DataDirectory.CreateSubdirectory("similarlists") : new LDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

			FuzzySongSearcher searchEngine = new FuzzySongSearcher(tools.SongFilesSearchData.Songs);

			foreach (var m3ufile in m3us) {
				try {
					using (new DTimer("Processing " + m3ufile.Name))
						ProcessM3U(tools, searchEngine, m3ufile.AsInfo(), m3uDir.AsInfo());
				} catch (Exception e) {
					Console.WriteLine("Unexpected error on processing " + m3ufile);
					Console.WriteLine(e.ToString());
				}
			}
		}

		static void FindPlaylistSongLocally(SongTools tools, ISongFileData playlistEntry, Action<SongFileData> ifFound, Action<SongRef> ifNotFound, Action<ISongFileData> cannotParse) {
			SongFileData bestMatch = null;
			if (tools.FindByPath.ContainsKey(playlistEntry.SongUri.ToString()))
				ifFound(tools.FindByPath[playlistEntry.SongUri.ToString()]);
			else if (playlistEntry.IsLocal && File.Exists(playlistEntry.SongUri.LocalPath)) {
				ifFound((SongFileData)SongFileDataFactory.ConstructFromFile(tools.ConfigFile.Sections.Select(cs => cs.BaseUri).Where(uri => uri != null).FirstOrDefault(uri => uri.IsBaseOf(playlistEntry.SongUri)), new LFile(playlistEntry.SongUri.LocalPath), tools.ConfigFile.PopularityEstimator));
			} else if (playlistEntry is PartialSongFileData) {
				int bestMatchVal = Int32.MaxValue;

				foreach (SongRef songref in SongRef.PossibleSongRefs(playlistEntry.HumanLabel)) {
					foreach (var songCandidate in tools.FindByName[songref]) {
						int candidateMatchVal = 100 * Math.Abs(playlistEntry.Length - songCandidate.Length) + Math.Min(199, Math.Abs(songCandidate.bitrate - 224));
						if (candidateMatchVal < bestMatchVal) {
							bestMatchVal = candidateMatchVal;
							bestMatch = songCandidate;
						}
					}
				}
				if (bestMatch != null) ifFound(bestMatch);
				else {
					var songref = SongRef.PossibleSongRefs(playlistEntry.HumanLabel).FirstOrDefault();
					if (songref != null) ifNotFound(songref);
					else cannotParse(playlistEntry);
				}
			} else {
				cannotParse(playlistEntry);
			}
		}

		static void ProcessM3U(SongTools tools, FuzzySongSearcher fuzzySearcher, FileInfo m3ufile, DirectoryInfo m3uDir) {
			Console.WriteLine("Trying " + m3ufile.FullName);
			IEnumerable<ISongFileData> playlist = SongFileDataFactory.LoadExtM3U(LFile.Construct(m3ufile));
			var known = new List<SongFileData>();
			var unknown = new List<SongRef>();
			foreach (var song in playlist)
				FindPlaylistSongLocally(tools, song,
					known.Add,
					unknown.Add,
					oopsSong => Console.WriteLine("Can't deal with: " + oopsSong.HumanLabel + "\nat:" + oopsSong.SongUri.ToString()));

			double lookupDur;
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.
			var res = FindSimilarPlaylist.ProcessPlaylist(tools, FindSimilarPlaylist.UncachedFuzzySearch(tools, fuzzySearcher), known, unknown, out lookupDur, 1000);

			FileInfo outputplaylist = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.m3u"));
			using (var stream = outputplaylist.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252)))
				SongFileDataFactory.WriteSongsToM3U(writer, res.knownTracks);

			FileInfo outputsimtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.txt"));
			using (var stream = outputsimtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				foreach (var track in res.similarList) {
					SongRef songref = tools.LastFmCache.LookupTrack.Execute(track.trackid);
					writer.WriteLine("{0} {2} {1}    [{3}]",
							track.cost,
							songref,
							tools.FindByName[songref].Any() ? "" : "***",
							string.Join(";  ", track.basedOn.Select(songWithCost => tools.LastFmCache.LookupTrack.Execute(songWithCost.trackid).ToString()).ToArray())
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
					let bestMatch = fuzzySearcher.FindAnyMatch(track)
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
	}
}

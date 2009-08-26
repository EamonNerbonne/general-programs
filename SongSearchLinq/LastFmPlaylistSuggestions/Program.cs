using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using MoreLinq;
using System.IO;
using LastFMspider;
using EmnExtensions.DebugTools;
using EmnExtensions;
using EmnExtensions.Collections;
namespace LastFmPlaylistSuggestions
{
	class Program
	{
		static void Main(string[] args) {
			//args.PrintAllDebug();
			//Console.ReadLine();
			RunNew(new LastFmTools(new SongDatabaseConfigFile(false)), args);
		}

		static void RunNew(LastFmTools tools, string[] args) {
			var dir = tools.DB.DatabaseDirectory.CreateSubdirectory("inputlists");
			var m3us = args.Length == 0 ? dir.GetFiles("*.m3u?") : args.Select(s => new FileInfo(s)).Where(f => f.Exists);
			DirectoryInfo m3uDir = args.Length == 0 ? tools.DB.DatabaseDirectory.CreateSubdirectory("similarlists") : new DirectoryInfo(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

			foreach (var m3ufile in m3us) {
				try {
					using (var dTimer = new DTimer("Processing " + m3ufile.Name))
						ProcessM3U(tools, m3ufile, m3uDir);
				} catch (Exception e) {
					Console.WriteLine("Unexpected error on processing " + m3ufile);
					Console.WriteLine(e.ToString());
				}
			}
		}

		static void FindPlaylistSongLocally(LastFmTools tools, ISongData playlistEntry, Action<SongData> ifFound, Action<SongRef> ifNotFound, Action<ISongData> cannotParse) {
			SongData bestMatch = null;
			int artistTitleSplitIndex = playlistEntry.HumanLabel.IndexOf(" - ");
			if (tools.Lookup.dataByPath.ContainsKey(playlistEntry.SongPath))
				ifFound(tools.Lookup.dataByPath[playlistEntry.SongPath]);
			else if (playlistEntry.IsLocal && File.Exists(playlistEntry.SongPath)) {
				ifFound((SongData)SongDataFactory.ConstructFromFile(new FileInfo(playlistEntry.SongPath)));
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


		public class SongWithCost : IComparable<SongWithCost>
		{
			public SongRef songref;
			public double cost = double.PositiveInfinity;
			public int index = -1;
			public HashSet<SongRef> basedOn = new HashSet<SongRef>();

			public int CompareTo(SongWithCost other) { return cost.CompareTo(other.cost); }
		}

		public class SongWithCostCache
		{
			Dictionary<SongRef, SongWithCost> songCostLookupDict = new Dictionary<SongRef, SongWithCost>();
			public SongWithCost Lookup(SongRef song) {
				SongWithCost retval;
				if (songCostLookupDict.TryGetValue(song, out retval))
					return retval;
				retval = new SongWithCost { songref = song };
				songCostLookupDict.Add(song, retval);
				return retval;
			}
		}

		static void ProcessM3U(LastFmTools tools, FileInfo m3ufile, DirectoryInfo m3uDir) {
			Console.WriteLine("Trying " + m3ufile.FullName);
			var playlist = LoadExtM3U(m3ufile);
			var known = new List<SongData>();
			var unknown = new List<SongRef>();
			foreach (var song in playlist)
				FindPlaylistSongLocally(tools, song,
					(songData) => { known.Add(songData); },
					(songRef) => { unknown.Add(songRef); },
					(oopsSong) => { Console.WriteLine("Can't deal with: " + oopsSong.HumanLabel + "\nat:" + oopsSong.SongPath); }
				);
			//OK, so we now have the playlist in the var "playlist" with knowns in "known" except for the unknowns, which are in "unknown" as far as possible.

			var playlistSongRefs = new HashSet<SongRef>(known.Select(sd => SongRef.Create(sd)).Where(sr => sr != null).Cast<SongRef>().Concat(unknown));

			SongWithCostCache songCostCache = new SongWithCostCache();
			Heap<SongWithCost> songCosts = new Heap<SongWithCost>((sc, index) => { sc.index = index; });
			List<SongWithCost> similarList = new List<SongWithCost>();

			playlistSongRefs
				.Select(songref => songCostCache.Lookup(songref))
				.ForEach(songcost => { songcost.cost = 0.0; songcost.basedOn.Add(songcost.songref); songCosts.Add(songcost); });

			int lastPercent = 0;
			while (similarList.Count < 1000) {
				SongWithCost currentSong;
				if (!songCosts.RemoveTop(out currentSong))
					break;
				if (!playlistSongRefs.Contains(currentSong.songref))
					similarList.Add(currentSong);


				var nextSimlist = tools.SimilarSongs.Lookup(currentSong.songref);
				if (nextSimlist == null)
					continue;

				int simRank = 0;
				foreach (var similarTrack in nextSimlist.similartracks) {
					SongWithCost similarSong = songCostCache.Lookup(similarTrack.similarsong);
					double directCost = currentSong.cost + 1.0 + simRank / 20.0;
					simRank++;
					if (similarSong.index == -1) {
						similarSong.cost = directCost;
						foreach (var baseSong in currentSong.basedOn)
							similarSong.basedOn.Add(baseSong);
						songCosts.Add(similarSong);
					} else if (similarSong.cost > currentSong.cost) {
						songCosts.Delete(similarSong.index);
						similarSong.index = -1;
						//new cost should be somewhere between next.cost, and min(old-cost, direct-cost)
						double oldOffset = similarSong.cost - currentSong.cost;
						double newOffset = directCost - currentSong.cost;
						double combinedOffset = 1.0 / (1.0 / oldOffset + 1.0 / newOffset);
						similarSong.cost = currentSong.cost + combinedOffset;
						foreach (var baseSong in currentSong.basedOn)
							similarSong.basedOn.Add(baseSong);
						songCosts.Add(similarSong);
					}
				}
				if (similarList.Count / 10 > lastPercent) {
					lastPercent = similarList.Count / 10;
					var msg = "[" + songCosts.Count + ";" + lastPercent + "%]";
					Console.Write(msg.PadRight(16, ' '));
				}

			}

			var knownTracks =
				(from simtrack in similarList
				 where tools.Lookup.dataByRef.ContainsKey(simtrack.songref)
				 select
					(from songcandidate in tools.Lookup.dataByRef[simtrack.songref]
					 orderby Math.Abs(songcandidate.bitrate - 224)
					 select songcandidate).First()).ToArray()
				;

			Console.WriteLine("{0} similar tracks found, of which {1} found locally.", similarList.Count, knownTracks.Length);

			FileInfo outputplaylist = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.m3u"));
			using (var stream = outputplaylist.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
				writer.WriteLine("#EXTM3U");
				foreach (var track in knownTracks) {
					writer.WriteLine("#EXTINF:" + track.Length + "," + track.HumanLabel + "\n" + track.SongPath);
				}
			}
			FileInfo outputsimtracks = new FileInfo(Path.Combine(m3uDir.FullName, Path.GetFileNameWithoutExtension(m3ufile.Name) + "-similar.txt"));
			using (var stream = outputsimtracks.Open(FileMode.Create))
			using (var writer = new StreamWriter(stream, Encoding.GetEncoding(1252))) {
				foreach (var track in similarList) {
					writer.WriteLine("{0} {2} {1}    [{3}]",
							track.cost,
							track.songref.ToString(),
							tools.Lookup.dataByRef.ContainsKey(track.songref) ? "" : "***",
							string.Join(";  ", track.basedOn.Select(sr => sr.ToString()).ToArray())
						);
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

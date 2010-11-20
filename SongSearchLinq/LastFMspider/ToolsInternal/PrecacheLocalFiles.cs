using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using System.Globalization;

namespace LastFMspider {
	internal static partial class ToolsInternal {

		public static void PrecacheLocalFiles(LastFmTools tools, bool shuffle) {
			var SimilarSongs = tools.SimilarSongs;
			var Lookup = tools.Lookup;
			int ttCount = 0; object sync = new object();
			Console.WriteLine("Caching Top tracks");
			var artists = tools.SongsOnDisk.Songs.Select(song => song.artist).Where(artist => artist != null).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
			Parallel.ForEach(artists, new ParallelOptions { MaxDegreeOfParallelism = 10, }, artist => {
				try {
					var ttList = SimilarSongs.LookupTopTracks(artist, TimeSpan.FromDays(100.0));
					var saList = SimilarSongs.LookupSimilaArtists(artist, TimeSpan.FromDays(100.0));
					lock (sync) {
						ttCount++;
						if (100 * (ttCount - 1) / artists.Length != 100 * ttCount / artists.Length)
							Console.WriteLine("{0}%", 100 * ttCount / artists.Length);
					}
				} catch (Exception e) {
					tools.LogNonFatalError("Precaching artist data failed for artist " + (artist ?? "<null>"), e);
				}
			});

			Console.WriteLine("Loading song database...");
			if (tools.SongsOnDisk.InvalidDataCount != 0)
				Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", tools.SongsOnDisk.InvalidDataCount);
			Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", tools.SongsOnDisk.Songs.Length);
			SongRef[] songsToDownload = tools.SongsOnDisk.Songs.Select(SongRef.Create).Where(sd => sd != null).ToArray();
			if (shuffle)
				songsToDownload.Shuffle();
			tools.UnloadDB();
			Console.WriteLine("Downloading Last.fm similar tracks...");
			int progressCount = 0;
			int total = songsToDownload.Length;
			int hits = 0, newSongs = 0;
			int songCountNrOfDigits = songsToDownload.Length.ToString(CultureInfo.InvariantCulture).Length;
			string progressFmtString = "{0,3}%   new={2," + songCountNrOfDigits + "}, hits={1," + songCountNrOfDigits + "},  total={3," + songCountNrOfDigits + "}; {4}";

			//foreach( var songref in songsToDownload) { 
			//since parallelising this can't work DB-wise, a plain foreach is almost always faster.  
			//However, when foreach is faster, it's only by a tiny amount, and when indeed it needs to precache a lot (i.e. network, i.e. quite parallelisable), it's much slower...
			Parallel.ForEach(songsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 10, }, songref => {
				try {
					lock (songsToDownload) {
						progressCount++;
					}
					var info = SimilarSongs.EnsureCurrent(songref, TimeSpan.FromDays(100.0));
					lock (songsToDownload) {
						if (info.Item1.StatusCode == 0)
							hits++;
						if (info.Item2 != null)
							newSongs++;
						if (info.Item2 != null || 100 * (progressCount - 1) / total != 100 * progressCount / total)
							Console.WriteLine(progressFmtString,
								 100 * progressCount / total,
								hits,
								newSongs,
								progressCount,
								(info.Item2 != null ?
								"  current: \"" + songref + "\" new with " + (info.Item2.similartracks == null ? 0 : info.Item2.similartracks.Length) + " similar tracks" : "")

									);

					}
				} catch (Exception e) {
					tools.LogNonFatalError("Precaching song similarity data failed for song " + (songref.ToString() ?? "<null>"), e);
				}

			}
			);

			Console.WriteLine("Done precaching.");
		}
	}
}

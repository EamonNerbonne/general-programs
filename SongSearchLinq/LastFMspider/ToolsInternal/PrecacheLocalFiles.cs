using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmnExtensions.Algorithms;
using LastFMspider.LastFMSQLiteBackend;
using LastFMspider.OldApi;
using System.Globalization;

namespace LastFMspider
{
	internal static partial class ToolsInternal
	{

		public static void PrecacheLocalFiles(LastFmTools tools, bool shuffle) {
			var DB = tools.DB;
			var SimilarSongs = tools.SimilarSongs;
			var Lookup = tools.Lookup;

			Console.WriteLine("Loading song database...");
			if (DB.InvalidDataCount != 0)
				Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
			Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
			SongRef[] songsToDownload = Lookup.dataByRef.Keys.ToArray();
			if (shuffle)
				songsToDownload.Shuffle();
			tools.UnloadDB();
			Console.WriteLine("Downloading Last.fm similar tracks...");
			int progressCount = 0;
			int total = songsToDownload.Length;
			int hits=0, newSongs=0;
			int songCountNrOfDigits = songsToDownload.Length.ToString(CultureInfo.InvariantCulture).Length;
			string progressFmtString = "{0,3}%   new={2," + songCountNrOfDigits + "}, hits={1," + songCountNrOfDigits + "},  total={3," + songCountNrOfDigits + "}; {4}";

			//foreach( var songref in songsToDownload) { 
			//since parallelising this can't work DB-wise, a plain foreach is almost always faster.  
			//However, when foreach is faster, it's only by a tiny amount, and when indeed it needs to precache a lot (i.e. network, i.e. quite parallelisable), it's much slower...
			Parallel.ForEach(songsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 10, }, songref => {
				lock (songsToDownload) {
					progressCount++;
				}
				var info = SimilarSongs.EnsureCurrent(songref, TimeSpan.FromDays(100.0));
				lock (songsToDownload) {
					if (info.Item1.StatusCode == 0)
						hits++;
					if (info.Item2 != null)
						newSongs++;
					if(info.Item2!=null || 100 * (progressCount - 1) / total != 100 * progressCount / total)
					Console.WriteLine(progressFmtString,
						 100 * progressCount / total,
						hits,
						newSongs,
						progressCount,
						(info.Item2!=null?
						"  current: \""+songref+"\" new with " + (info.Item2.similartracks == null ? 0 : info.Item2.similartracks.Length) + " similar tracks":"")

							);
					
				}
			}
			);
			Console.WriteLine("Done precaching.");
		}
	}
}

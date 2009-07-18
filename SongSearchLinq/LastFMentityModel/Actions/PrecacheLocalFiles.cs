using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using EmnExtensions.Algorithms;

namespace LastFMentityModel.Actions
{
	public static partial class LfmAction
	{
		/// <summary>
		/// Downloads Last.fm metadata for all tracks in the song database (if not already present).
		/// </summary>
		/// <param name="shuffle">Whether to perform the precaching in a random order.  Doing so slows down the precaching when almost all
		/// items are already downloaded, but permits multiple download threads to run in parallel without duplicating downloads.</param>
	/*	public static void PrecacheLocalFiles(bool shuffle, SimpleSongDB DB, LastFMCacheModel model) {
			Console.WriteLine("Loading song database...");
			if (DB.InvalidDataCount != 0) Console.WriteLine("Ignored {0} songs with unknown tags (should be 0).", DB.InvalidDataCount);
			Console.WriteLine("Taking those {0} songs and indexing em by artist/title...", DB.Songs.Count);
			SongRef[] songsToDownload = DB.Songs.Select(sd=>SongRef.Create(sd)).Distinct().ToArray();
			if (shuffle) songsToDownload.Shuffle();
			DB = null;
			Console.WriteLine("Downloading Last.fm similar tracks...");
			int progressCount = 0;
			int total = songsToDownload.Length;
			long similarityCount = 0;
			int hits = 0;
			foreach (SongRef songref in songsToDownload) {
				try {
					progressCount++;
					var similar = DownloadIfNotPresent(model, songref);//precache the last.fm data.  unsure - NOT REALLY necessary?
					int newSimilars = similar == null || similar.similartracks == null ? 0 : similar.similartracks.Length;
					similarityCount += newSimilars;
					if (similar != null)
						hits++;
					Console.WriteLine("{0,3} - tot={4} in hits={5}, with relTo={3} in \"{1} - {2}\"",
						100 * progressCount / (double)total,
						songref.Artist,
						songref.Title,
						newSimilars,
						(double)similarityCount,
						hits);

				} catch (Exception e) {
					Console.WriteLine("Exception: {0}", e.ToString());
				}//ignore all errors.
			}
			Console.WriteLine("Done precaching.");
		}

		private static object DownloadIfNotPresent(LastFMCacheModel model, SongRef songref) {
			var tracks = from artist in model.Artist
						 where artist.LowercaseArtist == songref.Artist.ToLatinLowercase()
						 from track in artist.Tracks
						 where track.LowercaseTitle ==songref.Title.ToLatinLowercase()
						 select track;
				
			Track t = tracks.FirstOrDefault();
			if(t == null);

			if(model.Artist.Where(a=>a.LowercaseArtist == songref.Artist.ToLatinLowercase())
			throw new NotImplementedException();
		}
	 */


	}
}

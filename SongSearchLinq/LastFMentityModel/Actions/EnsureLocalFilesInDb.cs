using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using LastFMspider;
using EmnExtensions.Algorithms;
using System.Diagnostics;

namespace LastFMentityModel.Actions
{
	public static partial class LfmAction
	{

		public static void EnsureLocalFilesInDb(SimpleSongDB db, LastFMCacheModel model) {
			Stopwatch overall = Stopwatch.StartNew();
			Stopwatch timer = Stopwatch.StartNew();
			Console.WriteLine("Getting songdata");
			var songrefs = (from songdata in db.Songs
						   let songref = SongRef.Create(songdata)
						   where songref != null
						   select songref).Distinct().ToArray();
			timer.PrintTimeRes("loaded songrefs:", songrefs.Length);

			var newArtists = (from artist in songrefs.Select(sr => sr.Artist).Distinct()
							 let lowerArtist = artist.ToLatinLowercase()
							 where !model.Artist.Any(a => a.LowercaseArtist == lowerArtist)
							 select new Artist { FullArtist = artist, LowercaseArtist = lowerArtist }).ToArray();
			timer.PrintTimeRes("newed artists:", newArtists.Length);

			foreach (var newArtist in newArtists) {
				model.AddToArtist(newArtist);
				Console.WriteLine("Added {0}", newArtist.FullArtist);
			}
			timer.PrintTimeRes("added artists:", newArtists.Length);
			model.SaveChanges();
			timer.PrintTimeRes("saved changes", null);

			var newTracks = (from songref in songrefs
							let lowercaseArtist = songref.Artist.ToLatinLowercase()
							let artist = model.Artist.First(a => a.LowercaseArtist == lowercaseArtist)
							let lowerTitle = songref.Title.ToLatinLowercase()
							where !model.Track.Any(t => t.LowercaseTitle == lowerTitle)
							select new Track { LowercaseTitle = lowerTitle, FullTitle = songref.Title, Artist = artist }).ToArray(); ;
			timer.PrintTimeRes("newed tracks:", newTracks.Length);

			foreach (var newTrack in newTracks) {
				model.AddToTrack(newTrack);
				Console.WriteLine("Added {0}", newTrack.Artist.FullArtist + " - " + newTrack.FullTitle);
			}
			timer.PrintTimeRes("added tracks:", newArtists.Length);
			model.SaveChanges();
			timer.PrintTimeRes("saved changes", null);
			Console.WriteLine("Overall, took {0}.", overall.Elapsed);
		}
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

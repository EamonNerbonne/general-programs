using System;
using System.Collections.Generic;
using EmnExtensions;
using SongDataLib;
using EmnExtensions.DebugTools;
using LastFMspider;
using System.Diagnostics;

namespace SongDiskCrawler {
	static class SongDiskCrawlerMain {
		static int counter,newsongs,lastcounter;
		static double lastprogressprint;
		static readonly Dictionary<string, ISongFileData> songs = new Dictionary<string, ISongFileData>();
		static void SongHandler(ISongFileData song, double ratio) {
			if (song.IsLocal) {
				songs[song.SongUri.ToString()] = song;
			}
			//SongData songdata = song as SongData;
			//if (songdata != null)
			//    Console.WriteLine("{1}:  {0}", songdata.popularity, songdata.HumanLabel);
			//else
			//    Console.WriteLine("?:  {0}", songdata.popularity, songdata.HumanLabel);
			counter++;
			double seconds = timer.Elapsed.TotalSeconds - lastprogressprint;
			if (seconds > 10.0) {
				lastprogressprint = timer.Elapsed.TotalSeconds;
				Console.WriteLine("{0:f1}%, at {1:f1} nodes/sec: ", ratio * 100, (counter - lastcounter) / seconds);
				lastcounter = counter;
			}
		}
		static Stopwatch timer;
		static ISongFileData LookupSong(Uri localSongPath) {
			ISongFileData retval = null;
			songs.TryGetValue(localSongPath.ToString(), out retval);
			if (retval == null) newsongs++;
			return retval;
		}
		static void Main(string[] args) {


#if !DEBUG
			try {
#endif
			SongDataConfigFile dcf = F.Create(() => new SongDataConfigFile(true)).TimeFunc("Loading Config file...");
			dcf.PopularityEstimator = new LastFmPopularityEstimator(new SongTools(dcf));

			timer = Stopwatch.StartNew();
			using (new DTimer("Loading songs from database..."))
				dcf.Load(SongHandler);
			Console.WriteLine("Found: " + songs.Count);
			lastcounter = counter = 0;
			timer = Stopwatch.StartNew();
			using (new DTimer("Rescanning files..."))
				dcf.RescanAndSave(LookupSong, SongHandler);
			Console.WriteLine("Songs found: " + counter + " of which " + newsongs + " new.");
#if !DEBUG
			}
			catch (Exception e) {
				Console.WriteLine("==========================");
				Console.WriteLine("===TERMINAL ERROR=========");
				Console.WriteLine("==========================");
				Console.WriteLine(e.ToString());
				Console.WriteLine("Press any key to ABORT...");
				Console.ReadKey();
				throw;
			}
#endif
		}
	}
}
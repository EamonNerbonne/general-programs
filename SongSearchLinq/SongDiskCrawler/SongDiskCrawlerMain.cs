using System;
using System.Collections.Generic;
using EmnExtensions;
using SongDataLib;
using EmnExtensions.DebugTools;
using LastFMspider;

namespace SongDiskCrawler {

	class SongDiskCrawlerMain {
		static int counter = 0;
		static int newsongs = 0;
		static double lastmark = 0;
		static int lastcounter = 0;
		static Dictionary<string, ISongData> songs = new Dictionary<string, ISongData>();
		static void SongHandler(ISongData song, double ratio) {
			if (song.IsLocal) {
				songs[song.SongUri.ToString()] = song;
			}
			//SongData songdata = song as SongData;
			//if (songdata != null)
			//    Console.WriteLine("{1}:  {0}", songdata.popularity, songdata.HumanLabel);
			//else
			//    Console.WriteLine("?:  {0}", songdata.popularity, songdata.HumanLabel);
			counter++;
			double seconds = (timer.ElapsedSinceMark.TotalSeconds - lastmark);
			if (seconds > 10.0) {
				lastmark = timer.ElapsedSinceMark.TotalSeconds;
				Console.WriteLine("{0:f1}%, at {1:f1} nodes/sec: ", ratio * 100, (counter - lastcounter) / seconds);
				lastcounter = counter;
			}
		}
		static ISongData LookupSong(Uri localSongPath) {
			ISongData retval = null;
			songs.TryGetValue(localSongPath.ToString(), out retval);
			if (retval == null) newsongs++;
			return retval;
		}
		static NiceTimer timer;
		static void Main(string[] args) {


#if !DEBUG
			try {
#endif
			timer = new NiceTimer();
			timer.TimeMark("Loading Config file...");
			SongDatabaseConfigFile dcf = new SongDatabaseConfigFile(true);
			//dcf.PopularityEstimator = new LastFmPopularityEstimator(new LastFmTools(dcf));

			timer.TimeMark("Loading songs from database...");

			dcf.Load(SongHandler);
			Console.WriteLine("Found: " + songs.Count);
			lastcounter = counter = 0;
			timer.TimeMark("Rescanning files...");
			dcf.RescanAndSave(LookupSong, SongHandler);
			Console.WriteLine("Songs found: " + counter + " of which " + newsongs + " new.");
			timer.TimeMark(null);
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
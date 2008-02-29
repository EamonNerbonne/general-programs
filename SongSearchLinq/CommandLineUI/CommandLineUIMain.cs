using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EamonExtensionsLinq;
using EamonExtensionsLinq.DebugTools;
using SongDataLib;
namespace CommandLineUI
{
	class CommandLineUIMain
	{
		SearchableSongDB searchEngine;

		public static NiceTimer timer;

		static void Main(string[] args) {
#if !DEBUG
			try {
#endif
				timer = new NiceTimer("Starting");
				var prog = new CommandLineUIMain(args.Length > 0 ? new FileInfo(args[0]) : null);
				timer.TimeMark(null);
				prog.ExecBenchmark();
				//prog.ExecUI();
#if !DEBUG
			} catch(Exception e) {
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

		public void ExecBenchmark() {
			Random r = new Random(1337);
			for(int i = 0; i < 1000; i++) {
				int si = r.Next(searchEngine.db.songs.Length);
				string info = searchEngine.db.songs[si].FullInfo;
				int stringend = r.Next(5, info.Length);
				int stringstart = Math.Max(r.Next(stringend), r.Next(stringend));
				string[] queries = info.Substring(stringstart, stringend - stringstart).Split(' ', '\n');
				string[] sq = queries.Select(s => (s.Length > 2 ? s.Substring(Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.Select(s => (s.Length > 2 ? s.Substring(0, s.Length - Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.ToArray();
				searchEngine.Search(string.Join(" ", sq)).Take(30).ToArray();
			}
		}
		int counter = 0;
		int seconds = 0;
		bool UpdateHandler(string msg) {
			counter++;
			if(timer.ElapsedSinceMark.Seconds > seconds) {
				seconds = timer.ElapsedSinceMark.Seconds;
				Console.WriteLine("Nodes/sec: " + (counter / timer.ElapsedSinceMark.TotalSeconds));
				if(msg != null) Console.WriteLine(msg);
			}
			return true;
		}

		public CommandLineUIMain(FileInfo dbconfigfile) {
			timer.TimeMark("Loading DB");
			SongDatabaseConfigFile dcf =
				dbconfigfile == null ?
				new SongDatabaseConfigFile(true) :
				new SongDatabaseConfigFile(dbconfigfile,true);
			List<ISongData> loadingSongs = new List<ISongData>();
			dcf.Load(delegate(ISongData newsong, double progress) {
				loadingSongs.Add(newsong);
			});
			SongDB db = new SongDB(loadingSongs);
			loadingSongs = null;
			timer.TimeMark("Loading Search plugin");
			ISongSearcher ss = //new BwtLib.SongBwtSearcher();
				new SuffixTreeLib.SuffixTreeSongSearcher();
			searchEngine = new SearchableSongDB(db, ss);
		}


		public void ExecUI() {
			string input = "";
			while(input != null) {
				DateTime dtA = DateTime.Now;
				Console.WriteLine("======RESULTS======= in {0} songs.=========", searchEngine.db.songs.Length);

				searchEngine.Search(input).Take(20).Select(songdata => songdata.HumanLabel).PrintAllDebug();

				DateTime dtB = DateTime.Now;
				Console.WriteLine("in " + (dtB - dtA).TotalSeconds + " secs.");
				Console.WriteLine();
				Console.Write("Query (Esc to {0}): {1}", input.Length == 0 ? "EXIT" : "Reset", input);
				do {
					ConsoleKeyInfo key = Console.ReadKey(true);
					if(key.Key == ConsoleKey.Escape) {
						if(input == "") {
							input = null;
							break;
						} else
							input = "";
					} else if(key.Key == ConsoleKey.Backspace)
						input = input.Substring(0, Math.Max(input.Length - 1, 0));
					else if(key.KeyChar >= ' ')
						input += key.KeyChar;
				} while(Console.KeyAvailable);
				Console.WriteLine();
			}
		}
	}
}

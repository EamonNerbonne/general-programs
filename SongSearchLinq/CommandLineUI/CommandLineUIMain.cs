using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmnExtensions.DebugTools;
using SongDataLib;
namespace CommandLineUI {
	class CommandLineUIMain {
		readonly SearchableSongFiles searchEngine;


		static void Main(string[] args) {
#if !DEBUG
			try {
#endif

				var prog = DTimer.TimeFunc(() => new CommandLineUIMain(args.Length > 0 ? new FileInfo(args[0]) : null), "Starting");
				//prog.ExecBenchmark();
				prog.ExecUI();
#if !DEBUG
			} catch (Exception e) {
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
			for (int i = 0; i < 1000; i++) {
				int si = r.Next(searchEngine.db.songs.Length);
				string info = searchEngine.db.songs[si].FullInfo;
				int stringend = r.Next(5, info.Length);
				int stringstart = Math.Max(r.Next(stringend), r.Next(stringend));
				string[] queries = info.Substring(stringstart, stringend - stringstart).Split(' ', '\n');
				string[] sq = queries.Select(s => (s.Length > 2 ? s.Substring(Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.Select(s => (s.Length > 2 ? s.Substring(0, s.Length - Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.ToArray();
				searchEngine.Search(string.Join(" ", sq)).Take(100).ToArray();
			}
		}

		CommandLineUIMain(FileInfo dbconfigfile) {
			SongFilesSearchData db = DTimer.TimeFunc(() => {
				SongDataConfigFile dcf =
					dbconfigfile == null ?
					new SongDataConfigFile(true) :
					new SongDataConfigFile(dbconfigfile, true);
				BlockingCollection<ISongFileData> loadingSongs = new BlockingCollection<ISongFileData>();
				Task.Factory.StartNew(() => {
					dcf.Load((newsong, progress) => loadingSongs.Add(newsong));
					loadingSongs.CompleteAdding();
				});
				return new SongFilesSearchData(loadingSongs.GetConsumingEnumerable());
			}, "Loading DB");
			searchEngine = DTimer.TimeFunc(() => new SearchableSongFiles(db, null), "Loading Search plugin");//new SuffixTreeLib.SuffixTreeSongSearcher()
		}


		void ExecUI() {
			string input = "";
			while (input != null) {
				DateTime dtA = DateTime.Now;
				Console.WriteLine("======RESULTS======= in {0} songs.=========", searchEngine.db.songs.Length);

				searchEngine.Search(input).Take(20).Select(songdata => songdata.HumanLabel).PrintAllDebug();

				DateTime dtB = DateTime.Now;
				Console.WriteLine("in " + (dtB - dtA).TotalSeconds + " secs.");
				Console.WriteLine();
				Console.Write("Query (Esc to {0}): {1}", input.Length == 0 ? "EXIT" : "Reset", input);
				do {
					ConsoleKeyInfo key = Console.ReadKey(true);
					if (key.Key == ConsoleKey.Escape) {
						if (input == "") {
							input = null;
							break;
						} else
							input = "";
					} else if (key.Key == ConsoleKey.Backspace)
						input = input.Substring(0, Math.Max(input.Length - 1, 0));
					else if (key.KeyChar >= ' ')
						input += key.KeyChar;
				} while (Console.KeyAvailable);
				Console.WriteLine();
			}
		}
	}
}

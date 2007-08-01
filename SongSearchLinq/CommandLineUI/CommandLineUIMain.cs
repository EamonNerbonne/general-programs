using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SongDataLib;
using System.IO;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Text;
namespace CommandLineUI
{
	class CommandLineUIMain
	{
		SearchableSongDB searchEngine;

		public static NiceTimer timer;

		static void Main(string[] args) {
			timer = new NiceTimer("Starting");
			var prog = new CommandLineUIMain(args.Select(str => new FileInfo(str)));
			timer.TimeMark(null);
			//prog.ExecBenchmark();
			prog.ExecUI();
		}

		public void ExecBenchmark() {
			Random r = new Random(1337);
			for(int i = 0; i < 1000; i++) {
				int si = r.Next(searchEngine.db.songs.Length);
				string info = searchEngine.db.songs[si].FullInfo;
				int stringend = r.Next(5, info.Length);
				int stringstart = Math.Max(r.Next(stringend), r.Next(stringend));
				string[] queries = info.Substring(stringstart, stringend - stringstart).Split(' ', '\t');
				string[] sq = queries.Select(s => (s.Length > 2 ? s.Substring(Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.Select(s => (s.Length > 2 ? s.Substring(0, s.Length - Math.Min(r.Next(s.Length - 2), r.Next(s.Length - 2))) : s))
											.ToArray();
				searchEngine.Search(string.Join(" ", sq)).Take(30).ToArray();
			}
		}

		public CommandLineUIMain(IEnumerable<FileInfo> dbfiles) {
			timer.TimeMark("Loading DB");
			SongDB db = new SongDB( dbfiles);
			timer.TimeMark("Loading Search plugin");
			ISongSearcher ss = new SuffixTreeLib.SuffixTreeSongSearcher();
			searchEngine = new SearchableSongDB(db, ss);
		}


		public void ExecUI() {
			string input = "";
			while(input != "EXIT") {
				DateTime dtA = DateTime.Now;
				Console.WriteLine("======RESULTS======= in {0} songs.=========", searchEngine.db.songs.Length);

				searchEngine.Search(input).Take(5).Select(songdata => songdata.filepath).PrintAllDebug();

				DateTime dtB = DateTime.Now;
				Console.WriteLine("in " + (dtB - dtA).TotalSeconds + " secs.");
				Console.WriteLine();
				Console.Write("Query (Esc to EXIT): " + input);
				ConsoleKeyInfo key = Console.ReadKey(true);
				if(key.Key == ConsoleKey.Escape) {
					if(input == "")
						break;
					else
						input = "";
				} else if(key.Key == ConsoleKey.Backspace)
					input = input.Substring(0, Math.Max(input.Length - 1, 0));
				else if(key.KeyChar >= ' ')
					input += key.KeyChar;
			}
		}
	}
}

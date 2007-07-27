using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SongDataLib;
using System.IO;
using EamonExtensionsLinq.DebugTools;
using EamonExtensionsLinq;
using EamonExtensionsLinq.Text;
namespace CommandLineUI {
    class CommandLineUIMain {
        SongDB db;
        ISongSearcher ss;

        public IEnumerable<int> MatchAll(SearchResult[] results, string[] queries) {
            Array.Sort(results, queries);
            IEnumerable<int> smallestMatch = results[0].songIndexes;
            //queries are still in the "best" possible order!
            foreach (int si in smallestMatch) {
                string songtext = db.NormalizedSong(si);
                if (queries.Skip(1).All(q => songtext.Contains(q)))
                    yield return si;
            }
        }
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
            for (int i = 0; i < 1000; i++) {
                int si = r.Next(db.songs.Length);
                string info = db.songs[si].FullInfo;
                int stringend = r.Next(5,info.Length);
                int stringstart = Math.Max(r.Next(stringend), r.Next(stringend));
                string[] queries = info.Substring(stringstart,stringend-stringstart).Split(' ', '\t');
                string[] sq = queries.Select(s=>(s.Length>2?s.Substring(Math.Min(r.Next(s.Length-2),r.Next(s.Length-2))):s))
                                     .Select(s=>(s.Length>2?s.Substring(0,s.Length-Math.Min(r.Next(s.Length-2),r.Next(s.Length-2))):s))
                                     .ToArray();
                Matches(string.Join(" ", sq)).Take(30).ToArray();
            }
        }

        public CommandLineUIMain(IEnumerable<FileInfo> dbfiles) {
            timer.TimeMark("Loading DB");
            db = new SongDB(EamonExtensionsLinq.Text.Canonicalize.Basic, dbfiles);
            timer.TimeMark("Loading Search plugin");
            ss = new BwtLib.SongBwtSearcher();//new SuffixTreeLib.SuffixTreeSongSearcher();
            ss.Init(db);
        }

        IEnumerable<int> Matches(string querystring) {
            string[] query = querystring.Split(' ').Select(q => Canonicalize.Basic(q)).ToArray();
            SearchResult[] res = query.Select(q => ss.Query(q)).ToArray();
            return MatchAll(res, query);
        }

        public void ExecUI(){
            string input = "";
            while (input != "EXIT") {
                DateTime dtA = DateTime.Now;
                Console.WriteLine("======RESULTS======= in {0} songs.=========",db.songs.Length);

                Matches(input).Take(5).Select(si=>db.songs[si].filepath).PrintAllDebug();

                DateTime dtB = DateTime.Now;
                Console.WriteLine("in " + (dtB - dtA).TotalSeconds + " secs.");
                Console.WriteLine();
                Console.Write("Query (Esc to EXIT): " + input);
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape) {
                    if (input == "")
                        break;
                    else
                        input = "";
                } else if (key.Key == ConsoleKey.Backspace)
                    input = input.Substring(0, Math.Max(input.Length - 1, 0));
                else if(key.KeyChar >= ' ')
                    input += key.KeyChar;
            }
        }
    }
}

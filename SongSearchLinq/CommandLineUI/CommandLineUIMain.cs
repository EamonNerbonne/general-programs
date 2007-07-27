using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;
using SongDataLib;
using System.IO;
using EamonExtensions.DebugTools;
using EamonExtensions;
namespace CommandLineUI {
    class CommandLineUIMain {
        SongDB db;
        ISongSearcher ss;

        public IEnumerable<int> MatchAll(SearchResult[] results, byte[][] queries) {
            Array.Sort(results, queries);
            IEnumerable<int> smallestMatch = results[0].songIndexes;
            //queries are still in the "best" possible order!
            foreach (int si in smallestMatch) {
                byte[] elem = db.norm(db.songs[si].FullInfo).ToArray();
                if (queries.Skip(1).All(q => SongUtil.Contains(elem, q)))
                    yield return si;
            }
        }
        public static NiceTimer timer;

        static void Main(string[] args) {
            timer = new NiceTimer("Starting");
            var prog = new CommandLineUIMain(new FileInfo(args[0]));
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

        public CommandLineUIMain(FileInfo dbfile) {
            timer.TimeMark("Loading DB");
            db = new SongDB(dbfile,SongUtil.makeCanonical);
            timer.TimeMark("Loading Search plugin");
            ss = new BwtLib.SongBwtSearcher();//new SuffixTreeLib.SuffixTreeSongSearcher();
            ss.Init(db);
        }

        IEnumerable<int> Matches(string querystring) {
            byte[][] query = querystring.Split(' ').Select(q => SongUtil.makeCanonical(q).ToArray()).ToArray();
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

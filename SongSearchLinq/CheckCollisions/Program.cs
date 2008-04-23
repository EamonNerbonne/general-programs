using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SongDataLib;
using System.IO;
using LastFMspider;

namespace CheckCollisions
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading Song Database...");
            SimpleSongDB db = new SimpleSongDB(new SongDatabaseConfigFile(new FileInfo(args[0]), false),null);
            Console.WriteLine("Loaded {0} songs.", db.Songs.Count);
            Console.WriteLine("Checking for hash collisions:");
            var res = from songref in (
                        from song in db.Songs
                        let songref = SongRef.Create(song)
                        where songref!=null
                        select songref
                        ).Distinct()
                      group songref by songref.hashcode into g
                      let groupC = g.Count()
                      where groupC > 1
                      orderby groupC descending
                      select new { Count = groupC, Songs = g };
            res=res.ToArray();
            Console.WriteLine("Found {0} collisions",res.Count());

            foreach (var collisionset in res)
            {
                Console.WriteLine("{0} hits: {1}", collisionset.Count, string.Join(", ", collisionset.Songs.Select(songref => songref.Artist + "-" + songref.Title).ToArray()));
            }
            Console.WriteLine("Summary: {0} clashing refs", res.Select(set => set.Count - 1).Sum());
            Console.ReadLine();
        }
    }
}

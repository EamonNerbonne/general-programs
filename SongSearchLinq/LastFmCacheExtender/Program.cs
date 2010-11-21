using System;
using LastFMspider;
using SongDataLib;

namespace LastFmCacheExtender
{
	static class Program
    {
        static SongTools tools;
        static void Main(string[] args) {
            
            var config = new SongDataConfigFile(false);
            Console.WriteLine("Loading song similarity...");
            tools = new SongTools(config);
			//tools.PrecacheLocalFiles();//might want to do this first, but...
			tools.SimilarSongs.EnsureLocalFilesInDB();//this is much faster, of course.
            tools.UnloadDB();
            Console.WriteLine("go!");
            ExtendSimilarities();
        }

		static void ExtendSimilarities() {
            bool run = true;
            int errCount = 0;
            while (run&&errCount<10) {
                try {
                    run = false;
					run = 0 < tools.SimilarSongs.PrecacheArtistTopTracks() || run;
					run = 0 < tools.SimilarSongs.PrecacheArtistSimilarity() || run;
					run = 0 < tools.SimilarSongs.PrecacheSongSimilarity() || run;
                } catch {
                    run = true;
                    errCount++;
                }
            }
        }
    }
}

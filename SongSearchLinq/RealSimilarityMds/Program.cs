using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using SongDataLib;
using LastFMspider;
using System.IO;
using System.Text.RegularExpressions;

namespace RealSimilarityMds
{
    class Program
    {
        static LastFmTools tools;
        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        static void Main(string[] args) {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Loading");
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);
            //var sims = SimilarTracks.LoadOrCache(tools, SimilarTracks.DataSetType.Training);
            //sims.ConvertToDistanceFormat();
            timer.TimeMark("GC");
            System.GC.Collect();
            timer.TimeMark("Loading cached distance matrix");
            CachedDistanceMatrix cachedMatrix = CachedDistanceMatrix.LoadOrCache(tools.ConfigFile.DataDirectory);
            timer.TimeMark("Saving"); 
            cachedMatrix.Save();
            timer.TimeMark("Loading strongly cached files");
            cachedMatrix.LoadDistFromAllCacheFiles();
            timer.TimeMark("Saving"); 
            cachedMatrix.Save();
            timer.Done();

        }
    }
}

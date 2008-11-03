using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions;
using SongDataLib;
using LastFMspider;
using System.IO;
using System.Text.RegularExpressions;
using hitmds;
using System.Threading;

namespace RealSimilarityMds
{
    class Program
    {
        static Regex fileNameRegex = new Regex(@"(e|t|b)(?<num>\d+)\.(dist|bin)", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        ProgressManager progress;
        public Program(ProgressManager progress) {
            this.progress = progress;
            background = new Thread(Run) {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
        }
        Thread background;
        public void RunInBackground() {
            background.Start();
        }

        LastFmTools tools;
        void Run() {
            progress.NewTask("Configuring",1.0);
            SongDatabaseConfigFile config = new SongDatabaseConfigFile(false);
            tools = new LastFmTools(config);

            progress.SetProgress(0.1);
            
            CachedDistanceMatrix cachedMatrix = LoadCachedMatrix(tools);
            Console.WriteLine("dists: {0} total, {1} non-finite", cachedMatrix.Matrix.Values.Count(), cachedMatrix.Matrix.Values.Where(f => f.IsFinite()).Count());
            

            var positionedPoints = DoMds(cachedMatrix);
            progress.NewTask("Finding Billboard hits",1.0);

            Dictionary<int,SongRef> songrefByMds= WellKnownTracksByMdsId(tools,cachedMatrix);
            

            FileInfo logFile = new FileInfo (Path.Combine (tools.ConfigFile.DataDirectory.FullName,@".\mdsdPoints.txt") ); 

            using(Stream s = logFile.Open(FileMode.Create, FileAccess.Write))
            using (TextWriter writer = new StreamWriter(s)) {
                for (int i = 0; i < positionedPoints.GetLength(0); i++) {
                    if (songrefByMds.ContainsKey(i)) {
                        writer.WriteLine("{0,10:6}, {1,10:6}:    {2}", positionedPoints[i, 0], positionedPoints[i, 1], songrefByMds[i]);
                    }
                }
            }

            progress.NewTask("Done!", 1.0);
        }

        private static Dictionary<int, SongRef> WellKnownTracksByMdsId(LastFmTools tools, CachedDistanceMatrix cachedMatrix) {
            NiceTimer timer = new NiceTimer(); 
            timer.TimeMark("Loading TrackMapper");
            TrackMapper trainingMapper = SimilarTracks.LoadOnlyTrackMapper(tools, SimilarTracks.DataSetType.Training);
            timer.TimeMark("Loading Billboard tracks");
            var songrefBySqliteId = WellKnownTracksBySqliteId(tools);

            var q =  //combines cachedMatrix.Mapping and trainingMapper and songrefBySqliteId to tuples (mdsId, songref)
                from denseID in cachedMatrix.Mapping.CurrentlyMapped
                let sqliteID = trainingMapper.LookupSqliteID(denseID)
                where songrefBySqliteId.ContainsKey(sqliteID)
                select new {
                    MdsId = cachedMatrix.Mapping.GetMap(denseID),
                    Song = songrefBySqliteId[sqliteID]
                };

            var retval = q.ToDictionary(kvp => kvp.MdsId, kvp => kvp.Song);
            timer.Done();
            return retval;
        }

        static Regex wellknown = new Regex(@"(billboard|top100)", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static IEnumerable<KeyValuePair<int, SongRef>> FindWellKnown(LastFmTools tools) {
            
            return
                from songref in (
                    from songdata in tools.DB.Songs
                    where wellknown.IsMatch(songdata.SongPath)
                    select SongRef.Create(songdata)).Distinct()
                where songref != null
                let trackID = tools.SimilarSongs.backingDB.LookupTrackID.Execute(songref)
                where trackID.HasValue
                select new KeyValuePair<int, SongRef>(trackID.Value, songref);
        }

        private static Dictionary<int, SongRef> WellKnownTracksBySqliteId(LastFmTools tools) {
            return FindWellKnown(tools).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }


        private CachedDistanceMatrix LoadCachedMatrix(LastFmTools tools) {
            NiceTimer timer = new NiceTimer();
            timer.TimeMark("Loading cached distance matrix");
            CachedDistanceMatrix cachedMatrix = CachedDistanceMatrix.LoadOrCache(tools.ConfigFile.DataDirectory);
            progress.SetProgress(0.2);
            timer.TimeMark("Saving matrix");
            cachedMatrix.Save();
            progress.SetProgress(0.3);
            timer.TimeMark("Loading strongly cached files into matrix");
            cachedMatrix.LoadDistFromAllCacheFiles();
            progress.SetProgress(0.9);
            timer.TimeMark("Saving matrix");
            cachedMatrix.Save();
            timer.Done();
            progress.SetProgress(1.0);
            return cachedMatrix;
        }

        private double[,] DoMds(CachedDistanceMatrix cachedMatrix) {
            Random r = new Random();
            int dimensions=5;

            int totalRounds = cachedMatrix.Mapping.Count * 50;
            
            progress.NewTask("MDS",1.0);

            float maxDist = cachedMatrix.Matrix.Values.Where(dist => dist.IsFinite()).Max();

            using (Hitmds mdsImpl = new Hitmds(cachedMatrix.Mapping.Count, dimensions, (i, j) => cachedMatrix.Matrix[i, j].IsFinite()?cachedMatrix.Matrix[i, j]:maxDist*10, r)) {
                mdsImpl.mds_train(totalRounds, 5.0, 0.0, (cycle, ofTotal, src) => { progress.SetProgress(cycle/(double)ofTotal); });

                double[,] retval = new double[cachedMatrix.Mapping.Count, dimensions];
                for (int mdsId = 0; mdsId < cachedMatrix.Mapping.Count; mdsId++)
                    for (int dim = 0; dim < dimensions; dim++)
                        retval[mdsId, dim] = mdsImpl.GetPoint(mdsId, dim);
                progress.SetProgress(1.0);
                return retval;
            }
        }
    }
}

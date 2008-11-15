using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using System.Collections.ObjectModel;
using System.Windows;
using hitmds;
using System.IO;
using EmnExtensions;
using System.Text.RegularExpressions;
using SongDataLib;
namespace SimilarityMdsLib
{
    public class MdsEngine
    {
        public struct FormatAndOptions
        {
            public SimilarityFormat Format;
            public Options Options;

            public override string ToString() {                return Format.ToString() + Options.ToString();             }

            static Regex filenameRegex = new Regex(@"^mds-(?<format>.*)N(?<N>\d+)LR(?<LR>\d+)SA(?<SA>\d+)PU(?<PU>\d)D(?<D>\d+)(\.bin|-(corr|test)\.graph)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
            public static FormatAndOptions? FindOptionsFromFileName(string filename) {
                var match = filenameRegex.Match(filename);

                return !match.Success
                    ? (FormatAndOptions?)null
                    : new FormatAndOptions {
                        Format = (SimilarityFormat)Enum.Parse(typeof(SimilarityFormat), match.Groups["format"].Value),
                        Options = new Options {
                            Dimensions = int.Parse(match.Groups["D"].Value),
                            NGenerations = int.Parse(match.Groups["N"].Value),
                            LearnRate = int.Parse(match.Groups["LR"].Value) / 1000.0,
                            PointUpdateStyle = int.Parse(match.Groups["PU"].Value),
                            StartAnnealingWhen = int.Parse(match.Groups["SA"].Value) / 1000.0,
                        },
                    };
            }

            public static IEnumerable<FormatAndOptions> AvailableInCache(SongDatabaseConfigFile configFile) {
                DirectoryInfo resDir = configFile.DataDirectory.CreateSubdirectory("res");
                return
                resDir.GetFiles("*.bin")
                    .Select(file => FindOptionsFromFileName(file.Name))
                    .Where(opts => opts.HasValue)
                    .Select(opts => opts.Value);
            }
        }
        public struct Options
        {
            public int NGenerations;
            public double LearnRate;
            public double StartAnnealingWhen;
            public int PointUpdateStyle;//0(orig) 1(emn*0.5) or 2(emn dynamically scaled)
            public int Dimensions;
            public override string ToString() {
                return "N" + NGenerations + "LR" + ((int)(LearnRate * 1000)) + "SA" + ((int)(StartAnnealingWhen * 1000)) + "PU" + PointUpdateStyle + "D" + Dimensions;
            }
        }
        public readonly Options Opts;
        bool maySave = false;
        public IProgressManager progress { get; set; }
        SimCacheManager settings;
        TestDataInTraining evaluator;
        CachedDistanceMatrix cachedMatrix;

        public MdsEngine(SimCacheManager settings, TestDataInTraining evaluator, CachedDistanceMatrix cachedMatrix, Options Opts) {
            progress = new NullProgressManager();
            this.settings = settings;
            this.evaluator = evaluator;
            this.Opts = Opts;
            this.cachedMatrix = cachedMatrix;
        }

        public bool ResultsAlreadyCached { get { return testFile.Exists; } }




        public string resultsFilename { get { return new FormatAndOptions { Format = settings.Format, Options = Opts }.ToString(); } }
        public FileInfo mdsFile { get { return new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + ".bin")); } }
        public FileInfo corrFile { get { return new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + "-corr.graph")); } }
        public FileInfo testFile { get { return new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + "-test.graph")); } }

        public void SaveMds() {
            if (!maySave)
                throw new Exception("No calculations to save!");
            double[,] positionedPoints = MdsResults;
            progress.NewTask("Saving MDS");
            using (Stream s = mdsFile.Open(FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(s)) {
                writer.Write((int)positionedPoints.GetLength(0));//songCount
                writer.Write((int)positionedPoints.GetLength(1));//dimCount

                for (int i = 0; i < positionedPoints.GetLength(0); i++) { //mdsSongIndex
                    for (int dim = 0; dim < positionedPoints.GetLength(1); dim++) {
                        writer.Write((double)positionedPoints[i, dim]);
                    }
                    progress.SetProgress((i + 1.0) / positionedPoints.GetLength(0));
                }
            }
            using (Stream s = corrFile.Open(FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(s)) {
                writer.Write((int)Correlations.Count);
                foreach (Point p in Correlations) {
                    writer.Write((double)p.X);
                    writer.Write((double)p.Y);
                }
            }
            using (Stream s = testFile.Open(FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(s)) {
                writer.Write((int)TestSetRankings.Count);
                foreach (Point p in TestSetRankings) {
                    writer.Write((double)p.X);
                    writer.Write((double)p.Y);
                }
            }

            progress.Done();
        }

        public void LoadCachedMds() {
            double[,] positionedPoints;
            progress.NewTask("Loading MDS");
            using (Stream s = mdsFile.OpenRead())
            using (BinaryReader reader = new BinaryReader(s)) {
                int songCount = reader.ReadInt32();
                int dimCount = reader.ReadInt32();
                positionedPoints = new double[songCount, dimCount];
                for (int i = 0; i < positionedPoints.GetLength(0); i++) { //mdsSongIndex
                    for (int dim = 0; dim < positionedPoints.GetLength(1); dim++) {
                        positionedPoints[i, dim] = reader.ReadDouble();
                    }
                    progress.SetProgress((i + 1.0) / positionedPoints.GetLength(0));
                }
            }
            MdsResults = positionedPoints;
            using (Stream s = corrFile.OpenRead())
            using (BinaryReader reader = new BinaryReader(s)) {
                int numCorrelations = reader.ReadInt32();
                Correlations.Clear();
                foreach (int n in Enumerable.Range(0, numCorrelations)) {
                    Correlations.Add(new Point(reader.ReadDouble(), reader.ReadDouble()));
                }
            }
            using (Stream s = testFile.OpenRead())
            using (BinaryReader reader = new BinaryReader(s)) {
                int numTestSetRanking = reader.ReadInt32();
                TestSetRankings.Clear();
                foreach (int n in Enumerable.Range(0, numTestSetRanking)) {
                    TestSetRankings.Add(new Point(reader.ReadDouble(), reader.ReadDouble()));
                }
            }

            progress.Done();
        }

        public double LoadOnlyFinalCorrelation() {
            using (Stream s = corrFile.OpenRead())
            using (BinaryReader reader = new BinaryReader(s)) {
                int numCorrelations = reader.ReadInt32();
                int offset = 4 + 8 * (2 * numCorrelations - 1);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadDouble();
            }
        }
        public double LoadOnlyFinalTestSetRanking() {
            using (Stream s = testFile.OpenRead())
            using (BinaryReader reader = new BinaryReader(s)) {
                int numTestSetRanking = reader.ReadInt32();
                int offset = 4 + 8 * (2 * numTestSetRanking - 1);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                return reader.ReadDouble();
            }
        }

        public readonly ObservableCollection<Point> Correlations = new ObservableCollection<Point>();
        public readonly ObservableCollection<Point> TestSetRankings = new ObservableCollection<Point>();
        public double[,] MdsResults { get; private set; }
        int nextGraphUpdateOn = 1;
        int testCheckIter = 0;
        const int graphRes = 1000;
        const int testCheckMult = 10;
        void DoProgress(int cycle, int ofTotal, Hitmds src) {
            double progressRatio = cycle / (double)ofTotal;
            if ((int)(progressRatio * graphRes) >= nextGraphUpdateOn) {
                Correlations.Add(new Point(progressRatio, CalcCorr(src)));
                nextGraphUpdateOn++;
                if (testCheckIter == 0) {
                    TestSetRankings.Add(new Point(progressRatio, CalcRanking(src)));
                }
                testCheckIter = (testCheckIter + 1) % testCheckMult;
            }
            progress.SetProgress(progressRatio);
        }

        static double CalcCorr(Hitmds mdsImpl) {
            return 1 / Math.Sqrt(mdsImpl.corr_2() + 1);
        }

        double CalcRanking(Hitmds mdsImpl) {
            return 1 - evaluator.AverageRanking(mdsImpl.DistsTo);
        }

        private static double[,] ExtractDims(Hitmds mdsImpl, int dimensions, int mdsIdCount) {
            double[,] retval = new double[mdsIdCount, dimensions];
            for (int mdsId = 0; mdsId < mdsIdCount; mdsId++)
                for (int dim = 0; dim < dimensions; dim++)
                    retval[mdsId, dim] = mdsImpl.GetPoint(mdsId, dim);
            return retval;
        }
        public void DoMds(IProgressManager progress) {
            this.progress = progress;
            DoMds();
        }
        public void DoMds() {
            if (evaluator == null || cachedMatrix == null)
                throw new ArgumentNullException();
            Random r = new Random();

            int totalRounds = cachedMatrix.Mapping.Count * Opts.NGenerations;

            progress.NewTask("MDS-init");

            float maxDist = cachedMatrix.Matrix.Values.Where(dist => dist.IsFinite()).Max();

            progress.NewTask("MDS");
            using (Hitmds mdsImpl = new Hitmds(Opts.Dimensions, cachedMatrix.Matrix, r)) { //(i, j) => cachedMatrix.Matrix[i, j].IsFinite() ? cachedMatrix.Matrix[i, j] : maxDist * 10
                nextGraphUpdateOn = 1;
                mdsImpl.mds_train(totalRounds, Opts.LearnRate, Opts.StartAnnealingWhen, DoProgress, Opts.PointUpdateStyle);
                Correlations.Add(new Point(1.0, CalcCorr(mdsImpl)));
                TestSetRankings.Add(new Point(1.0, CalcRanking(mdsImpl)));

                MdsResults = mdsImpl.PointPositions();
                progress.SetProgress(1.0);
            }

            maySave = true;
        }
    }
}

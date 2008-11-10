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
namespace SimilarityMdsLib
{
    public class MdsEngine
    {
        public struct Options
        {
            public int NGenerations;
            public double LearnRate;
            public double StartAnnealingWhen;
            public int PointUpdateStyle;//0(orig) 1(emn*0.5) or 2(emn dynamically scaled)
            public int Dimensions;
            public override string ToString() {
                return "N" + NGenerations + "LR" + ((int)LearnRate * 1000) + "SA" + ((int)StartAnnealingWhen * 1000) + "PU" + PointUpdateStyle + "D" + Dimensions;
            }
        }
        public readonly Options Opts;

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

        public void SaveMds() {

            double[,] positionedPoints = MdsResults;
            progress.NewTask("Saving MDS");
            string resultsFilename = settings.Format.ToString() + Opts.ToString();
            FileInfo mdsFile = new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + ".bin"));
            FileInfo corrFile = new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + "-corr.graph"));
            FileInfo testFile = new FileInfo(Path.Combine(settings.DataDirectory.FullName, @".\res\mds-" + resultsFilename + "-test.graph"));
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
                writer.Write((int) TestSetRankings.Count);
                foreach (Point p in TestSetRankings) {
                    writer.Write((double)p.X);
                    writer.Write((double)p.Y);
                }
            }

            progress.Done();
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

        public void DoMds() {
            Random r = new Random();

            int totalRounds = cachedMatrix.Mapping.Count * Opts.NGenerations;

            progress.NewTask("MDS-init");

            float maxDist = cachedMatrix.Matrix.Values.Where(dist => dist.IsFinite()).Max();

            progress.NewTask("MDS");
            using (Hitmds mdsImpl = new Hitmds(cachedMatrix.Mapping.Count, Opts.Dimensions, cachedMatrix.Matrix, r)) { //(i, j) => cachedMatrix.Matrix[i, j].IsFinite() ? cachedMatrix.Matrix[i, j] : maxDist * 10
                nextGraphUpdateOn = 1;
                mdsImpl.mds_train(totalRounds, Opts.LearnRate, Opts.StartAnnealingWhen, DoProgress, Opts.PointUpdateStyle);
                Correlations.Add(new Point(1.0, CalcCorr(mdsImpl)));
                TestSetRankings.Add(new Point(1.0, CalcRanking(mdsImpl)));

                MdsResults = mdsImpl.PointPositions();
                progress.SetProgress(1.0);
            }
        }
    }
}

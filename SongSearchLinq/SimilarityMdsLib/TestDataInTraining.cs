using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LastFMspider;
using LastFMspider.LastFMSQLiteBackend;

namespace SimilarityMdsLib
{
    public class TestDataInTraining
    {
        static IEnumerable<SimilarTrackRow> TranslateTo(SimCacheManager settingsTrainingSet, ArbitraryTrackMapper mapTrainToMds,DataSetType testSet) {
            TrackMapper mapSqliteToTrain = settingsTrainingSet.LoadTrackMapper();
            
            var IdsTrainingSqlite = mapTrainToMds
                .CurrentlyMapped
                .Select(trainId => mapSqliteToTrain.LookupSqliteID(trainId));

            var settingsTestSet=settingsTrainingSet.WithDataSetType(testSet);//might actually be verification set.
            SimilarTracks similarTrackTestSet = settingsTestSet.LoadSimilarTracks();

            TrackMapper mapSqliteToTest = similarTrackTestSet.TrackMapper;
            var sqliteSimilaritiesTestSet = similarTrackTestSet.SimilaritiesSqlite;
            similarTrackTestSet = null;
            mapSqliteToTest.BuildReverseMapping();

            int[] IdsTestAndTraining = IdsTrainingSqlite
                .Where(sqliteId => mapSqliteToTest.LookupDenseID(sqliteId)>=0)
                .ToArray(); //execute compound query
            Array.Sort(IdsTestAndTraining);
            mapSqliteToTest = null;


            return 
                from sqliteTestSimilarity in sqliteSimilaritiesTestSet
                where
                 Array.BinarySearch(IdsTestAndTraining, sqliteTestSimilarity.TrackA) >= 0
                 && Array.BinarySearch(IdsTestAndTraining, sqliteTestSimilarity.TrackB) >= 0
                select new SimilarTrackRow {
                    Rating = sqliteTestSimilarity.Rating,
                    TrackA = mapTrainToMds.GetMap(mapSqliteToTrain.FindDenseID(sqliteTestSimilarity.TrackA)),
                    TrackB = mapTrainToMds.GetMap(mapSqliteToTrain.FindDenseID(sqliteTestSimilarity.TrackB)),
                };
        }

        int[] mdsIdInTestPairs;
        int[][] mdsIdIsCloseInTestTo;
        public TestDataInTraining(SimCacheManager settings, CachedDistanceMatrix cachedMatrix) {
            var testPairsWithMdsId = TestDataInTraining.TranslateTo(settings, cachedMatrix.Mapping, DataSetType.Test).ToArray();
            mdsIdInTestPairs = (
                from similarity in testPairsWithMdsId
                from track in new[] { similarity.TrackA, similarity.TrackB }
                select track
                ).Distinct().OrderBy(track => track).ToArray();
            List<int>[] sims = new List<int>[cachedMatrix.Mapping.Count];
            foreach (int interestingId in mdsIdInTestPairs)
                sims[interestingId] = new List<int>();
            foreach (var sim in testPairsWithMdsId) {
                sims[sim.TrackA].Add(sim.TrackB);
                sims[sim.TrackB].Add(sim.TrackA);
            }
            mdsIdIsCloseInTestTo = sims.Select(closeTo => closeTo == null ? null : closeTo.ToArray()).ToArray();
        }

        public double AverageRanking(Func<int, double[]> distanceToAllInMds) {
            int[] mdsIds = new int[mdsIdIsCloseInTestTo.Length];
            int[] mdsRanks = new int[mdsIdIsCloseInTestTo.Length];
            double sum = 0.0;//the sum of the rankings in (0..1) of all items in mdsIdInTestPairs
            foreach (int inTest in mdsIdInTestPairs) {
                double[] distanceFrom = distanceToAllInMds(inTest);
                int[] shouldBeClose = mdsIdIsCloseInTestTo[inTest];
                for (int i = 0; i < mdsIds.Length; i++) mdsIds[i] = i;

                Array.Sort(distanceFrom, mdsIds);
                for (int i = 0; i < mdsIds.Length; i++) mdsRanks[mdsIds[i]] = i;//i.e. first rank is assigned to the item first in the distance measure

                double sumRankCloseToInTest = 0.0;
                foreach (int closeId in shouldBeClose) {
                    sumRankCloseToInTest += mdsRanks[closeId] / (double)mdsIds.Length;
                }

                sum += sumRankCloseToInTest / shouldBeClose.Length;
            }

            return sum / mdsIdInTestPairs.Length;
        }
    }
}

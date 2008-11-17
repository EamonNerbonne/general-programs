using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimilarityMdsLib;
using LastFMspider;
using SongDataLib;

namespace LastFmMdsDisplay
{
    public class EmbeddingManager
    {
        //AvgRank2N140LR2000SA0PU1D2

        public MdsResults Mds { get; private set; }
        public LastFmTools Tools { get; private set; }

        public EmbeddingManager() {
            Tools = new LastFmTools(new SongDatabaseConfigFile(false));
            //AvgRank2N140LR2000SA0PU1D2
            Mds = MdsResults.LoadResults(new MdsEngine.FormatAndOptions {
                Format = SimilarityFormat.AvgRank2,
                Options = new MdsEngine.Options {
                    NGenerations = 140,
                    LearnRate = 2.0,
                    StartAnnealingWhen = 0.0,
                    PointUpdateStyle = 1,
                    Dimensions = 2,
                }
            }, new SimCacheManager(SimilarityFormat.LastFmRating, Tools, DataSetType.Training));
        }

    }
}

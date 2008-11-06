using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LastFMspider
{
    public enum SimilarityFormat
    {
        LastFmRating,
        Log200,
        Log2000,
        AvgRank
    };
    public static class SimilarityFormatConv
    {
        public static string ToPathString(SimilarityFormat format) {
            switch (format) {
                case SimilarityFormat.AvgRank: return "1";
                case SimilarityFormat.Log200: return "200";
                case SimilarityFormat.Log2000: return "2000";
                default: throw new Exception(format.ToString() + " is not cached.");
            }
        }

    }
}

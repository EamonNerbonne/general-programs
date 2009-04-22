using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace EmnExtensions.Algorithms
{
    public class Histogrammer
    {
        public struct Data
        {
            public double point;
            public double density;
        }

        double[] sortedData;
        int infCount;
        public double MinVal { get { return sortedData[0]; } }
        public double MaxVal { get { return sortedData[sortedData.Length - 1]; } }
        double? maximumDensity ;
        int minBucketSize, maxResolution;
        public double MaximumDensity { get { if (!maximumDensity.HasValue) GenerateHistogram().Count(); return maximumDensity.Value; } }
        
        public Histogrammer(IEnumerable<double> values, int minBucketSize,int maxResolution) {
            this.maxResolution = maxResolution;
            this.minBucketSize = minBucketSize;
            sortedData = values.Where(f => f.IsFinite() || infCount++<0 ).ToArray();
            Array.Sort(sortedData);

            int numBuckets = sortedData.Length - minBucketSize;
            if (minBucketSize < 1 || numBuckets < 1) {// no output point!
                throw new ArgumentException("Too few points: bucket size: " + minBucketSize + "  bucket number:" + numBuckets);
            }
        }
        public IEnumerable<Data> GenerateHistogram() {
            return GenerateHistogram(0.0, 1.0);
        }

        public IEnumerable<Data> GenerateHistogram(double startPercentile,double endPercentile) {


            double minBucketWidth = (MaxVal - MinVal) / maxResolution;

            double curSum = 0;
            int startIndex = (int)(startPercentile*sortedData.Length+0.5);
            int endIndex = startIndex;
            int untilIndex = (int)(endPercentile * sortedData.Length + 0.5);
            double maxDensity = 0.0;
            while (endIndex < untilIndex) {
                if (endIndex - startIndex < minBucketSize || sortedData[endIndex] - sortedData[startIndex] < minBucketWidth) { //make sure we satisfy minimum bucket size.
                    curSum += sortedData[endIndex]; endIndex++;
                } else {//ok my window is at least as big as necessary! 
                    double density = (endIndex - startIndex) / (sortedData[endIndex] - sortedData[startIndex]);
                    if (density > maxDensity) maxDensity = density;
                    yield return new Data {
                        point = curSum / (endIndex - startIndex),
                        density = density
                    };

                    startIndex = endIndex;
                    curSum = 0;
                    //do {//old blurred histo code
                    //    curSum -= sortedData[startIndex]; startIndex++;
                    //} while (!(endIndex - startIndex < bucketSize || sortedData[endIndex] - sortedData[startIndex] < minBucketWidth));
                    //we only stop shrinking once we're sure it'll need growing again.
                }
            }
            maximumDensity = maxDensity;
        }

    }
}
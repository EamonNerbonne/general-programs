using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Algorithms
{
    struct BasicStatistics
    {
        public double mean { get { return (sum / count); } }
        public double samplevariance { get { return (sumOfSqrs - mean * mean * count) / (count-1); } }
        public double sum;
        public int count;
        public double sumOfSqrs;
        public void AddValue(double val) {
            count++;
            sumOfSqrs += val*val;
            sum += val;
        }
        public static BasicStatistics Calculate(IEnumerable<double> data) {
            BasicStatistics stats = new BasicStatistics();
            foreach (double f in data) 
                stats.AddValue(f);
            return stats;
        }

        
    }
}

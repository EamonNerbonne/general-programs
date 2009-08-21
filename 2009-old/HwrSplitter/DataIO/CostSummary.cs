using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataIO
{
    public struct CostSummary
    {
        public double
            lengthErr, posErr, spaceErr,
            spaceDarkness, wordLightness;
        public double Total { get { return lengthErr + posErr + spaceErr + spaceDarkness + wordLightness; } }
        public static CostSummary operator +(CostSummary a, CostSummary b) {
            return new CostSummary {
                lengthErr = a.lengthErr + b.lengthErr,
                posErr = a.posErr + b.posErr,
                spaceErr = a.spaceErr + b.spaceErr,
                spaceDarkness = a.spaceDarkness + b.spaceDarkness,
                wordLightness = a.wordLightness + b.wordLightness
            };
        }
        public static CostSummary operator -(CostSummary a, CostSummary b) {
            return new CostSummary {
                lengthErr = a.lengthErr - b.lengthErr,
                posErr = a.posErr - b.posErr,
                spaceErr = a.spaceErr - b.spaceErr,
                spaceDarkness = a.spaceDarkness - b.spaceDarkness,
                wordLightness = a.wordLightness - b.wordLightness
            };
        }

        public override string ToString() {
            return string.Format("spaceE: {2:f2}, spaceDark: {3:f2}, wordLight: {4:f2}\nlenE: {0:f2}, posE: {1:f2}, TOT: {5:f2}",
                lengthErr, posErr, spaceErr,
            spaceDarkness, wordLightness, Total
                );
        }
    }
}

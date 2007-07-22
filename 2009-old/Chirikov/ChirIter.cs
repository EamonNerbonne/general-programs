using System;
using System.Collections.Generic;
using System.Text;

namespace Chirikov {
    public struct ChirPoint {
        public double p, x;
        public ChirPoint(double p, double x) {
            this.p = p;
            this.x = x;
        }
    }
    public class ChirIter {
        public static IEnumerable<ChirPoint> Generate(double k, ChirPoint px) {
            while (true) {
                ChirPoint tmp;
                tmp.p = px.p + k / (2 * Math.PI) * Math.Sin(2 * Math.PI * px.x);
                tmp.x = px.x + tmp.p;
                //if (tmp.p - (int)tmp.p < 0 || tmp.x - (int)tmp.x < 0)
                    //throw new Exception("uhhh");
                px.p = tmp.p - Math.Floor(tmp.p);
                px.x = tmp.x - Math.Floor(tmp.x);
                yield return px;
            }
        }
    }
}

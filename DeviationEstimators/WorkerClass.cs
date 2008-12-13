using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.MathHelpers;
using EmnExtensions;

namespace DeviationEstimators
{
    public class WorkerClass
    {
        public void Run() {
            Console.WriteLine("Making random numbers...");

            double nextNum = double.NaN;
            DateTime nextUpdate = DateTime.Now + TimeSpan.FromSeconds(10.0);
            DateTime firstState = DateTime.Now;
            Random r = new Random();
            ulong done = 0;
            while (true) {
                nextNum = r.NextDouble();//RndHelper.MakeSecureSingle();
                done++;
                if (!nextNum.IsFinite() || nextNum >= 1.0 || nextNum<=0) {
                    Console.WriteLine("Got value {0} after {1} iterations.", nextNum, done);
                    nextUpdate = DateTime.Now + TimeSpan.FromSeconds(10.0);
                } else if ((((uint)done) & 32767u) == 32767u) {
                    if (nextUpdate < DateTime.Now) {
                        Console.WriteLine("Did {0} iterations, at {1}/sec.",  done,(double)done/(DateTime.Now-firstState).TotalSeconds );
                        nextUpdate = nextUpdate + TimeSpan.FromSeconds(10.0);
                    }
                }

            }
            
        }
    }
}

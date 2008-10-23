using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using EamonExtensionsLinq.Algorithms;
namespace DnDstatCalc {
    public struct Prob<T> {
        public T Val;
        public double P;
        public Prob(T Val,double P) {this.Val=Val;this.P=P;}
    }

    static class Program {
        const    int takenum = 6;
        const    int trynum = 9;
        static IEnumerable<Prob<T>> Combine<T>(IEnumerable<IEnumerable<Prob<T>>> dists,Func<T,T,T> comb) {
            return dists.Aggregate((seta,setb) => (from a in seta from b in setb select new Prob<T>(comb(a.Val,b.Val),a.P*b.P)));
        }

        static IEnumerable< Prob<T> > Normalize<T>(IEnumerable<Prob<T>> distribution ) {
            var tot = (from p in distribution select p.P).Sum();
            return distribution.Select(p => new Prob<T>(p.Val, p.P/tot));
        }

        static int StatMod(int stat) { return (stat - 10) / 2; }
        static int StatVal(int stat) {
            int off = stat - 10;
            int off3 = off * off * off;
            return off + off3 / 60;
            /* 9 -1
             * 10 0  0      0
             * 11 1  0      1
             * 12 2  0      8
             * 13 3  0      27
             * 14 5  1 0    64
             * 15 7  2 0    125
             * 16 9  3 0    216
             * 17 12 5 1    343
             * 18 16 8 3    512
            */
        }
        static void Main(string[] args) {
            var d6 = Enumerable.Range(1,6);
            var dieTuple = from a in d6
                           from b in d6
                           from c in d6
                           from d in d6
                           select new int[]{a,b,c,d};
            var histogram = from t in dieTuple
                            let val = t.Sum() - t.Min()
                            group 0 by (int)val into valGroup
                            orderby valGroup.Key
                            select new Prob<int>(valGroup.Key, valGroup.Count());
            histogram = Normalize(histogram).ToArray();
            var sum = 0.0;
            var cumHist = histogram.Select(pr => new Prob<int>(pr.Val, (sum += pr.P))).ToArray();
            Random r = new Random();
            Func<int> rollStat = delegate() {
                double roll = r.NextDouble();
                return cumHist.First(pr => pr.P >= roll).Val;
            };

            double sumcosts=0.0;
            int count = 0;
			int reach15 = 0;
			int reach14h = 0;
			int cost22 = 0;
			int cost22plus = 0;
            foreach (var x in Enumerable.Range(0, 10000000)) {
				var stats = new[] { rollStat(), rollStat(), rollStat(), rollStat(), rollStat(), rollStat() }.OrderBy(i=>i).ToArray();
                stats[0] = rollStat();
				//ok, we rerolled the lowest.
				//now find weighted average stat value:
				stats.Shuffle();
				var indexed= stats.Select((stat, idx) => new { Index = idx, Val = stat });
				var groupedStats = (from idxStat in indexed
				 group idxStat by idxStat.Index/2 into defenseGroup
				 let betterStat = defenseGroup.Select(s=>s.Val).Max()
                 let worseStat = defenseGroup.Select(s=>s.Val).Min()
				 select new {Good=betterStat,Bad=worseStat}).ToArray();
				var attackStat = groupedStats.First().Good;
				var defStatSum = groupedStats.Select(s => s.Good).Sum();
				var secondaryStat = stats.OrderBy(s => s).Skip(1).First();
				var offStatSum = groupedStats.Select(s => s.Bad).Sum();
				var weightedAvg = (3 * attackStat + defStatSum + secondaryStat + offStatSum / 3.0) / 8.0; //could be (3*18 + (18+14+11)+14+(10+10+8)/3.0)/8.0
				//so pointbuy allows 361/24== 15+1/24),though def. is 16,14,13,12,11,10
				//def is: 4*16+2*14 + 13 + 11

                var totalmod = stats.Select(stat => StatMod(stat)).Sum();
                var totalcost = stats.Select(stat => StatVal(stat)).Sum()+2;
                //if(totalcost>22)Console.WriteLine(string.Join(", ", stats.Select(i => i.ToString()).ToArray()) + ":  " + totalmod + "   ("+totalcost+")");
                if (totalmod < 4 || totalmod > 8) continue;

				if (weightedAvg > 15.04166) reach15++;
				if (weightedAvg >= 14.5) reach14h++;
				if (totalcost > 22) cost22plus++;
				if (totalcost >= 22) cost22++;
				sumcosts += totalcost;
                count++;
            }
            Console.WriteLine("Avg:" + sumcosts / count);
			Console.WriteLine("ReachDefaultArray: " + reach14h*100.0 / count + "%");
			Console.WriteLine("ReachOptimum: " + reach15 * 100.0 / count + "%");
			Console.WriteLine("ReachPointBuy: " + cost22 * 100.0 / count + "%");
			Console.WriteLine("ExceedPointBuy: " + cost22plus * 100.0 / count + "%");

			//Console.ReadLine();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;

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
        static void Main(string[] args) {
            var d6 = Sequence.Range(1,6);
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
            var avg = histogram.Select(p => p.P*p.Val).Sum();
            
            
            var roll3d6 = from a in d6
                          from b in d6 
                          from c in d6
                          group 0 by a+b+c into result
                          orderby result.Key
                          select new Prob<int>(result.Key, result.Count());
            var rolled3d6 = Normalize(roll3d6).ToArray();
            /*
            var test = (from t1 in rolled3d6
                       from t2 in rolled3d6
                       from t3 in rolled3d6
                       from t4 in rolled3d6
                       from t5 in rolled3d6
//                       from t6 in rolled3d6
                       let chance = t1.P*t2.P*t3.P*t4.P*t5.P//*t6.P
                       let nums = (new int[]{t1.Val,t2.Val,t3.Val,t4.Val,t5.Val}).OrderBy(i=>i).ToSequence().Skip(2).Sum()
                       let WA = nums/3.0*chance
                       select WA).Sum();

                        /*/
            double test=0.0;
            double err=0.0;
            foreach(var p in 
                       from t1 in rolled3d6
                       from t2 in rolled3d6
                       from t3 in rolled3d6
                       from t4 in rolled3d6
                       from t5 in rolled3d6
                       from t6 in rolled3d6
                       from t7 in rolled3d6
                       from t8 in rolled3d6
                       from t9 in rolled3d6
//                       from t6 in rolled3d6
                       select new Prob<int[]>(new int[]{t1.Val,t2.Val,t3.Val,t4.Val,t5.Val,t6.Val,t7.Val,t8.Val,t9.Val},t1.P*t2.P*t3.P*t4.P*t5.P*t6.P*t7.P*t8.P*t9.P)){
                int[] arr = p.Val;
                Array.Sort<int>(arr);
                double oldtest=test;
                double pr = p.P * ( arr[3]+arr[4]+arr[5]+arr[6]+arr[7]+arr[8]);
                test += pr;
                oldtest-=test;
                err+= Math.Abs(oldtest+pr);

            }
            test/=6.0;/**/
            //estimated running time: 2 days.

            
            /*var rolledNx3d6 = Sequence.Repeat(rolled3d6,trynum).Aggregate((new[] {new Prob<IEnumerable<int>>(new int[]{},1.0)}).ToSequence() ,
                (histThrowSeq,histThrow) => (from throwSeqProb in histThrowSeq from aThrowProb in histThrow select new Prob<IEnumerable<int>>(throwSeqProb.Val.Concat(Sequence.Repeat(aThrowProb.Val,1)),throwSeqProb.P*aThrowProb.P)));
            var nextRes = rolledNx3d6.Select(aSeqProb =>new Prob<double>( aSeqProb.Val.OrderBy(i=>i).Skip(trynum-takenum).Aggregate(0.0,(d,n) => d+n/(double)takenum),aSeqProb.P ))
                .Aggregate(0.0,(tot,a) => tot+a.Val*a.P);*/
            //Combine<IEnumerable<int>>(,(a,b) => a.Concat<int>(b));
            


            foreach(var res in histogram) {
                Console.WriteLine("Roll {0} percentage: {1}",res.Val,res.P);
            }
            Console.WriteLine("In other words, the average is " + avg + ".");
            //Console.WriteLine("Best "+takenum+" of "+trynum+" of 3d6: "+nextRes);
            Console.WriteLine("Best "+takenum+" of "+trynum+" of 3d6: "+test);
            Console.WriteLine(err);

            Console.ReadLine();
        }
    }
}

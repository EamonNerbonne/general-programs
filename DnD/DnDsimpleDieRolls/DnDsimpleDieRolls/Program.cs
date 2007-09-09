using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;

namespace DnDsimpleDieRolls {
    class Histogram<T> {
        int total=0;
        Dictionary<T,int> dict = new Dictionary<T,int>();
        public void Add(T val) {
            dict[val] = Frequency(val) + 1;
            total++;
        }
        public IEnumerable<T> Values { get{ return dict.Keys;}}
        public int Frequency(T val) { return (dict.ContainsKey(val)?dict[val]:0);}
        public double Chance(T val) { return (double)Frequency(val) / (double)total;}
        public Histogram() {}
        public Histogram(IEnumerable<T> coll) {foreach(var a in coll) Add(a);}
    }
    class Program {
       static void Main(string[] args) {
           var d6 = Sequence.Range(1,6);
           var d4 = Sequence.Range(1,4);
           var dieTuple = Sequence.Repeat(d6,7).Aggregate((IEnumerable<IEnumerable<int>>)new int[][]{new int[]{}},
               (listsRes,listPoss) => (from poss in listPoss from res in listsRes select  res.Concat(Sequence.Repeat(poss,1))));
               
           var rolls = from roll in dieTuple
                       select (
                            from val in roll
                            orderby val select val).Take(3).Sum() - 3;
           var hist = new Histogram<int>(rolls);
           foreach(var val in hist.Values) {
               Console.WriteLine("Value {0} has probability {1}.",val,hist.Chance(val));
           }
           Console.WriteLine("Weighed Average: {0}",(from val in hist.Values select val * hist.Chance(val)).Sum());
           Console.ReadLine();
        }
    }
}

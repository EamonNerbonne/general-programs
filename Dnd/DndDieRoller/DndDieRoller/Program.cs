using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DndDieRoller
{
    class Program
    {
        static void Main(string[] args)
        {
            var hmm = from a in Enumerable.Range(1, 20)
                    from b in Enumerable.Range(1, 20)
                    let win= a - 3 > b
                    group win by win into g
                    select new {stat=g.Key, count=g.Count()};
            var lookup = hmm.ToDictionary(a => a.stat, a => a.count);

            
            Console.WriteLine("dead body wins "+lookup[true] +" times.");
            Console.WriteLine("dead body loses "+lookup[false] +" times.");

        }
    }
}

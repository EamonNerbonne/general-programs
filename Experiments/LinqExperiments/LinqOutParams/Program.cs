using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqOutParams
{
    class Program
    {
        static void Main(string[] args) {
            int tmp=-1;
            foreach (int i in
                from num in Enumerable.Range(0, 100)
                let str = num.ToString() + (num == 57 ? "x" : "")
                let isInt = int.TryParse(str, out tmp)
                where isInt
                select tmp)
                Console.WriteLine(i);
        }
    }
}

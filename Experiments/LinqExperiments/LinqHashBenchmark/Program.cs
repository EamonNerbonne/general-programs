using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ConsoleApplication1 {
    class Program {
        static void Main(string[] args) {
            DateTime dt = DateTime.Now;

            var dict = Enumerable.Range(1, 500000).ToDictionary(num=>Convert.ToString(num, 16));
            var c = (from i in Enumerable.Range(1, 500000) where dict.ContainsKey(i.ToString()) select i).Count();

	        Console.WriteLine(c);
            Console.WriteLine(DateTime.Now - dt);
            Console.ReadKey();

        }
    }
}



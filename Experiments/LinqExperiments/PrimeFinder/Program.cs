using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;

namespace PrimeFinder {
    class Program {
        static void Main(string[] args) {

            foreach (var i in 
                
                (from num in Enumerable.Range(2,100)
                 where Enumerable.Range(2,(int)Math.Sqrt(num)-1).All(num2 => num % num2 != 0) 
                 select num)
                 
                 ) Console.WriteLine(i);
        }
    }
}

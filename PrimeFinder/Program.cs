using System;
using System.Collections.Generic;
using System.Text;
using System.Query;
using System.Xml.XLinq;
using System.Data.DLinq;

namespace LINQConsoleApplication1 {
    class Program {
        static void Main(string[] args) {

            foreach (var i in 
                
                (from num in Sequence.Range(2,100)
                 where Sequence.Range(2,(int)Math.Sqrt(num)-1).All(num2 => num % num2 != 0) 
                 select num)
                 
                 ) Console.WriteLine(i);
        }
    }
}

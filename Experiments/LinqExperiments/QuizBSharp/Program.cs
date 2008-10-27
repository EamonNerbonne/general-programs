using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuizBSharp
{
    class Program
    {
        static void Main(string[] args) {


            string s1 = "John"; string t1 = "Bart";
            Func<string, string> assignRefO = t => s1 = t;
            Console.WriteLine("s1 = \"{0}\"; (t => s1 = t)(\"{1}\") = \"{2}\"; s1 = \"{3}\"", s1, t1, assignRefO(t1), s1);

            int i1 = 0; int j2 = 1;
            Func<int, int> assignValO = j => i1 = j;
            Console.WriteLine("i1 = {0}; (j => i1 = j)({1}) = {2}; i1 = {3}", i1, j2, assignValO(j2), i1);

            string s2 = "John"; string u = "Lisa";
            Func<string, string> assignRefI = t => t = u;
            Console.WriteLine("s2 = \"{0}\"; u = \"{1}\"; (t => t = u)(s2) = \"{2}\"; s2 = \"{3}\"", s2, u, assignRefI(s2), s2);

            int i2 = 0; int k = 2;
            Func<int, int> assignValI = j => j = k;
            Console.WriteLine("i2 = {0}; k = {1}; (j => j = k)(i2) = {2}; i2 = {3}", i2, k, assignValI(i2), i2);

        }
    }
}

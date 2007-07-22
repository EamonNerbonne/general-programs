using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Xml.Linq;
//using System.Data.Linq;

namespace CompareToTest {
    class Program:IComparable<Program> {
        int a;
        char b;
        IComparable c;

        //a helper function you could place in a utility library.  Uses 
        //a little bit of C# 3.0 syntax for clarity
        public static int CompareHelp<T>(T a, T b, params Func<T, IComparable>[] getters) {
            foreach(var f in getters) {
                int cmp = f(a).CompareTo(f(b));
                if(cmp!=0) return cmp;
            }
            return 0;
        }

        //LINQ based compare function
        public int CompareTo(Program other) {
            return CompareHelp(this,other, (o)=>o.a, (o)=>o.b, (o)=>o.c);
            
        }

        //C# 2.0 based compare function
        public int CompareOld(Program other) {
            return CompareHelp(this,other, 
                delegate(Program o){return o.a;}, 
                delegate(Program o){return o.b;}, 
                delegate(Program o){return o.c;});
        }


        public Program(int a, char b, IComparable c) {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        static void Main(string[] args) {
            Console.WriteLine(new Program(1, 'a', "test").CompareTo(new Program(1, 'a', "abc")));
            Console.WriteLine(new Program(1, 'a', "test").CompareOld(new Program(1, 'a', "abc")));

            Console.ReadLine();
        }
    }
}

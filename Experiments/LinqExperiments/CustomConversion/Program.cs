using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomConversion
{
    class Program
    {
        static void Main(string[] args)
        {
            ClassA varA=new ClassA();
            ClassB varB=new ClassB();
            object objA=new ClassA(),objB=new ClassB();
            Console.WriteLine(varA);
            Console.WriteLine(varB);
            Console.WriteLine(objA);
            Console.WriteLine(objB);
        }
    }

    class ClassA
    {
        public override string ToString()
        {
            return "aha";
        }
    }

    class ClassB
    {
        public static explicit operator object(ClassB b)
        {
            return "notDone";
        }
    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Comparison1020Iterators {
    class Program {
        public delegate long TestDelegate();
        static void Main(string[] args) {
            Test(delegate() { long retval = 0; foreach (int num in new MyCustomCollectionClass()) retval += num; return retval; }, "Custom Iterator");
            Test(delegate (){long retval=0;foreach (int num in new My10CollectionClass().arr) retval+=num; return retval;},"Builtin Array Iterator");
            Test(delegate (){long retval=0;foreach (int num in new My20CollectionClass().arr) retval+=num; return retval;},".net 2.0 type iterator");
            Test(delegate() { long retval = 0; foreach (int num in new My10CollectionClass().arr) retval += num; return retval; }, "Builtin Array Iterator2");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void Test(TestDelegate func,string desc) {
            DateTime dtStart = DateTime.Now;
            long sum = func();
            TimeSpan ts = DateTime.Now.Subtract(dtStart);
            Console.WriteLine(desc + " - Sum {0}; Time: {1} milliseconds.", sum, ts.Milliseconds);
        }
    }


    public class My10CollectionClass :IEnumerable {
        public int[] arr;
        public My10CollectionClass() { arr = new int[30000000]; for (int i = 0; i < arr.Length; i++)arr[i] = i; }
        public IEnumerator GetEnumerator() { return arr.GetEnumerator(); }
    }
    public class My20CollectionClass  {
        public int[] arr;
        public My20CollectionClass() { arr = new int[30000000]; for (int i = 0; i < arr.Length; i++)arr[i] = i; }
        public IEnumerator<int> GetEnumerator() { for (int i = 0; i < arr.Length; i++) yield return arr[i]; }
    }
    public class MyCustomCollectionClass :  IEnumerable<int> {
        public int[] arr;
        public MyCustomCollectionClass() { arr = new int[30000000]; for (int i = 0; i < arr.Length; i++)arr[i] = i; }
        
        //public bool MoveNext() { return ++index < arr.Length; }

        public IEnumerator<int> GetEnumerator() { return new CustomIter(this); }
//        public void Dispose() { }
  //      object IEnumerator.Current { get { return Current; } }

        IEnumerator IEnumerable.GetEnumerator() { return new CustomIter(this); }

        struct CustomIter : IEnumerator<int>
        {
            public CustomIter(MyCustomCollectionClass thingo) { this.arr=thingo.arr; index = -1; }

            int[] arr;
            private int index ;
            public void Reset() { index = -1; }
            public int Current { get { return arr[index]; } }

            public void Dispose()            {           }


            object IEnumerator.Current            {                get { return Current; }            }

            public bool MoveNext()            {             return   ++index<arr.Length;            }


        }
    }
}
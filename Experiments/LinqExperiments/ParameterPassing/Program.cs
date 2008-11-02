using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParameterPassing
{
    class CheckEqual<T> where T:class
    {
        T last = default(T);
        public bool TestEqual(T t) {
            bool retval = eqcheck(t,last);
            last = t;
            return retval;
        }

        static bool Eq(T a, T b) { return a == b; }
        Func<T, T, bool> eqcheck;
        public CheckEqual() {
            eqcheck = Eq;
        }
        public CheckEqual(Func<T, T, bool> eqcheck) {
            this.eqcheck = eqcheck;
        }

    }

    class Program
    {
        static string[] lastone = null;
        static bool ParamsArrayPassing(params string[] strings) {
            bool retval = lastone == strings;
            lastone = strings;
            return retval;
        }

        
        static void Main(string[] args) {
            ParamsArrayPassing("hmm");
            for(int i=0;i<2;i++)
                Console.WriteLine("Does the compiler reuse params arrays in loops? " + ParamsArrayPassing("hmm"));

            CheckEqual<Action<string>> check = new CheckEqual<Action<string>>();

            for (int i = 0; i < 2; i++)
                Console.WriteLine("Does the compiler reuse actions in loops? " + check.TestEqual(Name=>{ }));
            
            Console.WriteLine("Does the compiler reuse actions in duplicated code " + check.TestEqual(Name => { }));
            Console.WriteLine("Does the compiler reuse actions in duplicated code " + check.TestEqual(Name => { }));


            CheckEqual<Action<string>> check2 = new CheckEqual<Action<string>>(
                (a, b) =>
                    a != null && b != null && a.Method == b.Method
                );
            
            check2.TestEqual(Name => { });
            Console.WriteLine("Does the compiler reuse action methods " + check2.TestEqual(Name => { }));

            CheckEqual<Action<string>> check3 = new CheckEqual<Action<string>>(
                (a, b) =>
                    a != null && b != null && a.Target == b.Target
                );
            string x;

            check3.TestEqual(Name => { x = "something1"; });
            Console.WriteLine("Does the compiler reuse action objects " + check3.TestEqual(Name => { x = "something else"; }));

        }
    }
}

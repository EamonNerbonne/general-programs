using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace MemoizeTest
{
    public delegate R MyFunc<T, R>(T t);

    static class Program
    {
        static Func<T, R> MemoizeA<T, R>(Func<T, R> f) {
            Dictionary<T, R> lookup = new Dictionary<T, R>();
            return delegate(T input) {
                R retval;
                if (!lookup.TryGetValue(input, out retval)) {
                    retval = f(input);
                    lookup.Add(input, retval);
                }
                return retval;
            };

        }
        static Func<T, R> Memoize<T, R>(this Func<T, R> f) {
            Dictionary<T, R> cache = new Dictionary<T, R>();
            return t => {
                if (cache.ContainsKey(t))
                    return cache[t];
                else
                    return (cache[t] = f(t));
            };
        }

        static Func<T, R> Rec<T, R>(this Func<Func<T,R>, T, R> f) {
            return t => f(Rec(f), t);
        }
        static Expression<Func<T, R>> Rec<T, R>(this Expression<Func<Func<T, R>, T, R>> f) {
            return t => f(Rec(f), t);
        }

        /*
        static Expression<Func<T, R>> Memoize<T, R>(this Expression<Func<T, R>> f) {
            Dictionary<T, R> cache = new Dictionary<T, R>();
            return t => cache.ContainsKey(t)?cache[t]:cache[t] = f. ;
        }*/


        static void Main(string[] args) {

            MyFunc<int,int> myfunc = a => a+1;
            Func<int, int> func = (Func<int,int>)Delegate.CreateDelegate(typeof(Func<int,int>),myfunc.Target,  myfunc.Method);
            Console.WriteLine("well: " + myfunc(1));

            Expression<Func<int, int>> expr = a => a + 1;

            Func<uint, uint> fib = Rec((Func<uint,uint> f,uint x) => x == 0 ? 1 : f(x - 1) + f(x - 2));


        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EmnExtensions
{
    public static class F
    {
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable) => enumerable ?? Enumerable.Empty<T>();

        public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> iter, Func<T, bool> splitMark)
        {
            var queue = new Queue<T>();
            foreach (var t in iter) {
                if (splitMark(t)) {
                    if (queue.Count != 0) {
                        yield return queue;
                    }

                    queue = new();
                }

                queue.Enqueue(t);
            }

            if (queue.Count != 0) {
                yield return queue;
            }
        }

        public static IEnumerable<Z> ZipWith<X, Y, Z>(this IEnumerable<X> seq, IEnumerable<Y> other, Func<X, Y, Z> f)
        {
            using (var seqE = seq.GetEnumerator())
            using (var otherE = other.GetEnumerator()) {
                while (seqE.MoveNext() && otherE.MoveNext()) {
                    yield return f(seqE.Current, otherE.Current);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static T Swallow<T>(Func<T> trial, Func<T> error)
        {
            try {
                return trial();
            } catch (Exception) {
                return error();
            }
        }

        public static T Swallow<T, TE>(Func<T> trial, Func<TE, T> error)
            where TE : Exception
        {
            try {
                return trial();
            } catch (TE e) {
                return error(e);
            }
        }

        //public struct Maybe<T> {
        //    readonly T val;
        //    readonly Exception e;
        //    public bool HasValue { get { return e == null; } }
        //    public T Value { get { if (!HasValue) throw new InvalidOperationException("Attempted to get a value with an error", e); else return val; } }
        //    Maybe(T val, Exception e) { this.val = val; this.e = e; }
        //    public static Maybe<T> FromException(Exception e) { return new Maybe<T>(default(T), e); }
        //    public static Maybe<T> FromValue(T val) { return new Maybe<T>(val, null); }
        //}

        //public static Maybe<T> Try<T>(Func<T> f) {
        //    try {
        //        return Maybe<T>.FromValue(f());
        //    } catch (Exception e) {
        //        return Maybe<T>.FromException(e);
        //    }
        //}

        //public static void Do(IEnumerable<string> lines) {
        //    var lookup = lines.Select(line => line.Split('|')).ToLookup(line => line[0]);
        //    lines.GroupBy(

        //}

        public static string ToStringOrNull(this object value) => value == null ? null : value.ToString();

        public static int IndexOfMax<T>(this IEnumerable<T> sequence, Func<int, T, bool> filter)
            where T : IComparable<T>
        {
            using (var enumerator = sequence.GetEnumerator()) {
                var retval = -1;
                var max = default(T);
                var currentIndex = -1;
                while (enumerator.GetNext(out var current)) {
                    currentIndex++;
                    if (filter(currentIndex, current) && (retval == -1 || current.CompareTo(max) > 0)) {
                        max = current;
                        retval = currentIndex;
                    }
                }

                return retval;
            }
        }

        public static bool IsFinite(this float f) => !(float.IsInfinity(f) || float.IsNaN(f));
        public static bool IsFinite(this double f) => !(double.IsInfinity(f) || double.IsNaN(f));

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public static bool GetNext<T>(this IEnumerator<T> enumerator, out T nextValue)
        {
            if (enumerator.MoveNext()) {
                nextValue = enumerator.Current;
                return true;
            }

            nextValue = default(T);
            return false;
        }


        public static IEnumerable<T> AsEnumerable<T>(Func<T> func)
        {
            while (true) {
                yield return func();
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(Action init, Func<T> func)
        {
            init();
            return AsEnumerable(func);
        }


        //no-op functions to support C# type inference:
        public static Func<T> Create<T>(Func<T> func) => func;
        public static Func<A, T> Create<A, T>(Func<A, T> func) => func;
        public static Func<A, B, T> Create<A, B, T>(Func<A, B, T> func) => func;
        public static Func<A, B, C, T> Create<A, B, C, T>(Func<A, B, C, T> func) => func;
        public static Action<A> Create<A>(Action<A> action) => action;
        public static Action<A, B> Create<A, B>(Action<A, B> action) => action;
        public static Action<A, B, C> Create<A, B, C>(Action<A, B, C> action) => action;


        public static Func<T> Curry<A, T>(Func<A, T> func, A a) => () => func(a);
        public static Func<B, T> Curry<A, B, T>(Func<A, B, T> func, A a) => b => func(a, b);
        public static Func<B, C, T> Curry<A, B, C, T>(Func<A, B, C, T> func, A a) => (b, c) => func(a, b, c);
    }
}

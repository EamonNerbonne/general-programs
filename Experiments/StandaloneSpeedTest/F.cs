using System;
using System.Collections.Generic;

namespace EmnExtensions
{
	public static class F
	{
		public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> iter, Func<T, bool> splitMark) {
			var queue = new Queue<T>();
			foreach(T t in iter) {
				if(splitMark(t)) {
					if(queue.Count != 0) {
						yield return queue;
					}
					queue = new Queue<T>();
				}
				queue.Enqueue(t);
			}
			if(queue.Count != 0) {
				yield return queue;
			}
		}

        public static IEnumerable<Z> ZipWith<X, Y, Z>(this IEnumerable<X> seq,  IEnumerable<Y> other, Func<X, Y, Z> f) {
            var seqE = seq.GetEnumerator();
            var otherE = other.GetEnumerator();
            while (seqE.MoveNext() && otherE.MoveNext()) 
                yield return f(seqE.Current, otherE.Current);
        }
 
		public static T Swallow<T>(Func<T> trial, Func<T> error) {
			try {
				return trial();
			} catch(Exception) {
				return error();
			}
		}

		public static string ToStringOrNull(this object obj) {
			return obj == null ? null : obj.ToString();
		}

        public static int IndexOfMax<T>(this IEnumerable<T> iter,Func<int,T,bool> filter) where T:IComparable<T> {
            var enumerator = iter.GetEnumerator();
            int retval = -1;
            T max = default(T);
            int currentIndex = -1;
            T current;
            while (enumerator.GetNext(out current)) {
                currentIndex++;
                if (filter(currentIndex, current) && (retval==-1||current.CompareTo(max) > 0)) {
                    max = current;
                    retval = currentIndex;
                }
            }

            return retval;
        }

        public static bool IsFinite(this float f) {
            return !(float.IsInfinity(f) || float.IsNaN(f));
        }
        public static bool IsFinite(this double f) {
            return !(double.IsInfinity(f) || double.IsNaN(f));
        }

        public static bool GetNext<T>(this IEnumerator<T> enumerator, out T nextVal) {
            if (enumerator.MoveNext()) {
                nextVal = enumerator.Current;
                return true;
            } else {
                nextVal = default(T);
                return false;
            }
        }


        public static IEnumerable<T> AsEnumerable<T>(Func<T> func) {
			while (true)yield return func();
        }

        public static IEnumerable<T> AsEnumerable<T>(Action init,Func<T> func) {
            init();
			return AsEnumerable(func);
        }

		//no-op functions to support C# type inference:
		public static Func<T> Create<T>(Func<T> func) { return func; }
		public static Func<A,T> Create<A,T>(Func<A,T> func) { return func; }
		public static Func<A,B, T> Create<A,B, T>(Func<A,B, T> func) { return func; }
		public static Func<A, B,C, T> Create<A, B,C, T>(Func<A, B,C, T> func) { return func; }
		public static Action<A> Create<A>(Action<A> action) { return action; }
		public static Action<A,B> Create<A,B>(Action<A,B> action) { return action; }
		public static Action<A,B,C> Create<A,B,C>(Action<A,B,C> action) { return action; }
	}
}

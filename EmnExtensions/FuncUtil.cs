using System;
using System.Collections.Generic;

namespace EmnExtensions
{
	public static class Functional
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

        public static int IndexOfMax<T>(this IEnumerable<T> iter,Func<T,bool> filter) where T:IComparable<T> {
            var enumerator = iter.GetEnumerator();
            if(!enumerator.MoveNext())
                return -1;
            T max = enumerator.Current;
            int retval = 0;
            int currentIndex=0;
            while(enumerator.MoveNext()) {
                currentIndex++;
                if (filter(enumerator.Current) &&enumerator.Current.CompareTo(max) > 0) {
                    max = enumerator.Current;
                    retval = currentIndex;
                }
            }
            return retval;
        }
	}
}

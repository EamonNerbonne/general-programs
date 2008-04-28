using System;
using System.Collections.Generic;

namespace EamonExtensionsLinq
{
	public static class FuncUtil
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
	}
}

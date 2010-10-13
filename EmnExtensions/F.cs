using System;
using System.Collections.Generic;
using System.Linq;

namespace EmnExtensions {
	public static class F {

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable) { return enumerable ?? Enumerable.Empty<T>(); }

		public static IEnumerable<IEnumerable<T>> SplitWhen<T>(this IEnumerable<T> iter, Func<T, bool> splitMark) {
			var queue = new Queue<T>();
			foreach (T t in iter) {
				if (splitMark(t)) {
					if (queue.Count != 0) {
						yield return queue;
					}
					queue = new Queue<T>();
				}
				queue.Enqueue(t);
			}
			if (queue.Count != 0) {
				yield return queue;
			}
		}

		public static IEnumerable<Z> ZipWith<X, Y, Z>(this IEnumerable<X> seq, IEnumerable<Y> other, Func<X, Y, Z> f) {
			using (var seqE = seq.GetEnumerator())
			using (var otherE = other.GetEnumerator()) {
				while (seqE.MoveNext() && otherE.MoveNext())
					yield return f(seqE.Current, otherE.Current);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static T Swallow<T>(Func<T> trial, Func<T> error) {
			try {
				return trial();
			} catch (Exception) {
				return error();
			}
		}

		public static T Swallow<T, TE>(Func<T> trial, Func<TE, T> error) where TE : Exception {
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

		public static string ToStringOrNull(this object value) {
			return value == null ? null : value.ToString();
		}

		public static int IndexOfMax<T>(this IEnumerable<T> sequence, Func<int, T, bool> filter) where T : IComparable<T> {
			using (var enumerator = sequence.GetEnumerator()) {
				int retval = -1;
				T max = default(T);
				int currentIndex = -1;
				T current;
				while (enumerator.GetNext(out current)) {
					currentIndex++;
					if (filter(currentIndex, current) && (retval == -1 || current.CompareTo(max) > 0)) {
						max = current;
						retval = currentIndex;
					}
				}

				return retval;
			}
		}

		public static bool IsFinite(this float f) {
			return !(float.IsInfinity(f) || float.IsNaN(f));
		}
		public static bool IsFinite(this double f) {
			return !(double.IsInfinity(f) || double.IsNaN(f));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public static bool GetNext<T>(this IEnumerator<T> enumerator, out T nextValue) {
			if (enumerator.MoveNext()) {
				nextValue = enumerator.Current;
				return true;
			} else {
				nextValue = default(T);
				return false;
			}
		}


		public static IEnumerable<T> AsEnumerable<T>(Func<T> func) {
			while (true) yield return func();
		}

		public static IEnumerable<T> AsEnumerable<T>(Action init, Func<T> func) {
			init();
			return AsEnumerable(func);
		}


		//no-op functions to support C# type inference:
		public static Func<T> Create<T>(Func<T> func) { return func; }
		public static Func<A, T> Create<A, T>(Func<A, T> func) { return func; }
		public static Func<A, B, T> Create<A, B, T>(Func<A, B, T> func) { return func; }
		public static Func<A, B, C, T> Create<A, B, C, T>(Func<A, B, C, T> func) { return func; }
		public static Action<A> Create<A>(Action<A> action) { return action; }
		public static Action<A, B> Create<A, B>(Action<A, B> action) { return action; }
		public static Action<A, B, C> Create<A, B, C>(Action<A, B, C> action) { return action; }
	}
}

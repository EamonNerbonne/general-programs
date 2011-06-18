using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhereTest {
	static class FastFilteredEnumerable {
		public static IEnumerable<T> Where<T>(IEnumerable<T> list, Func<T, bool> pred) {
			return list is FastFilteredEnumerable1<T> ? new FastFilteredEnumerable2<T>((FastFilteredEnumerable1<T>)list, pred)
				: list is FastFilteredEnumerable2<T> ? new FastFilteredEnumerable3<T>((FastFilteredEnumerable2<T>)list, pred)
				: list is FastFilteredEnumerable3<T> ? new FastFilteredEnumerable4<T>((FastFilteredEnumerable3<T>)list, pred)
				: (IEnumerable<T>)new FastFilteredEnumerable1<T>(list, pred);
		}


		class FastFilteredEnumerable1<T> : IEnumerable<T>, IEnumerator<T> {
			public FastFilteredEnumerable1(IEnumerable<T> list, Func<T, bool> pred) { this.pred = pred; this.list = list; current = default(T); enumerator = null; }
			public readonly Func<T, bool> pred;
			public readonly IEnumerable<T> list;
			IEnumerator<T> enumerator;
			T current;

			public T Current { get { return current; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }

			public void Dispose() { if (enumerator != null) { enumerator.Dispose(); enumerator = null; } }

			public bool MoveNext() {
				while (enumerator.MoveNext()) {
					current = enumerator.Current;
					//var next = enumerator.Current;
					if (pred(current)) {
						//current = next;
						return true;
					}
				}
				return false;
			}

			public void Reset() { enumerator.Reset(); current = default(T); }

			public IEnumerator<T> GetEnumerator() {
				if (enumerator == null) {
					enumerator = list.GetEnumerator();
					return this;
				} else {
					return new FastFilteredEnumerable1<T>(list, pred).GetEnumerator();
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		class FastFilteredEnumerable2<T> : IEnumerable<T>, IEnumerator<T> {
			public FastFilteredEnumerable2(FastFilteredEnumerable1<T> list, Func<T, bool> pred2) { this.list = list.list; this.pred = list.pred; this.pred2 = pred2; current = default(T); enumerator = null; }
			public FastFilteredEnumerable2(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2) { this.list = list; this.pred = pred; this.pred2 = pred2; current = default(T); enumerator = null; }
			public readonly Func<T, bool> pred, pred2;
			public readonly IEnumerable<T> list;
			IEnumerator<T> enumerator;
			T current;
			public T Current { get { return current; } }
			public void Dispose() { if (enumerator != null) { enumerator.Dispose(); enumerator = null; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }

			public bool MoveNext() {
				while (enumerator.MoveNext()) {
					var next = enumerator.Current;
					if (pred(next) && pred2(next)) {
						current = next;
						return true;
					}
				}
				return false;
			}

			public void Reset() { enumerator.Reset(); current = default(T); }

			public IEnumerator<T> GetEnumerator() {
				if (enumerator == null) {
					enumerator = list.GetEnumerator();
					return this;
				} else {
					return new FastFilteredEnumerable2<T>(list, pred, pred2).GetEnumerator();
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		class FastFilteredEnumerable3<T> : IEnumerable<T>, IEnumerator<T> {
			public FastFilteredEnumerable3(FastFilteredEnumerable2<T> list, Func<T, bool> pred3) { this.list = list.list; this.pred = list.pred; this.pred2 = list.pred2; this.pred3 = pred3; current = default(T); enumerator = null; }
			public FastFilteredEnumerable3(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2, Func<T, bool> pred3) { this.list = list; this.pred = pred; this.pred2 = pred2; this.pred3 = pred3; current = default(T); enumerator = null; }
			public readonly Func<T, bool> pred, pred2, pred3;
			public readonly IEnumerable<T> list;
			IEnumerator<T> enumerator;
			T current;
			public T Current { get { return current; } }
			public void Dispose() { if (enumerator != null) { enumerator.Dispose(); enumerator = null; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }

			public bool MoveNext() {
				while (enumerator.MoveNext()) {
					var next = enumerator.Current;
					if (pred(next) && pred2(next) && pred3(next)) {
						current = next;
						return true;
					}
				}
				return false;
			}

			public void Reset() { enumerator.Reset(); current = default(T); }

			public IEnumerator<T> GetEnumerator() {
				if (enumerator == null) {
					enumerator = list.GetEnumerator();
					return this;
				} else {
					return new FastFilteredEnumerable3<T>(list, pred, pred2, pred3).GetEnumerator();
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

		class FastFilteredEnumerable4<T> : IEnumerable<T>, IEnumerator<T> {
			public FastFilteredEnumerable4(FastFilteredEnumerable3<T> list, Func<T, bool> pred4) { this.list = list.list; this.pred = list.pred; this.pred2 = list.pred2; this.pred3 = list.pred3; this.pred4 = pred4; current = default(T); enumerator = null; }
			public FastFilteredEnumerable4(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2, Func<T, bool> pred3, Func<T, bool> pred4) { this.list = list; this.pred = pred; this.pred2 = pred2; this.pred3 = pred3; this.pred4 = pred4; current = default(T); enumerator = null; }
			public readonly Func<T, bool> pred, pred2, pred3, pred4;
			public readonly IEnumerable<T> list;
			IEnumerator<T> enumerator;
			T current;
			public T Current { get { return current; } }
			public void Dispose() { if (enumerator != null) { enumerator.Dispose(); enumerator = null; } }
			object System.Collections.IEnumerator.Current { get { return Current; } }

			public bool MoveNext() {
				while (enumerator.MoveNext()) {
					var next = enumerator.Current;
					if (pred(next) && pred2(next) && pred3(next) && pred4(next)) {
						current = next;
						return true;
					}
				}
				return false;
			}

			public void Reset() { enumerator.Reset(); current = default(T); }

			public IEnumerator<T> GetEnumerator() {
				if (enumerator == null) {
					enumerator = list.GetEnumerator();
					return this;
				} else {
					return new FastFilteredEnumerable4<T>(list, pred, pred2, pred3, pred4).GetEnumerator();
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}

	}
}

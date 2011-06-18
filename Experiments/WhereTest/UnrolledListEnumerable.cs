using System;
using System.Collections.Generic;

namespace WhereTest {
	static class UnrolledListEnumerable {
		public static IEnumerable<T> Where<T>(IEnumerable<T> list, Func<T, bool> pred) { return new EnumerableImpl<T>(list, pred); }

		sealed class EnumerableImpl<T> : IEnumerable<T> {
			public EnumerableImpl(IEnumerable<T> list, Func<T, bool> pred) {
				this.pred = pred;
				this.list = list;
				var flist = list as EnumerableImpl<T>;
				height = flist != null ? flist.height + 1 : 1;
			}
			readonly Func<T, bool> pred;
			readonly IEnumerable<T> list;
			readonly int height;

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

			abstract class AbstractEnumerator : IEnumerator<T> {
				protected T current;
				public T Current { get { return current; } }
				protected readonly IEnumerator<T> underlying;
				public void Dispose() { underlying.Dispose(); }
				object System.Collections.IEnumerator.Current { get { return Current; } }
				protected AbstractEnumerator(IEnumerator<T> underlying) { this.underlying = underlying; }

				public abstract bool MoveNext();

				public void Reset() {
					current = default(T);
					underlying.Reset();
				}
			}

			AbstractEnumerator Create() {
				if (height == 1) return new Enumerator1(this);
				else if (height == 2) return new Enumerator2(this);
				else if (height == 3) return new Enumerator3(this);
				else if (height == 4) return new Enumerator4(this);
				else if (height == 5) return new Enumerator5(this);
				else if (height == 6) return new Enumerator6(this);
				else return new Enumerator7(this);
			}

			class Enumerator1 : AbstractEnumerator {
				protected readonly Func<T, bool> pred1;
				public Enumerator1(EnumerableImpl<T> list) : base(list.list.GetEnumerator()) { pred1 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}

			class Enumerator2 : Enumerator1 {
				protected readonly Func<T, bool> pred2;
				public Enumerator2(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred2 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}

			class Enumerator3 : Enumerator2 {
				protected readonly Func<T, bool> pred3;
				public Enumerator3(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred3 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next) && pred3(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}

			class Enumerator4 : Enumerator3 {
				protected readonly Func<T, bool> pred4;

				public Enumerator4(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred4 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next) && pred3(next) && pred4(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}

			class Enumerator5 : Enumerator4 {
				protected readonly Func<T, bool> pred5;
				public Enumerator5(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred5 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next) && pred3(next) && pred4(next) && pred5(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}
			class Enumerator6 : Enumerator5 {
				protected readonly Func<T, bool> pred6;
				public Enumerator6(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred6 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next) && pred3(next) && pred4(next) && pred5(next) && pred6(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}
			class Enumerator7 : Enumerator6 {
				protected readonly Func<T, bool> pred7;
				public Enumerator7(EnumerableImpl<T> list) : base((EnumerableImpl<T>)list.list) { pred7 = list.pred; }
				public override bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred1(next) && pred2(next) && pred3(next) && pred4(next) && pred5(next) && pred6(next) && pred7(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}
			}


			static AbstractEnumerator CreateArrayEnumerator(EnumerableImpl<T> filteredList) {
				var pred = new Func<T, bool>[filteredList.height];
				while (filteredList.height > 1) {
					pred[filteredList.height - 1] = filteredList.pred;
					filteredList = (EnumerableImpl<T>)filteredList.list;
				}
				pred[0] = filteredList.pred;
				return new ArrayEnumerator(pred, filteredList.list.GetEnumerator());
			}

			class ArrayEnumerator : AbstractEnumerator {
				readonly Func<T, bool>[] pred;

				public ArrayEnumerator(Func<T, bool>[] pred, IEnumerator<T> underlying) : base(underlying) { this.pred = pred; }
				public override bool MoveNext() {
				whileloop: while (underlying.MoveNext()) {
						var next = underlying.Current;
						for (int i = 0; i < pred.Length; i++)
							if (!pred[i](next))
								goto whileloop;

						current = next;
						return true;
					}
					return false;
				}
			}


			public IEnumerator<T> GetEnumerator() { return Create(); }
		}

	}
}

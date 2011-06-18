using System;
using System.Collections.Generic;

namespace WhereTest {
	static class GenericFilterListEnumerable {
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


			interface IPred {
				bool Predicate(T val);
				IEnumerable<T> Init(EnumerableImpl<T> list);
			}

			struct SinglePred : IPred {
				Func<T, bool> pred;
				public bool Predicate(T val) { return pred(val); }
				public IEnumerable<T> Init(EnumerableImpl<T> list) {
					pred = list.pred;
					return list.list;
				}
			}

			struct CombinedPred<TNext> : IPred where TNext : struct, IPred {
				Func<T, bool> pred;
				TNext next;
				public bool Predicate(T val) { return pred(val) && next.Predicate(val); }
				public IEnumerable<T> Init(EnumerableImpl<T> top) {
					//next = new TNext();
					var bottom = (EnumerableImpl<T>)next.Init(top);
					pred = bottom.pred;
					return bottom.list;
				}
			}

			sealed class EnumImpl<TPred> : IEnumerator<T> where TPred : struct, IPred {
				readonly IEnumerator<T> underlying;
				readonly TPred pred;
				public EnumImpl(EnumerableImpl<T> list) { pred = new TPred(); var tail = pred.Init(list); underlying = tail.GetEnumerator(); }
				T current;
				public T Current { get { return current; } }
				public void Dispose() { underlying.Dispose(); }
				object System.Collections.IEnumerator.Current { get { return Current; } }

				public bool MoveNext() {
					while (underlying.MoveNext()) {
						var next = underlying.Current;
						if (pred.Predicate(next)) {
							current = next;
							return true;
						}
					}
					return false;
				}

				public void Reset() { current = default(T); underlying.Reset(); }
			}


#if true
			public IEnumerator<T> GetEnumerator() {
				if (height == 1) return new EnumImpl<SinglePred>(this);
				else if (height == 2) return new EnumImpl<CombinedPred<SinglePred>>(this);
				else if (height == 3) return new EnumImpl<CombinedPred<CombinedPred<SinglePred>>>(this);
				else if (height == 4) return new EnumImpl<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>(this);
				else if (height == 5) return new EnumImpl<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>(this);
				else if (height == 6) return new EnumImpl<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>>(this);
				else return new EnumImpl<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>>>(this);
			}
#else
			//apparently this borks NGEN!
			public IEnumerator<T> GetEnumerator() { return CreatePredEnumByHeight<SinglePred>(height); }
			IEnumerator<T> CreatePredEnumByHeight<TPred>(int remainingheight) where TPred : struct, IPred {
				if (remainingheight == 1) return new EnumImpl<TPred>(this);
				else return CreatePredEnumByHeight<CombinedPred<TPred>>(remainingheight - 1);
			}
#endif
		}
	}
}

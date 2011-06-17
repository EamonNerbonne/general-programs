using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using FilterT = System.Func<System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>, System.Collections.Generic.IEnumerable<int>>;
using PredFactory = System.Func<int, System.Func<int, bool>>;


class Test {
	static void Main() {
		const int size = 10000;
		const int repeats = 30;

		var tests = new[]{
			new { Name = "false", Query = (PredFactory)(n => x => false)},
			new { Name = "true ", Query = (PredFactory)(n => x => true)},
			new { Name = "mod  ", Query = (PredFactory)(n => x => x % (n + 1) != 0)},
		};

		var filters = new[]{
			new { Name = "naive", Filter = (FilterT)NaiveWhere},
			new { Name = "linq ", Filter = (FilterT)Enumerable.Where },
			new { Name = "list ", Filter = (FilterT)ListWhere },
			new { Name = "fast ", Filter = (FilterT)FastWhere},
		};

		foreach (var depth in Enumerable.Range(1, 10)) {
			foreach (var test in tests) {
				foreach (var filterAndResult in filters
						.Select(filter => new { filter.Name, Result = TimeFilteredEnumerableCount(depth, size, repeats, filter.Filter, test.Query) })
						.OrderBy(result => result.Result.Item1)) {
					Console.WriteLine(test.Name + "*" + depth + " via " + filterAndResult.Name + ": " + filterAndResult.Result);
				}
				Console.WriteLine();
			}
			Console.WriteLine("------------------------------------");
		}
	}

	static IEnumerable<T> NaiveWhere<T>(IEnumerable<T> list, Func<T, bool> pred) {
		foreach (var item in list)
			if (pred(item))
				yield return item;
	}
	static IEnumerable<T> FastWhere<T>(IEnumerable<T> list, Func<T, bool> pred) {

		return list is FilteredEnum<T> ? new FilteredEnum2<T>((FilteredEnum<T>)list, pred)
			: list is FilteredEnum2<T> ? new FilteredEnum3<T>((FilteredEnum2<T>)list, pred)
			: list is FilteredEnum3<T> ? new FilteredEnum4<T>((FilteredEnum3<T>)list, pred)
			: (IEnumerable<T>)new FilteredEnum<T>(list, pred);
	}
	static IEnumerable<T> ListWhere<T>(IEnumerable<T> list, Func<T, bool> pred) {
		return new FilterListEnum<T>(list, pred);
	}

	static Tuple<double, int> TimeFilteredEnumerableCount(int depth, int size, int repeats, FilterT whereImpl, PredFactory filterForDepth) {
		return TimeCount(repeats, MakeFilteredEnumerable(depth, size, whereImpl, filterForDepth));
	}

	static IEnumerable<int> MakeFilteredEnumerable(int depth, int size, FilterT whereImpl, PredFactory filterForDepth) {
		return Enumerable.Range(1, depth).Aggregate(Enumerable.Range(0, size), (list, n) => whereImpl(list, filterForDepth(n)));
	}

	static Tuple<double, int> TimeCount(int repeats, IEnumerable<int> input) {
		return Enumerable.Repeat(input, 50).Select(list => TimeCountOnce(repeats, list)).OrderBy(x => x).Skip(9).First();
	}


	static Tuple<double, int> TimeCountOnce(int repeats, IEnumerable<int> input) {
		Stopwatch sw = Stopwatch.StartNew();
		int count = 0;
		for (int i = 0; i < repeats; i++)
			count += input.Count();
		sw.Stop();
		return Tuple.Create(sw.Elapsed.TotalMilliseconds, count);
	}


	class FilterListEnum<T> : IEnumerable<T> {

		public FilterListEnum(IEnumerable<T> list, Func<T, bool> pred) {
			this.pred = pred;
			this.list = list;
			var flist = list as FilterListEnum<T>;
			height = flist != null ? flist.height + 1 : 1;
		}
		public readonly Func<T, bool> pred;
		public readonly IEnumerable<T> list;
		public readonly int height;

		static AbstractEnumerator Create(FilterListEnum<T> filteredList) {
			if (filteredList.height == 1)
				return new Enumerator1(filteredList);
			else if (filteredList.height == 2)
				return new Enumerator2(filteredList);
			else if (filteredList.height == 3)
				return new Enumerator3(filteredList);
			else if (filteredList.height == 4)
				return new Enumerator4(filteredList);
			else if (filteredList.height == 5)
				return new Enumerator5(filteredList);
			else if (filteredList.height == 6)
				return new Enumerator6(filteredList);
			else //if (filteredList.height == 7)
				return new Enumerator7(filteredList);
		}

		static AbstractEnumerator CreateArrayEnumerator(FilterListEnum<T> filteredList) {
			var pred = new Func<T, bool>[filteredList.height];
			while (filteredList.height > 1) {
				pred[filteredList.height - 1] = filteredList.pred;
				filteredList = (FilterListEnum<T>)filteredList.list;
			}
			pred[0] = filteredList.pred;
			return new ArrayEnumerator(pred, filteredList.list.GetEnumerator());
		}

		class ArrayEnumerator : AbstractEnumerator {
			public readonly Func<T, bool>[] pred;

			public ArrayEnumerator(Func<T, bool>[] pred, IEnumerator<T> underlying) : base(underlying) { this.pred = pred; }
			public override bool Predicate(T val) {
				foreach (var subpred in pred)
					if (!subpred(val))
						return false;
				return true;
			}
		}

		abstract class AbstractEnumerator : IEnumerator<T> {
			protected T current;
			public T Current { get { return current; } }
			protected IEnumerator<T> underlying;
			public void Dispose() { underlying.Dispose(); }
			object System.Collections.IEnumerator.Current { get { return Current; } }
			protected AbstractEnumerator(IEnumerator<T> underlying) { this.underlying = underlying; }

			public abstract bool Predicate(T val);

			public bool MoveNext() {
				while (underlying.MoveNext()) {
					var next = underlying.Current;
					if (Predicate(next)) {
						current = next;
						return true;
					}
				}
				return false;
			}


			public void Reset() {
				current = default(T);
				underlying.Reset();
			}
		}

		interface IPred {
			bool Predicate(T val);
			void Init(FilterListEnum<T> list);
			IEnumerator<T> GetEnumerator();

		}

		struct SinglePred : IPred {
			Func<T, bool> pred;
			IEnumerator<T> enumerator;
			public IEnumerator<T> GetEnumerator() { return enumerator; }
			public bool Predicate(T val) { return pred(val); }
			public void Init(FilterListEnum<T> list) {
				enumerator = list.list.GetEnumerator();
				pred = list.pred;
			}
		}

		struct CombinedPred<TNext> : IPred where TNext : IPred {
			Func<T, bool> pred;
			TNext next;
			public IEnumerator<T> GetEnumerator() { return next.GetEnumerator(); }
			public bool Predicate(T val) { return next.Predicate(val) && pred(val); }
			public void Init(FilterListEnum<T> list) {
				pred = list.pred;
				next.Init((FilterListEnum<T>)list.list);
			}
		}



		sealed class EnumImpl<TPred> : IEnumerator<T> where TPred : IPred {
			readonly IEnumerator<T> underlying;
			readonly TPred pred;
			public EnumImpl(TPred pred) { this.underlying = pred.GetEnumerator(); this.pred = pred; }
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

		TPred CreatePred<TPred>() where TPred : IPred, new() { TPred retval = new TPred(); retval.Init(this); return retval; }
		EnumImpl<TPred> CreatePredEnum<TPred>() where TPred : IPred, new() { return new EnumImpl<TPred>(CreatePred<TPred>()); }

		IEnumerator<T> CreateGen(FilterListEnum<T> filteredList) {
			if (filteredList.height == 1)
				return CreatePredEnum<SinglePred>();
			else if (filteredList.height == 2)
				return CreatePredEnum<CombinedPred<SinglePred>>();
			else if (filteredList.height == 3)
				return CreatePredEnum<CombinedPred<CombinedPred<SinglePred>>>();
			else if (filteredList.height == 4)
				return CreatePredEnum<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>();
			else if (filteredList.height == 5)
				return CreatePredEnum<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>();
			else if (filteredList.height == 6)
				return CreatePredEnum<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>>();
			else //if (filteredList.height == 7)
				return CreatePredEnum<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<CombinedPred<SinglePred>>>>>>>();
		}



		class Enumerator1 : AbstractEnumerator {
			public readonly Func<T, bool> pred1;

			public Enumerator1(FilterListEnum<T> list) : base(list.list.GetEnumerator()) { this.pred1 = list.pred; }

			public override bool Predicate(T val) { return pred1(val); }
		}

		class Enumerator2 : Enumerator1 {
			public readonly Func<T, bool> pred2;

			public Enumerator2(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred2 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred2(val); }
		}

		class Enumerator3 : Enumerator2 {
			public readonly Func<T, bool> pred3;

			public Enumerator3(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred3 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred3(val); }
		}

		class Enumerator4 : Enumerator3 {
			public readonly Func<T, bool> pred4;

			public Enumerator4(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred4 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred4(val); }
		}

		class Enumerator5 : Enumerator4 {
			public readonly Func<T, bool> pred5;

			public Enumerator5(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred5 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred5(val); }
		}
		class Enumerator6 : Enumerator5 {
			public readonly Func<T, bool> pred6;

			public Enumerator6(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred6 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred6(val); }
		}
		class Enumerator7 : Enumerator6 {
			public readonly Func<T, bool> pred7;

			public Enumerator7(FilterListEnum<T> list) : base((FilterListEnum<T>)list.list) { this.pred7 = list.pred; }
			public override bool Predicate(T val) { return base.Predicate(val) && pred7(val); }
		}



		public IEnumerator<T> GetEnumerator() { return Create(this); }

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}


	class FilteredEnum<T> : IEnumerable<T>, IEnumerator<T> {
		public FilteredEnum(IEnumerable<T> list, Func<T, bool> pred) { this.pred = pred; this.list = list; current = default(T); enumerator = null; }
		public readonly Func<T, bool> pred;
		public readonly IEnumerable<T> list;
		IEnumerator<T> enumerator;
		T current;

		public T Current { get { return current; } }
		object System.Collections.IEnumerator.Current { get { return Current; } }

		public void Dispose() { if (enumerator != null) { enumerator.Dispose(); enumerator = null; } }


		public bool MoveNext() {
			while (enumerator.MoveNext()) {
				var next = enumerator.Current;
				if (pred(next)) {
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
				return new FilteredEnum<T>(list, pred).GetEnumerator();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	class FilteredEnum2<T> : IEnumerable<T>, IEnumerator<T> {
		public FilteredEnum2(FilteredEnum<T> list, Func<T, bool> pred2) { this.list = list.list; this.pred = list.pred; this.pred2 = pred2; current = default(T); enumerator = null; }
		public FilteredEnum2(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2) { this.list = list; this.pred = pred; this.pred2 = pred2; current = default(T); enumerator = null; }
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
				return new FilteredEnum2<T>(list, pred, pred2).GetEnumerator();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	class FilteredEnum3<T> : IEnumerable<T>, IEnumerator<T> {
		public FilteredEnum3(FilteredEnum2<T> list, Func<T, bool> pred3) { this.list = list.list; this.pred = list.pred; this.pred2 = list.pred2; this.pred3 = pred3; current = default(T); enumerator = null; }
		public FilteredEnum3(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2, Func<T, bool> pred3) { this.list = list; this.pred = pred; this.pred2 = pred2; this.pred3 = pred3; current = default(T); enumerator = null; }
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
				return new FilteredEnum3<T>(list, pred, pred2, pred3).GetEnumerator();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	class FilteredEnum4<T> : IEnumerable<T>, IEnumerator<T> {
		public FilteredEnum4(FilteredEnum3<T> list, Func<T, bool> pred4) { this.list = list.list; this.pred = list.pred; this.pred2 = list.pred2; this.pred3 = list.pred3; this.pred4 = pred4; current = default(T); enumerator = null; }
		public FilteredEnum4(IEnumerable<T> list, Func<T, bool> pred, Func<T, bool> pred2, Func<T, bool> pred3, Func<T, bool> pred4) { this.list = list; this.pred = pred; this.pred2 = pred2; this.pred3 = pred3; this.pred4 = pred4; current = default(T); enumerator = null; }
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
				return new FilteredEnum4<T>(list, pred, pred2, pred3, pred4).GetEnumerator();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
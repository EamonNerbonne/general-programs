using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

class Test {
	static void Main() {
		int size = 10000000;
		RunTests(size, x => false, false);
		RunTests(size, x => true, false);
		RunTests(size, x => false, false);
		RunTests(size, x => true, false);
		Console.WriteLine("Always false");
		RunTests(size, x => false, true);

		Console.WriteLine("Always true");
		RunTests(size, x => true,true);

	}

	static void RunTests(int size, Func<string, bool> predicate,bool bench) {
		for (int i = 1; i <= 5; i++) {
			RunTest(i, size, predicate, bench);
		}
	}

	static void RunTest(int depth, int size, Func<string, bool> predicate, bool bench) {
		IEnumerable<string> input = Enumerable.Repeat("value", size);

		for (int i = 0; i < depth; i++) {
			/*
			input = input.Where(predicate);
			 /*/
			input = MyWhere2(input, predicate);
			/**/

			//input = MyWhere(input, predicate);

		}
		if (!bench)
			Count(input);
		else {
			Stopwatch sw = Stopwatch.StartNew();
			Count(input); 
			//input.Count();
			sw.Stop();
			Console.WriteLine("Depth: {0} Size: {1} Time: {2}ms", depth, size, sw.ElapsedMilliseconds);
		}
	}
	static int Count<T>(IEnumerable<T> list) {
		int count = 0;
		using (var enumerator = list.GetEnumerator())
			while (enumerator.MoveNext())
				count++;
		return count;
	}

	static IEnumerable<T> MyWhere<T>(IEnumerable<T> list, Func<T, bool> pred) {
		foreach (var item in list)
			if (pred(item))
				yield return item;
	}
	static IEnumerable<T> MyWhere2<T>(IEnumerable<T> list, Func<T, bool> pred) {

		return list is FilteredEnum<T> ? new FilteredEnum2<T>((FilteredEnum<T>)list, pred)
			: list is FilteredEnum2<T> ? new FilteredEnum3<T>((FilteredEnum2<T>)list, pred) 
			: (IEnumerable<T>)new FilteredEnum<T>(list, pred);
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
		public readonly Func<T, bool> pred, pred2,pred3;
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
				return new FilteredEnum3<T>(list, pred, pred2,pred3).GetEnumerator();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

}
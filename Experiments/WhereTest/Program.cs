using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WhereTest;
using FilterT = System.Func<System.Collections.Generic.IEnumerable<int>, System.Func<int, bool>, System.Collections.Generic.IEnumerable<int>>;
using PredFactory = System.Func<int, System.Func<int, bool>>;

namespace WhereTest {
	class TestExecutor {
		static void Main() {
			const int size = 100000;
			const int repeats = 3;

			var tests = new[]{
			new { Name = "false", Query = (PredFactory)(n => x => false)},
			new { Name = "true ", Query = (PredFactory)(n => x => true)},
			new { Name = "mod  ", Query = (PredFactory)(n => x => x % (n + 1) != 0)},
		};

			var filters = new[]{
			//new { Name = "naive", Filter = (FilterT)NaiveFilteredEnumberable.Where},
			new { Name = "linq ", Filter = (FilterT)Enumerable.Where },
			//new { Name = "list ", Filter = (FilterT)GenericFilterListEnumerable.Where },
			new { Name = "unroll", Filter = (FilterT)UnrolledListEnumerable.Where },
			new { Name = "fast ", Filter = (FilterT)FastFilteredEnumerable.Where},
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
				count += Count(input);
			sw.Stop();
			return Tuple.Create(sw.Elapsed.TotalMilliseconds, count);
		}

		private static int Count<T>(IEnumerable<T> input) {
			int count = 0;
			using (var enumerable = input.GetEnumerator()) 
				while (enumerable.MoveNext())
					count++;
			return count;
		}
	}
}
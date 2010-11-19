using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms {
	public static class SortedUnionAlgorithm {

		class SafeEnumeratorIntComparer : IComparer<IEnumerator<int>> {
			public int Compare(IEnumerator<int> x, IEnumerator<int> y) {
				int xc = x.Current, yc = y.Current;
				return xc < yc ? -1 : xc > yc ? 1 : 0;
			}
		}
		class FastEnumeratorIntComparer : IComparer<IEnumerator<int>> {
			public int Compare(IEnumerator<int> x, IEnumerator<int> y) { return x.Current - y.Current; }
		}
		public static IEnumerable<int> SortedUnion(IEnumerable<int>[] inorderLists, bool limitedRangeInts = false) {
			IEnumerator<int>[] generators = new IEnumerator<int>[inorderLists.Length];

			try {
				for (int i = 0; i < inorderLists.Length; i++)
					generators[i] = inorderLists[i].GetEnumerator();

				CostHeap<IEnumerator<int>> gens = new CostHeap<IEnumerator<int>>();
				//IHeap<IEnumerator<int>> gens = limitedRangeInts?
				//    Heap.Factory<IEnumerator<int>>().Create(new FastEnumeratorIntComparer()) :
				//    Heap.Factory<IEnumerator<int>>().Create(new SafeEnumeratorIntComparer());


				foreach (var gen in generators)
					if (gen.MoveNext()) gens.Add(gen, gen.Current);
				//the costs are the enumerables, here.
				int lastYield = gens.Count > 0 ? gens.Top().Cost - 1 : 0;//anything but equal!

				while (gens.Count > 0) {
					var current = gens.Top();

					if (current.Cost != lastYield) yield return lastYield = current.Cost;

					if (current.Item.MoveNext()) gens.TopCostChanged(current.Item.Current);
					else gens.RemoveTop();
				}
				//SortedDictionary<int, IEnumerator<int>> sorter = new SortedDictionary<int, IEnumerator<int>>();
			} finally {
				DisposeAll(generators, 0);
			}
		}

		static void DisposeAll<T>(T[] disposables, int startAt) where T : IDisposable {
			for (int i = startAt; i < disposables.Length; i++) {
				try {
					if (disposables[i] != null) disposables[i].Dispose();
				} catch {
					DisposeAll(disposables, i + 1);
					throw;
				}
			}
		}
	}
}

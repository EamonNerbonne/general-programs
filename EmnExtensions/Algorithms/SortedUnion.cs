using System;
using System.Collections.Generic;
using System.Linq;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms {
	public static class SortedUnionAlgorithm {
		public static IEnumerable<int> SortedUnion(IEnumerable<int>[] inorderLists, bool limitedRangeInts = false) {
			IEnumerator<int>[] generators = new IEnumerator<int>[inorderLists.Length];

			try {
				for (int i = 0; i < inorderLists.Length; i++)
					generators[i] = inorderLists[i].GetEnumerator();

				CostHeap<IEnumerator<int>> gens = new CostHeap<IEnumerator<int>>();


				foreach (var gen in generators)
					if (gen.MoveNext()) gens.Add(gen, gen.Current);
				//the costs *are* the current enumerator value
				int lastYield = gens.Count > 0 ? gens.Top().Cost - 1 : 0;//anything but equal!

				while (gens.Count > 0) {
					var current = gens.Top();

					if (current.Cost != lastYield) yield return lastYield = current.Cost;

					if (current.Item.MoveNext()) gens.TopCostChanged(current.Item.Current);
					else gens.RemoveTop();
				}
			} finally {
				DisposeAll(generators, 0);
			}
		}

		static internal void DisposeAll<T>(T[] disposables, int startAt) where T : IDisposable {
			for (int i = startAt; i < disposables.Length; i++) {
				try {
					if (disposables[i] != null) disposables[i].Dispose();
				} catch {
					DisposeAll(disposables, i + 1);
					throw;
				}
			}
		}

		public static IEnumerable<int> ZipMerge(IEnumerable<int> a, IEnumerable<int> b) {
			var enumA = a.GetEnumerator();
			var enumB = b.GetEnumerator();
			int elA, elB;
			if (enumA.MoveNext()) {
				if (enumB.MoveNext()) {
					elA = enumA.Current;
					elB = enumB.Current;
					while (true) {
						if (elA < elB) {
							yield return elA;
							if (enumA.MoveNext()) elA = enumA.Current;
							else {//no more a's
								yield return elB;
								while (enumB.MoveNext()) yield return enumB.Current;
								break;
							}
						} else {
							yield return elB;
							if (enumB.MoveNext()) elB = enumB.Current;
							else {//no more b's!
								yield return elA;
								while (enumA.MoveNext()) yield return enumA.Current;
								break;
							}
						}
					}
				} else {
					yield return enumA.Current;
					while (enumA.MoveNext()) yield return enumA.Current;
				}
			} else while (enumB.MoveNext()) yield return enumB.Current;
		}

		public static IEnumerable<int> RemoveDup(IEnumerable<int> orderedList) {
			var orderedEnum = orderedList.GetEnumerator();
			if (!orderedEnum.MoveNext()) yield break;
			int current = orderedEnum.Current;
			yield return current;
			while (orderedEnum.MoveNext()) {
				int newVal = orderedEnum.Current;
				if (newVal != current) {
					current = newVal;
					yield return current;
				}
			}
		}

	}
}

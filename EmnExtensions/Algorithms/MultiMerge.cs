using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmnExtensions.Collections;

namespace EmnExtensions.Algorithms {
	public static class MultiMergeAlgorithm {

		class SafeEnumeratorIntComparer : IComparer<IEnumerator<int>> {
			public int Compare(IEnumerator<int> x, IEnumerator<int> y) {
				int xc = x.Current, yc = y.Current;
				return xc < yc ? -1 : xc > yc ? 1 : 0;
			}
		}
		class FastEnumeratorIntComparer : IComparer<IEnumerator<int>> {
			public int Compare(IEnumerator<int> x, IEnumerator<int> y) { return x.Current - y.Current; }
		}
		public static IEnumerable<int> MultiMerge(IEnumerable<int>[] inorderLists, bool limitedRangeInts=false) {
			IEnumerator<int>[] generators = new IEnumerator<int>[inorderLists.Length]; 
			
			try {
				for (int i = 0; i < inorderLists.Length; i++)
					generators[i] = inorderLists[i].GetEnumerator();

				IHeap<IEnumerator<int>> gens = limitedRangeInts?
					Heap.Create<IEnumerator<int>, FastEnumeratorIntComparer>(new FastEnumeratorIntComparer()) :
					Heap.Create<IEnumerator<int>, SafeEnumeratorIntComparer>(new SafeEnumeratorIntComparer());


				foreach(var gen in generators) 
					if(gen.MoveNext()) gens.Add(gen);
				int lastYield = gens.Count>0?gens[0].Current-1:0;//anything but equal!
				
				while(gens.Count > 0) {
					IEnumerator<int> current;
					gens.RemoveTop(out current);
					
					if(current.Current!=lastYield) yield return lastYield=current.Current;

					if(current.MoveNext()) gens.Add(current);
				}
				//SortedDictionary<int, IEnumerator<int>> sorter = new SortedDictionary<int, IEnumerator<int>>();
			} finally {
				DisposeAll(generators, 0);
			}
		}

		static void DisposeAll<T>(T[] disposables, int startAt) where T : IDisposable {
			for (int i = startAt; i < disposables.Length; i++) {
				try {
					if(disposables[i]!=null) disposables[i].Dispose();
				} catch {
					DisposeAll(disposables, i + 1);
					throw;
				}
			}
		}
	}
}

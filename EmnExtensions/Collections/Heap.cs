using System;
using System.Collections.Generic;
using System.Linq;

namespace EmnExtensions.Collections {
	public interface IHeap<T> {
		void Add(T elem);
		int Count { get; }
		bool RemoveTop(out T retval);
		T RemoveTop();
		T this[int i] { get; }
		void Delete(int indexOfItem);
		IEnumerable<T> ElementsInRoughOrder { get; }
	}
	public interface IHeapIndexSink<T> {
		void IndexChanged(T item, int newIndex);
	}
	public interface IHeapIndexed<T> {
		void HeapIndexChanged(int newIndex);
	}


	public static class Heap {
		public static HeapFactory<T> Factory<T>() { return new HeapFactory<T>(); }

		public static IHeap<T> CreateSelfTracked<T>() where T : IComparable<T>, IHeapIndexed<T> {
			return new Heap<T, SelfSink<T>, ComparableComparer<T>>(new SelfSink<T>(), new ComparableComparer<T>());
		}

		public static IHeap<T> CreateUntracked<T>() where T : IComparable<T> {
			return new Heap<T, NoSink<T>, ComparableComparer<T>>(new NoSink<T>(), new ComparableComparer<T>());
		}

		public static IHeap<T> CreateTracked<T>(Action<T, int> indexChanged) where T : IComparable<T> {
			return new Heap<T, DelegateSink<T>, ComparableComparer<T>>(CreateDelegateSink(indexChanged), new ComparableComparer<T>());
		}


		public struct HeapFactory<T> {
			public IHeap<T> Create(Action<T, int> indexSet = null) {
				if (typeof(T) == typeof(int))
					return (IHeap<T>)new HeapFactory<int>().Create(new IntComparer(), (Action<int, int>)((object)indexSet));
				if (typeof(T) == typeof(long))
					return (IHeap<T>)new HeapFactory<long>().Create(new LongComparer(), (Action<long, int>)((object)indexSet));

				return Create(Comparer<T>.Default, indexSet);
			}

			public IHeap<T> Create<TComp>(TComp customComparer, Action<T, int> indexSet = null)
				where TComp : IComparer<T> {
				if (indexSet == null)
					return Create(customComparer, new NoSink<T>());
				else
					return Create(customComparer, CreateDelegateSink(indexSet));
			}

			public IHeap<T> Create<TSink, TComp>(TComp customComparer, TSink sink)
				where TComp : IComparer<T>
				where TSink : IHeapIndexSink<T> {
				return new Heap<T, TSink, TComp>(sink, customComparer);
			}
		}

		public struct IntComparer : IComparer<int> { public int Compare(int x, int y) { return x < y ? -1 : x > y ? 1 : 0; } }
		public struct FastIntComparer : IComparer<int> { public int Compare(int x, int y) { return x - y; } }
		public struct LongComparer : IComparer<long> { public int Compare(long x, long y) { return x < y ? -1 : x > y ? 1 : 0; } }
		public struct ComparableComparer<T> : IComparer<T> where T : IComparable<T> { public int Compare(T x, T y) { return x.CompareTo(y); } }

		static DelegateSink<T> CreateDelegateSink<T>(Action<T, int> sink) { return new DelegateSink<T>(); }
		struct DelegateSink<T> : IHeapIndexSink<T> {
			readonly Action<T, int> sink;
			public DelegateSink(Action<T, int> sink) { this.sink = sink; }
			public void IndexChanged(T item, int newIndex) { sink(item, newIndex); }
		}
		struct NoSink<T> : IHeapIndexSink<T> {
			public void IndexChanged(T item, int newIndex) { }
		}

		struct SelfSink<T> : IHeapIndexSink<T> where T : IHeapIndexed<T> {
			public void IndexChanged(T item, int newIndex) { item.HeapIndexChanged(newIndex); }
		}

	}



	public class Heap<T, TSink, TComp> : IHeap<T>
		where TComp : IComparer<T>
		where TSink : IHeapIndexSink<T> {
		readonly TSink indexSet;
		readonly TComp comparer;
		T[] backingStore = new T[8];
		int backingCount;
		public Heap(TSink indexSet, TComp customComparer) {
			this.indexSet = indexSet;
			comparer = customComparer;
		}

		public void Add(T elem) {
			if (backingCount == backingStore.Length)
				Array.Resize(ref backingStore, backingStore.Length * 2);
			int newIndex = backingCount++;
			Bubble(newIndex, elem);
		}

		public int Count { get { return backingCount; } }

		private void Bubble(int newIndex, T elem) {
			int parIndex = (newIndex - 1) / 2;
			while (newIndex != 0 && 0 < comparer.Compare(backingStore[parIndex], elem)) {
				backingStore[newIndex] = backingStore[parIndex];
				indexSet.IndexChanged(backingStore[newIndex], newIndex);
				newIndex = parIndex;
				parIndex = (newIndex - 1) / 2;
			}
			backingStore[newIndex] = elem;
			indexSet.IndexChanged(backingStore[newIndex], newIndex);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
		public bool RemoveTop(out T retval) {
			if (backingCount == 0) {
				retval = default(T);
				return false;
			}
			retval = backingStore[0];

			backingCount--;
			if (0 != backingCount) Sink(0, backingStore[backingCount]);
			backingStore[backingCount] = default(T);

			return true;
		}

		public T RemoveTop() {
			if (backingCount == 0) throw new InvalidOperationException("Can't remove top; sequence empty");
			T retval = backingStore[0];

			backingCount--;
			if (0 != backingCount) Sink(0, backingStore[backingCount]);
			backingStore[backingCount] = default(T);

			return retval;
		}

		public void Delete(int indexOfItem) {
			T toSink = backingStore[--backingCount];
			//we must delete from the end of the array to avoid holes.
			//but we actually want to delete @ indexOfItem; so we'll insert this last element @ indexOfItem,
			//and if it's heavier than the original item there, we need to sink it towards the leaves
			//but if it's lighter, we need to bubble as usual.
			int sinkCompToDeleted = comparer.Compare(toSink, backingStore[indexOfItem]);

			if (indexOfItem == backingCount) { } //we deleted the last item.
			else if (sinkCompToDeleted >= 0) // sink item is heavier than deleted item, need to sink it.
				Sink(indexOfItem, toSink);
			else if (sinkCompToDeleted < 0) // last item is lighter than deleted item, need to bubble it.
				Bubble(indexOfItem, toSink);
		}

		void Sink(int p, T elem) {
			while (2 * p + 2 < backingCount) {
				int idxKidA = 2 * p + 1, idxKidB = idxKidA + 1;
				int kid = 0 >= comparer.Compare(backingStore[idxKidA], backingStore[idxKidB]) ? idxKidA : idxKidB;
				if (0 < comparer.Compare(elem, backingStore[kid])) {//elem isn't smaller than kid
					indexSet.IndexChanged(backingStore[p] = backingStore[kid], p);
					p = kid;
				} else break;
			}
			int singleKid = 2 * p + 1;
			if (singleKid < backingCount && 0 < comparer.Compare(elem, backingStore[singleKid])) {
				indexSet.IndexChanged(backingStore[p] = backingStore[singleKid], p);
				p = singleKid;
			}
			indexSet.IndexChanged(backingStore[p] = elem, p);
		}

		public T this[int i] { get { return backingStore[i]; } }
		public IEnumerable<T> ElementsInRoughOrder { get { return backingStore.Take(backingCount); } }
	}
}

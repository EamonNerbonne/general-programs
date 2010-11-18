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

	public static class Heap {
		public static IHeap<T> Create<T>() {
			if (typeof(T) == typeof(int))
				return (IHeap<T>)new HeapNoIndex<int, IntComparer>(new IntComparer());
			else return new HeapNoIndex<T, IComparer<T>>(Comparer<T>.Default);
		}

		public static IHeap<T> Create<T,TC>(TC comp) where TC:IComparer<T>{
			return new HeapNoIndex<T, TC>(comp);
		}
		static void Sink<T>(T ignore, int idx) { }
		public static IHeap<T> CreateIndexable<T>(Action<T, int> indexSet, IComparer<T> customComparer = null) {
			if(indexSet==null)
				return new HeapWithIndex<T, NoSink<T>, IComparer<T>>(new NoSink<T>(), customComparer ?? Comparer<T>.Default);
			else
				return new HeapWithIndex<T, DelegateSink<T>, IComparer<T>>(new DelegateSink<T>(indexSet), customComparer ?? Comparer<T>.Default);
		}

		public static IHeap<T> CreateSmart<T, TComp>(T sample, TComp customComparer, Action<T, int> indexSet) where TComp:IComparer<T> {
			if (indexSet == null)
				return new HeapWithIndex<T, NoSink<T>, TComp>(new NoSink<T>(), customComparer);
			else
				return new HeapWithIndex<T, DelegateSink<T>, TComp>(new DelegateSink<T>(indexSet), customComparer );
		}


		//public static IHeap<T> CreateIndexable<T, TSink, TCompare>(TSink indexSet, TCompare customComparer = null) {
		//    return new HeapWithIndex<T>(indexSet, customComparer);
		//}

		public struct IntComparer : IComparer<int> { public int Compare(int x, int y) { return x < y ? -1 : x > y ? 1 : 0; } }
		public struct FastIntComparer : IComparer<int> { public int Compare(int x, int y) { return x - y; } }

		struct DelegateSink<T> : IHeapIndexSink<T> {
			readonly Action<T,int> sink;
			public DelegateSink(Action<T, int> sink) { this.sink = sink; }
			public void IndexChanged(T item, int newIndex) {sink(item, newIndex);}
		}
		struct NoSink<T> : IHeapIndexSink<T> {
			public void IndexChanged(T item, int newIndex) { }
		}
	}


	class HeapNoIndex<T, TC> : IHeap<T> where TC : IComparer<T> {
		readonly TC comparer;
		T[] backingStore = new T[8];
		int backingCount;
		public HeapNoIndex(TC customComparer) { comparer = customComparer; }

		public void Add(T elem) {
			if (backingCount == backingStore.Length)
				Array.Resize(ref backingStore, backingStore.Length * 2);
			int newIndex = backingCount++;
			//backingStore[newIndex] = elem;
			Bubble(newIndex, elem);
		}

		public int Count { get { return backingCount; } }

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
			backingStore[backingCount] = default(T);
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

		private void Sink(int p, T elem) {
			while (2 * p + 2 < backingCount) {
				int idxKidA = 2 * p + 1, idxKidB = idxKidA + 1;
				int kid = 0 >= comparer.Compare(backingStore[idxKidA], backingStore[idxKidB]) ? idxKidA : idxKidB;
				if (0 < comparer.Compare(elem, backingStore[kid])) {//elem isn't smaller than kid
					backingStore[p] = backingStore[kid];
					p = kid;
				} else break;
			}
			int singleKid = 2 * p + 1;
			if (singleKid < backingCount && 0 < comparer.Compare(elem, backingStore[singleKid])) {
				backingStore[p] = backingStore[singleKid];
				p = singleKid;
			}
			backingStore[p] = elem;
		}

		void Bubble(int newIndex, T elem) {
			int parIndex = (newIndex - 1) / 2;
			while (newIndex != 0 && 0 < comparer.Compare(backingStore[parIndex], elem)) {
				backingStore[newIndex] = backingStore[parIndex];
				newIndex = parIndex;
				parIndex = (newIndex - 1) / 2;
			}
			backingStore[newIndex] = elem;
		}

		public T this[int i] { get { return backingStore[i]; } }
		public IEnumerable<T> ElementsInRoughOrder { get { return backingStore.Take(backingCount); } }
	}



	class HeapWithIndex<T, TSink, TComp> : IHeap<T> where TComp : IComparer<T> where TSink:IHeapIndexSink<T> {
		readonly TSink indexSet;
		readonly TComp comparer;
		T[] backingStore = new T[8];
		int backingCount;
		static void ignore(T t, int i) { }
		public HeapWithIndex(TSink indexSet, TComp customComparer) {
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
				indexSet.IndexChanged (backingStore[newIndex], newIndex);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmnExtensions.Collections
{

	public class Heap<T>
		where T : IComparable<T>
	{
		T[] backingStore = new T[8];
		int backingCount = 0;
		Action<T, int> indexSet;
		static void ignore(T t, int i) { }
		public Heap(Action<T, int> indexSet) {
			this.indexSet = indexSet ?? ignore;
		}
		public Heap() : this(null) { }
		public void Add(T elem) {
			if (backingCount == backingStore.Length)
				Array.Resize(ref backingStore, backingStore.Length * 2);
			int newIndex = backingCount++;
			//backingStore[newIndex] = elem;
			Bubble(newIndex, elem);
		}


		public int Count { get { return backingCount; } }

		private void Bubble(int newIndex, T elem) {
			int parIndex = (newIndex - 1) / 2;
			while (newIndex != 0 && 0 < backingStore[parIndex].CompareTo(elem)) {
				backingStore[newIndex] = backingStore[parIndex];
				indexSet(backingStore[newIndex], newIndex);
				newIndex = parIndex;
				parIndex = (newIndex - 1) / 2;
			}
			backingStore[newIndex] = elem;
			indexSet(backingStore[newIndex], newIndex);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
		public bool RemoveTop(out T retval) {
			if (backingCount == 0) {
				retval = default(T);
				return false;
			}
			retval = backingStore[0];
			Delete(0);

			return true;
		}

		public T RemoveTop() {
			T retval;
			if (RemoveTop(out retval)) {
				return retval;
			} else {
				throw new ArgumentOutOfRangeException("Can't remove top; sequence empty");
			}
		}

		public void Delete(int indexOfItem) {
			T toSink = backingStore[--backingCount];
			//we must delete from the end of the array to avoid holes.
			//but we actually want to delete @ indexOfItem; so we'll insert this last element @ indexOfItem,
			//and if it's heavier than the original item there, we need to sink it towards the leaves
			//but if it's lighter, we need to bubble as usual.
			int sinkCompToDeleted = toSink.CompareTo(backingStore[indexOfItem]);

			if (indexOfItem == backingCount) { } //we deleted the last item.
			else if (sinkCompToDeleted >= 0) // sink item is heavier than deleted item, need to sink it.
				Sink(indexOfItem, toSink);
			else if (sinkCompToDeleted < 0) // last item is lighter than deleted item, need to bubble it.
				Bubble(indexOfItem, toSink);
		}

		private void Sink(int p, T elem) {
			while (2 * p + 1 < backingCount) {
				int kid = 2 * p + 2 < backingCount ?
					(0 < backingStore[2 * p + 1].CompareTo(backingStore[2 * p + 2]) ? 2 * p + 2 : 2 * p + 1) :
					(2 * p + 1);
				if (0 >= elem.CompareTo(backingStore[kid])) {
					break;
				} else {//elem isn't smaller than kid
					indexSet(backingStore[p] = backingStore[kid], p);
					p = kid;
				}
			}
			indexSet(backingStore[p] = elem, p);
		}

		public T this[int i] { get { return backingStore[i]; } }
		public IEnumerable<T> ElementsInRoughOrder { get { return backingStore.Take(backingCount); } }
	}
}
